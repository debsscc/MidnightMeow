///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que gerencia o sistema de adrenalina do jogador.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAdrenaline : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;

    [Header("Unity Events")]
    public UnityEvent OnFrenzyActivated;
    public UnityEvent OnFrenzyDeactivated;

    private PlayerInputHandler _input;
    private float _currentAdrenaline;
    private bool _isFrenzyActive = false;
    private float _frenzyTimer;
    private bool _hasInvokedLowAdrenaline = false;

    public bool IsFrenzyActive => _isFrenzyActive;
    public float CurrentAdrenaline => _currentAdrenaline;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Start()
    {
        _currentAdrenaline = stats.maxAdrenaline;
        GameEvents.InvokePlayerAdrenalineChanged(_currentAdrenaline, stats.maxAdrenaline);
    }

    private void OnEnable()
    {
        GameEvents.OnCienciaCollected += HandleCienciaCollected;
        _input.OnFrenzyInput += HandleFrenzyInput;
    }

    private void OnDisable()
    {
        GameEvents.OnCienciaCollected -= HandleCienciaCollected;
        _input.OnFrenzyInput -= HandleFrenzyInput;
    }

    private void Update()
    {
        if (_isFrenzyActive)
        {
            UpdateFrenzyMode();
        }
        else
        {
            DecayAdrenaline();
        }

        CheckLowAdrenaline();
    }

    private void DecayAdrenaline()
    {
        if (_currentAdrenaline > 0)
        {
            _currentAdrenaline -= stats.adrenalineDecayRate * Time.deltaTime;
            _currentAdrenaline = Mathf.Max(0, _currentAdrenaline);
            GameEvents.InvokePlayerAdrenalineChanged(_currentAdrenaline, stats.maxAdrenaline);
        }
    }

    private void HandleCienciaCollected(int amount)
    {
        _currentAdrenaline += stats.adrenalineGainPerCiencia;
        _currentAdrenaline = Mathf.Min(_currentAdrenaline, stats.maxAdrenaline);
        GameEvents.InvokePlayerAdrenalineChanged(_currentAdrenaline, stats.maxAdrenaline);

        if (_currentAdrenaline > stats.adrenalineLowThreshold)
        {
            _hasInvokedLowAdrenaline = false;
        }
    }

    private void HandleFrenzyInput()
    {
        if (!_isFrenzyActive && _currentAdrenaline >= stats.adrenalineThresholdToActivate)
        {
            ActivateFrenzy();
        }
    }

    private void ActivateFrenzy()
    {
        _isFrenzyActive = true;
        _frenzyTimer = stats.frenzyDuration;
        OnFrenzyActivated?.Invoke();
        Debug.Log("Frenzy Mode Activated!");
    }

    private void UpdateFrenzyMode()
    {
        _frenzyTimer -= Time.deltaTime;

        _currentAdrenaline -= stats.adrenalineDecayRate * Time.deltaTime * 2f;
        _currentAdrenaline = Mathf.Max(0, _currentAdrenaline);
        GameEvents.InvokePlayerAdrenalineChanged(_currentAdrenaline, stats.maxAdrenaline);

        if (_frenzyTimer <= 0 || _currentAdrenaline <= 0)
        {
            DeactivateFrenzy();
        }
    }

    private void DeactivateFrenzy()
    {
        _isFrenzyActive = false;
        OnFrenzyDeactivated?.Invoke();
        Debug.Log("Frenzy Mode Deactivated!");
    }

    private void CheckLowAdrenaline()
    {
        if (_currentAdrenaline <= stats.adrenalineLowThreshold && !_hasInvokedLowAdrenaline)
        {
            _hasInvokedLowAdrenaline = true;
            GameEvents.InvokeAdrenalineLow();
        }
    }

    public float GetSpeedMultiplier()
    {
        return _isFrenzyActive ? stats.frenzySpeedMultiplier : 1f;
    }

    public int GetBonusBounces()
    {
        return _isFrenzyActive ? stats.frenzyBonusBounces : 0;
    }
}
