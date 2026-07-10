using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Label world-space acima do jogador caído. Texto depende do observador local (IsOwner / aliado).
/// Estados: caído → proximidade → revivendo (%) → oculto.
/// </summary>
[RequireComponent(typeof(NetworkPlayerHealth))]
public class DownedPlayerWorldUI : MonoBehaviour
{
    public enum DownedReviveLabelMode
    {
        Hidden,
        OwnerWaiting,
        AllyApproach,
        AllyPressE,
        ReviveProgress
    }

    [SerializeField] private GameObject reviveUIPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.25f, 0f);

    private NetworkPlayerHealth _health;
    private GameObject _promptInstance;
    private Transform _promptTransform;
    private RectTransform _canvasRect;
    private TextMeshProUGUI _label;
    private DownedReviveLabelMode _lastMode = DownedReviveLabelMode.Hidden;

    private void Awake()
    {
        _health = GetComponent<NetworkPlayerHealth>();
        TryResolvePrefabReference();
        InstantiatePromptIfNeeded();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_promptInstance != null)
            Destroy(_promptInstance);
    }

    private DownedPlayerConfig ResolveConfig() =>
        DownedPlayerConfigUtility.Resolve(_health != null ? _health.DownedConfig : null);

    private void TryResolvePrefabReference()
    {
        if (reviveUIPrefab != null)
            return;

        DownedPlayerConfig config = ResolveConfig();
        if (config != null)
            reviveUIPrefab = config.revivePromptPrefab;
    }

    private void InstantiatePromptIfNeeded()
    {
        TryResolvePrefabReference();

        if (_promptInstance != null || reviveUIPrefab == null)
            return;

        _promptInstance = Instantiate(reviveUIPrefab);
        _promptInstance.name = reviveUIPrefab.name;
        _promptTransform = _promptInstance.transform;
        _canvasRect = _promptInstance.GetComponent<RectTransform>();

        ConfigureWorldSpaceCanvas(_canvasRect);

        DownedReviveUILabelView labelView = _promptInstance.GetComponentInChildren<DownedReviveUILabelView>(true);
        _label = labelView != null ? labelView.Label : _promptInstance.GetComponentInChildren<TextMeshProUGUI>(true);
        NormalizeLabelLayout(_label);

        _promptInstance.SetActive(false);
    }

    /// <summary>Alinha escala/layout ao padrão do selamento (RatHoleSealPromptUI.BuildUI).</summary>
    private static void ConfigureWorldSpaceCanvas(RectTransform canvasRect)
    {
        if (canvasRect == null)
            return;

        Canvas canvas = canvasRect.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 115;
        }

        canvasRect.sizeDelta = new Vector2(4.8f, 0.22f);
        canvasRect.localScale = Vector3.one;
    }

    private static void NormalizeLabelLayout(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.fontSize = 1.65f;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.Center;
    }

    private void LateUpdate()
    {
        if (_health == null || !_health.IsSpawned)
        {
            ApplyMode(DownedReviveLabelMode.Hidden);
            return;
        }

        if (_promptInstance == null)
        {
            InstantiatePromptIfNeeded();
            if (_promptInstance == null)
                return;
        }

        DownedReviveLabelMode mode = ResolveLabelModeForLocalViewer();
        ApplyMode(mode);

        if (mode == DownedReviveLabelMode.Hidden)
            return;

        _promptTransform.position = transform.position + offset;
        _promptTransform.rotation = Quaternion.identity;
    }

    /// <summary>Resolve o modo de exibição para o cliente local (autoridade de visualização).</summary>
    public DownedReviveLabelMode ResolveLabelModeForLocalViewer()
    {
        if (!_health.CanBeRevived)
            return DownedReviveLabelMode.Hidden;

        DownedPlayerConfig config = ResolveConfig();
        if (config == null)
            return DownedReviveLabelMode.Hidden;

        bool sessionActive = IsReviveSessionActive();
        if (sessionActive || _health.IsReviveZoneActive)
            return DownedReviveLabelMode.ReviveProgress;

        if (_health.IsOwner)
        {
            if (!CanShowCooperativeRevivePrompt())
                return DownedReviveLabelMode.Hidden;

            return DownedReviveLabelMode.OwnerWaiting;
        }

        NetworkPlayerHealth localAlly = ResolveLocalFightingPlayer();
        if (localAlly == null)
            return DownedReviveLabelMode.Hidden;

        float dist = Vector2.Distance(localAlly.transform.position, transform.position);
        if (dist <= config.revivePromptRadius)
            return DownedReviveLabelMode.AllyPressE;

        float visibilityRadius = config.GetReviveLabelVisibilityRadius();
        if (dist <= visibilityRadius)
            return DownedReviveLabelMode.AllyApproach;

        return DownedReviveLabelMode.Hidden;
    }

    private bool CanShowCooperativeRevivePrompt()
    {
        if (GameSessionContext.IsSinglePlayer)
            return false;

        NetworkPlayerHealth[] players =
            Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth other = players[i];
            if (other == null || other == _health || !other.IsSpawned)
                continue;

            if (other.CanFight)
                return true;
        }

        return false;
    }

    private bool IsReviveSessionActive()
    {
        NetworkDownedReviveManager manager = NetworkDownedReviveManager.Instance;
        return manager != null && manager.IsSpawned && manager.HasActiveSession(_health.OwnerClientId);
    }

    private static NetworkPlayerHealth ResolveLocalFightingPlayer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return null;

        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null)
            return null;

        if (!localPlayer.TryGetComponent(out NetworkPlayerHealth health))
            return null;

        return health.CanFight ? health : null;
    }

    private void ApplyMode(DownedReviveLabelMode mode)
    {
        bool visible = mode != DownedReviveLabelMode.Hidden;
        SetVisible(visible);

        if (!visible || _label == null)
        {
            _lastMode = mode;
            return;
        }

        DownedPlayerConfig config = ResolveConfig();
        if (mode == DownedReviveLabelMode.ReviveProgress || mode != _lastMode)
        {
            _label.text = mode switch
            {
                DownedReviveLabelMode.OwnerWaiting => config.GetOwnerWaitingText(),
                DownedReviveLabelMode.AllyApproach => config.GetAllyApproachText(),
                DownedReviveLabelMode.AllyPressE => config.GetAllyPressEText(),
                DownedReviveLabelMode.ReviveProgress => FormatProgressText(_health.ReviveProgress, config),
                _ => string.Empty
            };
        }

        _lastMode = mode;
    }

    private static string FormatProgressText(float normalizedProgress, DownedPlayerConfig config)
    {
        int percent = Mathf.Clamp(Mathf.RoundToInt(normalizedProgress * 100f), 0, 100);
        return config != null ? config.FormatReviveProgressText(percent) : $"{percent}%";
    }

    private void SetVisible(bool visible)
    {
        if (_promptInstance != null)
            _promptInstance.SetActive(visible);
    }
}
