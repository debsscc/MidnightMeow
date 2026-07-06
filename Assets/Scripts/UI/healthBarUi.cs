/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Barra de vida do HUD com animação de damage trail e flash ao tomar dano.
---------------------------------------------------------------- */

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class healthBarUi : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    [Header("Animação de vida")]
    [Tooltip("Fill secundário que fica atrás e demora a descer após o dano.")]
    [SerializeField] private RawImage damageTrailFill;
    [SerializeField] private Color damageTrailColor = new Color(0.95f, 0.52f, 0.42f, 0.88f);
    [SerializeField] private float damageMainLerpSpeed = 18f;
    [SerializeField] private float damageTrailLerpSpeed = 4.8f;
    [SerializeField] private float healLerpSpeed = 11f;
    [SerializeField] private float trailDelayAfterMain = 0.12f;
    [SerializeField] private float damageFlashDuration = 0.16f;
    [SerializeField] private float damageFlashStrength = 0.42f;

    [Header("Brilho do damage trail")]
    [Tooltip("Velocidade do shimmer (brilho pulsante) enquanto o trail desce.")]
    [SerializeField] private float trailGlowSpeed = 7f;
    [Tooltip("Intensidade do brilho somado à cor do trail (0 = sem brilho).")]
    [SerializeField] private float trailGlowStrength = 0.3f;

    [Header("Fundo da barra")]
    [Tooltip("Imagem de fundo do slider; se vazio, tenta achar 'Background' filho do slider.")]
    [SerializeField] private Graphic backgroundGraphic;
    [SerializeField] private Color backgroundColor = Color.white;

    private Graphic _mainFillGraphic;
    private Color _mainFillDefaultColor;

    private float _targetNormalized = 1f;
    private float _displayNormalized = 1f;
    private float _trailNormalized = 1f;
    private float _trailHoldTimer;
    private float _damageFlashTimer;
    private bool _needsVisualUpdate = true;

    private Coroutine _refreshRoutine;

    private void Awake()
    {
        CacheFillReferences();
        EnsureDamageTrailFill();
        ResolveAndApplyBackground();
    }

    private void OnEnable()
    {
        GameplaySceneBootstrap.TryEnsureGameplayHud();
        NetworkPlayerHealth.OnNetworkHealthChanged += HandleNetworkHealthChanged;
        NetworkPlayerController.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        QueueRefresh();
    }

    private void OnDisable()
    {
        NetworkPlayerHealth.OnNetworkHealthChanged -= HandleNetworkHealthChanged;
        NetworkPlayerController.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;

        if (_refreshRoutine != null)
        {
            StopCoroutine(_refreshRoutine);
            _refreshRoutine = null;
        }
    }

    private void Start()
    {
        QueueRefresh();
    }

    private void Update()
    {
        if (!_needsVisualUpdate || healthSlider == null)
            return;

        TickHealthAnimation();
        ApplyBars();
    }

    private void HandleLocalPlayerSpawned(NetworkPlayerController _)
    {
        QueueRefresh();
    }

    private void HandleNetworkHealthChanged(ulong clientId, float current, float max)
    {
        if (!IsLocalPlayerClientId(clientId))
            return;

        UpdateHealthBar(current, max);
    }

    private static bool IsLocalPlayerClientId(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager != null && clientId == networkManager.LocalClientId;
    }

    private void QueueRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (_refreshRoutine != null)
            StopCoroutine(_refreshRoutine);

        _refreshRoutine = StartCoroutine(RefreshAfterLayoutRoutine());
    }

    private IEnumerator RefreshAfterLayoutRoutine()
    {
        yield return null;

        if (TryGetLocalHealth(out float current, out float max))
            SetTargetNormalized(current / max, instant: true);
        else
            SetTargetNormalized(1f, instant: true);

        _refreshRoutine = null;
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f)
            return;

        SetTargetNormalized(currentHealth / maxHealth, instant: false);
    }

    private void SetTargetNormalized(float normalized, bool instant)
    {
        float clamped = Mathf.Clamp01(normalized);

        if (instant)
        {
            _targetNormalized = clamped;
            _displayNormalized = clamped;
            _trailNormalized = clamped;
            _trailHoldTimer = 0f;
            _damageFlashTimer = 0f;
            _needsVisualUpdate = true;
            ApplyBars();
            return;
        }

        if (clamped < _targetNormalized - 0.0001f)
        {
            _trailNormalized = Mathf.Max(_trailNormalized, _displayNormalized);
            _targetNormalized = clamped;
            _trailHoldTimer = trailDelayAfterMain;
            _damageFlashTimer = damageFlashDuration;
        }
        else if (clamped > _targetNormalized + 0.0001f)
        {
            _targetNormalized = clamped;
            _trailHoldTimer = 0f;
        }
        else
        {
            _targetNormalized = clamped;
        }

        _needsVisualUpdate = true;
    }

    private void TickHealthAnimation()
    {
        float dt = Time.deltaTime;
        bool isDamage = _targetNormalized < _displayNormalized - 0.0001f;
        bool isHeal = _targetNormalized > _displayNormalized + 0.0001f;

        // Barra principal: desce rápido no dano, sobe suave na cura.
        if (isDamage)
        {
            _displayNormalized = Mathf.Lerp(_displayNormalized, _targetNormalized, dt * damageMainLerpSpeed);
            if (Mathf.Abs(_displayNormalized - _targetNormalized) <= 0.001f)
                _displayNormalized = _targetNormalized;
        }
        else if (isHeal)
        {
            _displayNormalized = Mathf.Lerp(_displayNormalized, _targetNormalized, dt * healLerpSpeed);
            if (Mathf.Abs(_displayNormalized - _targetNormalized) <= 0.001f)
                _displayNormalized = _targetNormalized;
        }

        // Trail (barra clara) desce sozinho — independente de o main já ter assentado —,
        // segurando um instante e depois deslizando devagar até o HP atual.
        if (_trailNormalized > _targetNormalized + 0.001f)
        {
            if (isHeal)
            {
                _trailNormalized = Mathf.Lerp(_trailNormalized, _targetNormalized, dt * healLerpSpeed);
            }
            else if (_trailHoldTimer > 0f)
            {
                _trailHoldTimer -= dt;
            }
            else
            {
                _trailNormalized = Mathf.Lerp(_trailNormalized, _targetNormalized, dt * damageTrailLerpSpeed);
            }

            if (Mathf.Abs(_trailNormalized - _targetNormalized) <= 0.001f)
                _trailNormalized = _targetNormalized;
        }

        // O trail nunca pode ficar atrás da barra principal.
        if (_trailNormalized < _displayNormalized)
            _trailNormalized = _displayNormalized;

        if (_damageFlashTimer > 0f)
            _damageFlashTimer -= dt;

        _needsVisualUpdate =
            isDamage
            || isHeal
            || _trailHoldTimer > 0f
            || _damageFlashTimer > 0f
            || Mathf.Abs(_trailNormalized - _targetNormalized) > 0.001f;
    }

    private void ApplyBars()
    {
        if (healthSlider == null)
            return;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.SetValueWithoutNotify(_displayNormalized);

        ApplyTrailFill(_trailNormalized);
        ApplyTrailGlow();
        ApplyDamageFlash();
    }

    private void ApplyTrailGlow()
    {
        if (damageTrailFill == null)
            return;

        bool trailActive = _trailHoldTimer > 0f || _trailNormalized > _targetNormalized + 0.001f;
        if (!trailActive || trailGlowStrength <= 0f)
        {
            damageTrailFill.color = damageTrailColor;
            return;
        }

        float shimmer = (Mathf.Sin(Time.unscaledTime * trailGlowSpeed) * 0.5f + 0.5f) * trailGlowStrength;
        damageTrailFill.color = new Color(
            Mathf.Clamp01(damageTrailColor.r + shimmer),
            Mathf.Clamp01(damageTrailColor.g + shimmer),
            Mathf.Clamp01(damageTrailColor.b + shimmer),
            damageTrailColor.a);
    }

    private void ApplyTrailFill(float normalized)
    {
        if (damageTrailFill == null || healthSlider.fillRect == null)
            return;

        RectTransform trailRect = damageTrailFill.rectTransform;
        RectTransform mainFill = healthSlider.fillRect;
        float clamped = Mathf.Clamp01(normalized);

        trailRect.anchorMin = mainFill.anchorMin;
        trailRect.anchorMax = new Vector2(clamped, mainFill.anchorMax.y);
        trailRect.pivot = mainFill.pivot;
        trailRect.offsetMin = mainFill.offsetMin;
        trailRect.offsetMax = mainFill.offsetMax;
        trailRect.localScale = mainFill.localScale;
    }

    private void ApplyDamageFlash()
    {
        if (_mainFillGraphic == null)
            return;

        if (_damageFlashTimer <= 0f)
        {
            _mainFillGraphic.color = _mainFillDefaultColor;
            return;
        }

        float t = Mathf.Clamp01(_damageFlashTimer / damageFlashDuration);
        _mainFillGraphic.color = Color.Lerp(
            _mainFillDefaultColor,
            Color.white,
            t * damageFlashStrength);
    }

    private void CacheFillReferences()
    {
        if (healthSlider == null || healthSlider.fillRect == null)
            return;

        _mainFillGraphic = healthSlider.fillRect.GetComponent<Graphic>();
        if (_mainFillGraphic == null)
            _mainFillGraphic = healthSlider.fillRect.GetComponentInChildren<Graphic>();

        if (_mainFillGraphic != null)
            _mainFillDefaultColor = _mainFillGraphic.color;
    }

    private void ResolveAndApplyBackground()
    {
        if (backgroundGraphic == null && healthSlider != null)
        {
            Transform bg = healthSlider.transform.Find("Background");
            if (bg != null)
                backgroundGraphic = bg.GetComponent<Graphic>();
        }

        if (backgroundGraphic != null)
            backgroundGraphic.color = backgroundColor;
    }

    private void EnsureDamageTrailFill()
    {
        if (damageTrailFill != null || healthSlider == null || healthSlider.fillRect == null)
            return;

        Transform mainFill = healthSlider.fillRect;
        GameObject trailObject = Instantiate(mainFill.gameObject, mainFill.parent);
        trailObject.name = "DamageTrail";

        Transform trailTransform = trailObject.transform;
        trailTransform.SetSiblingIndex(mainFill.GetSiblingIndex());

        damageTrailFill = trailObject.GetComponent<RawImage>();
        if (damageTrailFill == null)
            damageTrailFill = trailObject.GetComponentInChildren<RawImage>();

        if (damageTrailFill != null)
            damageTrailFill.color = damageTrailColor;
    }

    private static bool TryGetLocalHealth(out float current, out float max)
    {
        current = 0f;
        max = 0f;

        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health == null || !health.IsSpawned || !health.IsOwner)
                continue;

            current = health.CurrentHealth;
            max = health.MaxHealth;
            return max > 0f;
        }

        HealthComponent legacy = Object.FindFirstObjectByType<HealthComponent>(FindObjectsInactive.Exclude);
        if (legacy != null && legacy.CompareTag("Player") && legacy.IsAlive)
        {
            current = legacy.CurrentHealth;
            max = legacy.MaxHealth;
            return max > 0f;
        }

        return false;
    }
}
