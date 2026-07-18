///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: HUD de dicas do tutorial — escuta OnTutorialTipChanged e faz fade.
// ---------------------------------------------------------------- */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Anexe ao painel da dica na HUD. Só atualiza apresentação; lógica fica no <see cref="TutorialManager"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class TutorialUIController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Texto da dica (TMP).")]
    [SerializeField] private TextMeshProUGUI tipLabel;

    [Tooltip("CanvasGroup do painel (fade). Se vazio, usa o do mesmo GameObject.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Transição")]
    [Tooltip("Duração do fade in (segundos, unscaled).")]
    [SerializeField] private float fadeInSeconds = 0.25f;

    [Tooltip("Duração do fade out (segundos, unscaled).")]
    [SerializeField] private float fadeOutSeconds = 0.2f;

    private Coroutine _transitionRoutine;
    private TutorialTipSO _displayedTip;
    private int _displayedProgress;
    private int _displayedRequired = 1;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (tipLabel != null)
        {
            GameplayUiFonts.Apply(tipLabel);
            tipLabel.color = Color.black;
            tipLabel.fontStyle = FontStyles.Bold;
            tipLabel.text = string.Empty;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnTutorialTipChanged += HandleTipChanged;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        // Se o Manager já publicou a dica antes deste painel habilitar, sincroniza.
        TutorialManager manager = FindFirstObjectByType<TutorialManager>();
        if (manager != null && manager.CurrentTip != null)
            HandleTipChanged(manager.CurrentTip, manager.CurrentProgress, manager.CurrentTip.RequiredCount);
    }

    private void OnDisable()
    {
        GameEvents.OnTutorialTipChanged -= HandleTipChanged;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }
    }

    private void HandleLocaleChanged(Locale _)
    {
        RefreshLabelText();
    }

    private void HandleTipChanged(TutorialTipSO tip, int currentProgress, int requiredCount)
    {
        bool sameTip = tip != null && tip == _displayedTip;
        _displayedProgress = currentProgress;
        _displayedRequired = Mathf.Max(1, requiredCount);

        // Atualização só de progresso (ex. kills 1/3): troca o texto sem fade.
        if (sameTip)
        {
            RefreshLabelText();
            return;
        }

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(TransitionToTip(tip));
    }

    private IEnumerator TransitionToTip(TutorialTipSO tip)
    {
        // Fade out da dica atual (se visível).
        if (canvasGroup != null && canvasGroup.alpha > 0.01f)
            yield return FadeTo(0f, fadeOutSeconds);

        _displayedTip = tip;

        if (tip == null)
        {
            if (tipLabel != null)
                tipLabel.text = string.Empty;
            _transitionRoutine = null;
            yield break;
        }

        RefreshLabelText();
        yield return FadeTo(1f, fadeInSeconds);
        _transitionRoutine = null;
    }

    private void RefreshLabelText()
    {
        if (tipLabel == null || _displayedTip == null)
            return;

        tipLabel.text = _displayedTip.Trigger == TutorialTipTrigger.UseAbility
            ? TutorialTipDisplayFormatter.FormatAbilityKeys(
                _displayedTip.ResolvedTipText,
                _displayedProgress)
            : TutorialTipDisplayFormatter.Format(
                _displayedTip.ResolvedTipText,
                _displayedProgress,
                _displayedRequired);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float start = canvasGroup.alpha;
        if (duration <= 0.001f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
