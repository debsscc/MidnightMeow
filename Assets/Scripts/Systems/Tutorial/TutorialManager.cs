///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Avança a sequência de dicas do tutorial via eventos de GameEvents.
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>
/// Controlador lógico do tutorial. Não toca UI — apenas gerencia índice/progresso e dispara
/// <see cref="GameEvents.OnTutorialTipChanged"/>. Coloque um por cena de fase sob
/// <c>---- UI ----</c> → Canvas (não no prefab legado Gameplay_UI).
/// Em multiplayer cada cliente tem o próprio manager: move/shoot/ability/dash são locais;
/// kill e selo avançam com eventos compartilhados de gameplay.
/// </summary>
[DisallowMultipleComponent]
public class TutorialManager : MonoBehaviour
{
    private const int AbilityCompleteMask = (1 << 0) | (1 << 1);

    [Header("Dados")]
    [Tooltip("Sequência de dicas a exibir nesta cena.")]
    [SerializeField] private TutorialSequenceSO sequence;

    [Header("Comportamento")]
    [Tooltip("Se true, inicia a primeira dica no OnEnable.")]
    [SerializeField] private bool autoStart = true;

    [Tooltip("Atraso em segundos antes de mostrar a primeira dica.")]
    [SerializeField] private float startDelaySeconds;

    private int _currentIndex = -1;
    private TutorialTipSO _currentTip;
    private int _progress;
    private int _abilityMask;
    private bool _isRunning;
    private bool _completed;
    private Coroutine _startRoutine;

    /// <summary>Dica ativa, ou null se o tutorial não está exibindo nada.</summary>
    public TutorialTipSO CurrentTip => _currentTip;

    /// <summary>Progresso da dica atual (kills, etc.).</summary>
    public int CurrentProgress => _progress;

    public bool IsRunning => _isRunning;
    public bool IsCompleted => _completed;

    private void OnEnable()
    {
        GameEvents.OnTutorialMoveExecuted += HandleMove;
        GameEvents.OnTutorialShootExecuted += HandleShoot;
        GameEvents.OnTutorialAbilityExecuted += HandleAbility;
        GameEvents.OnTutorialDashExecuted += HandleDash;
        GameEvents.OnTutorialSealHoleExecuted += HandleSealHole;
        GameEvents.OnEnemyKilledByPlayer += HandleEnemyKilled;

        if (autoStart)
            BeginSequence();
    }

    private void OnDisable()
    {
        GameEvents.OnTutorialMoveExecuted -= HandleMove;
        GameEvents.OnTutorialShootExecuted -= HandleShoot;
        GameEvents.OnTutorialAbilityExecuted -= HandleAbility;
        GameEvents.OnTutorialDashExecuted -= HandleDash;
        GameEvents.OnTutorialSealHoleExecuted -= HandleSealHole;
        GameEvents.OnEnemyKilledByPlayer -= HandleEnemyKilled;

        if (_startRoutine != null)
        {
            StopCoroutine(_startRoutine);
            _startRoutine = null;
        }
    }

    /// <summary>Reinicia a sequência do zero (útil para testes / replay).</summary>
    public void BeginSequence()
    {
        if (sequence == null || sequence.TipCount == 0)
        {
            Debug.LogWarning("[TutorialManager] Sequence vazia ou não atribuída.", this);
            CompleteTutorial();
            return;
        }

        if (_startRoutine != null)
            StopCoroutine(_startRoutine);

        _completed = false;
        _isRunning = true;
        _currentIndex = -1;
        _currentTip = null;
        ResetTipProgress();

        if (startDelaySeconds > 0.01f && isActiveAndEnabled)
            _startRoutine = StartCoroutine(BeginAfterDelay());
        else
            AdvanceToNextTip();
    }

    /// <summary>Encerra o tutorial e esconde a dica (payload null).</summary>
    public void CompleteTutorial()
    {
        _isRunning = false;
        _completed = true;
        _currentTip = null;
        _currentIndex = sequence != null ? sequence.TipCount : 0;
        ResetTipProgress();
        GameEvents.InvokeTutorialTipChanged(null);
        GameEvents.InvokeTutorialCompleted();
    }

    private System.Collections.IEnumerator BeginAfterDelay()
    {
        yield return new WaitForSecondsRealtime(startDelaySeconds);
        _startRoutine = null;
        if (_isRunning && !_completed)
            AdvanceToNextTip();
    }

    private void HandleMove() => TryAdvanceOn(TutorialTipTrigger.Move);

    private void HandleShoot() => TryAdvanceOn(TutorialTipTrigger.Shoot);

    private void HandleDash() => TryAdvanceOn(TutorialTipTrigger.Dash);

    private void HandleSealHole() => TryAdvanceOn(TutorialTipTrigger.SealHole);

    private void HandleAbility(AbilitySlot slot)
    {
        if (!_isRunning || _completed || _currentTip == null)
            return;

        if (_currentTip.Trigger != TutorialTipTrigger.UseAbility)
            return;

        int bit = slot switch
        {
            AbilitySlot.Ability1 => 0,
            AbilitySlot.Ability2 => 1,
            _ => -1
        };
        if (bit < 0)
            return;

        _abilityMask |= 1 << bit;
        _progress = _abilityMask;
        PublishCurrentTip();

        if (_abilityMask == AbilityCompleteMask)
            AdvanceToNextTip();
    }

    private void HandleEnemyKilled(ulong _)
    {
        if (!_isRunning || _completed || _currentTip == null)
            return;

        if (_currentTip.Trigger != TutorialTipTrigger.KillEnemies)
            return;

        _progress++;
        PublishCurrentTip();

        if (_progress >= _currentTip.RequiredCount)
            AdvanceToNextTip();
    }

    private void TryAdvanceOn(TutorialTipTrigger trigger)
    {
        if (!_isRunning || _completed || _currentTip == null)
            return;

        if (_currentTip.Trigger != trigger)
            return;

        int required = _currentTip.RequiredCount;
        if (required > 1)
        {
            _progress++;
            PublishCurrentTip();
            if (_progress < required)
                return;
        }

        AdvanceToNextTip();
    }

    private void AdvanceToNextTip()
    {
        if (sequence == null)
        {
            CompleteTutorial();
            return;
        }

        int next = _currentIndex + 1;
        while (next < sequence.TipCount && sequence.GetTip(next) == null)
            next++;

        if (next >= sequence.TipCount)
        {
            CompleteTutorial();
            return;
        }

        _currentIndex = next;
        _currentTip = sequence.GetTip(_currentIndex);
        ResetTipProgress();
        PublishCurrentTip();
    }

    private void PublishCurrentTip()
    {
        if (_currentTip == null)
        {
            GameEvents.InvokeTutorialTipChanged(null);
            return;
        }

        GameEvents.InvokeTutorialTipChanged(_currentTip, _progress, _currentTip.RequiredCount);
    }

    private void ResetTipProgress()
    {
        _progress = 0;
        _abilityMask = 0;
    }
}
