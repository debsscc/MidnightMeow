using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Label world-space acima da carruagem quebrada. Estados: aproximar → E → progresso (%).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CarriageController), typeof(NetworkCarriageHealth))]
public class CarriageRepairWorldUI : MonoBehaviour
{
    public enum CarriageRepairLabelMode
    {
        Hidden,
        AllyApproach,
        AllyPressE,
        RepairProgress
    }

    [SerializeField] private GameObject repairUIPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, 0f);

    public void SetOffset(Vector3 worldOffset) => offset = worldOffset;

    private CarriageController _carriage;
    private NetworkCarriageHealth _health;
    private GameObject _promptInstance;
    private Transform _promptTransform;
    private TextMeshProUGUI _label;
    private CarriageRepairLabelMode _lastMode = CarriageRepairLabelMode.Hidden;

    private void Awake()
    {
        _carriage = GetComponent<CarriageController>();
        _health = GetComponent<NetworkCarriageHealth>();
        TryResolvePrefabReference();
        InstantiatePromptIfNeeded();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_promptInstance != null)
            Destroy(_promptInstance);
    }

    private CarriageConfig ResolveConfig() => CarriageConfigUtility.Resolve(_carriage != null ? _carriage.Config : null);

    private void TryResolvePrefabReference()
    {
        if (repairUIPrefab != null)
            return;

        CarriageConfig config = ResolveConfig();
        if (config != null)
            repairUIPrefab = config.repairPromptPrefab;
    }

    private void InstantiatePromptIfNeeded()
    {
        TryResolvePrefabReference();

        if (_promptInstance != null || repairUIPrefab == null)
            return;

        _promptInstance = Instantiate(repairUIPrefab);
        _promptInstance.name = repairUIPrefab.name;
        _promptTransform = _promptInstance.transform;

        ConfigureWorldSpaceCanvas(_promptInstance.GetComponent<RectTransform>());

        DownedReviveUILabelView labelView = _promptInstance.GetComponentInChildren<DownedReviveUILabelView>(true);
        _label = labelView != null ? labelView.Label : _promptInstance.GetComponentInChildren<TextMeshProUGUI>(true);
        NormalizeLabelLayout(_label);
        _promptInstance.SetActive(false);
    }

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
        GameplayUiFonts.Apply(label);
    }

    private void LateUpdate()
    {
        if (_carriage == null || !_carriage.IsSpawned || _health == null)
        {
            ApplyMode(CarriageRepairLabelMode.Hidden);
            return;
        }

        if (_promptInstance == null)
        {
            InstantiatePromptIfNeeded();
            if (_promptInstance == null)
                return;
        }

        CarriageRepairLabelMode mode = ResolveLabelModeForLocalViewer();
        ApplyMode(mode);

        if (mode == CarriageRepairLabelMode.Hidden)
            return;

        _promptTransform.position = transform.position + offset;
        _promptTransform.rotation = Quaternion.identity;
    }

    private CarriageRepairLabelMode ResolveLabelModeForLocalViewer()
    {
        if (_health == null || !_health.IsBroken)
            return CarriageRepairLabelMode.Hidden;

        CarriageConfig config = ResolveConfig();
        if (config == null)
            return CarriageRepairLabelMode.Hidden;

        if (_health.IsRepairActive)
            return CarriageRepairLabelMode.RepairProgress;

        NetworkPlayerHealth localAlly = ResolveLocalFightingPlayer();
        if (localAlly == null)
            return CarriageRepairLabelMode.Hidden;

        float dist = Vector2.Distance(localAlly.transform.position, transform.position);
        if (dist <= config.repairPromptRadius)
            return CarriageRepairLabelMode.AllyPressE;

        float visibilityRadius = config.GetRepairLabelVisibilityRadius();
        if (dist <= visibilityRadius)
            return CarriageRepairLabelMode.AllyApproach;

        return CarriageRepairLabelMode.Hidden;
    }

    private static NetworkPlayerHealth ResolveLocalFightingPlayer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return null;

        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null)
            return null;

        return localPlayer.TryGetComponent(out NetworkPlayerHealth health) && health.CanFight
            ? health
            : null;
    }

    private void ApplyMode(CarriageRepairLabelMode mode)
    {
        bool visible = mode != CarriageRepairLabelMode.Hidden;
        SetVisible(visible);

        if (!visible || _label == null)
        {
            _lastMode = mode;
            return;
        }

        CarriageConfig config = ResolveConfig();
        if (mode == CarriageRepairLabelMode.RepairProgress || mode != _lastMode)
        {
            float progress = _health.RepairProgress;
            _label.text = mode switch
            {
                CarriageRepairLabelMode.AllyApproach => config.GetApproachText(),
                CarriageRepairLabelMode.AllyPressE => config.GetPressEText(),
                CarriageRepairLabelMode.RepairProgress => config.FormatRepairProgressText(
                    Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100)),
                _ => string.Empty
            };
        }

        _lastMode = mode;
    }

    private void SetVisible(bool visible)
    {
        if (_promptInstance != null)
            _promptInstance.SetActive(visible);
    }
}
