using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Label world-space acima da carruagem. Reage a <see cref="CarriageState"/> (escolta)
/// e, quando Broken, aos prompts de conserto (aproximar → E → fique na área / %).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CarriageController), typeof(NetworkCarriageHealth))]
public class CarriageRepairWorldUI : MonoBehaviour
{
    public enum CarriageLabelMode
    {
        Hidden,
        EscortIdle,
        EscortMoving,
        EscortBroken,
        AllyApproach,
        AllyPressE,
        StayInArea,
        RepairProgress
    }

    private const float HealthBarLabelClearance = 0.55f;

    [SerializeField] private GameObject repairUIPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.85f, 0f);

    public void SetOffset(Vector3 worldOffset) => offset = worldOffset;

    private static readonly List<Vector2> ZoneBuffer = new List<Vector2>(CarriageRepairSession.MaxZones);

    private CarriageController _carriage;
    private NetworkCarriageHealth _health;
    private NetworkCarriageRepairManager _repairManager;
    private EnemyHealthBarDisplay _healthBar;
    private GameObject _promptInstance;
    private Transform _promptTransform;
    private TextMeshProUGUI _label;
    private CarriageLabelMode _lastMode = CarriageLabelMode.Hidden;
    private bool _subscribedToState;
    private bool _subscribedToRepair;

    private void Awake()
    {
        _carriage = GetComponent<CarriageController>();
        _health = GetComponent<NetworkCarriageHealth>();
        _repairManager = GetComponent<NetworkCarriageRepairManager>();
        _healthBar = GetComponent<EnemyHealthBarDisplay>();
        TryResolvePrefabReference();
        InstantiatePromptIfNeeded();
        SetVisible(false);
    }

    private void OnEnable()
    {
        TrySubscribeCarriageState();
        TrySubscribeRepairProgress();
    }

    private void OnDisable()
    {
        UnsubscribeCarriageState();
        UnsubscribeRepairProgress();
    }

    private void OnDestroy()
    {
        UnsubscribeCarriageState();
        UnsubscribeRepairProgress();
        if (_promptInstance != null)
            Destroy(_promptInstance);
    }

    private CarriageConfig ResolveConfig() => CarriageConfigUtility.Resolve(_carriage != null ? _carriage.Config : null);

    private void TrySubscribeCarriageState()
    {
        if (_subscribedToState || _carriage == null || !_carriage.IsSpawned)
            return;

        _carriage.CarriageStateVariable.OnValueChanged += HandleCarriageStateChanged;
        _subscribedToState = true;
    }

    private void UnsubscribeCarriageState()
    {
        if (!_subscribedToState || _carriage == null)
            return;

        _carriage.CarriageStateVariable.OnValueChanged -= HandleCarriageStateChanged;
        _subscribedToState = false;
    }

    private void TrySubscribeRepairProgress()
    {
        if (_subscribedToRepair || _repairManager == null || !_repairManager.IsSpawned)
            return;

        _repairManager.RepairProgressVariable.OnValueChanged += HandleRepairProgressChanged;
        _repairManager.RepairActiveVariable.OnValueChanged += HandleRepairActiveChanged;
        _subscribedToRepair = true;
    }

    private void UnsubscribeRepairProgress()
    {
        if (!_subscribedToRepair || _repairManager == null)
            return;

        _repairManager.RepairProgressVariable.OnValueChanged -= HandleRepairProgressChanged;
        _repairManager.RepairActiveVariable.OnValueChanged -= HandleRepairActiveChanged;
        _subscribedToRepair = false;
    }

    private void HandleCarriageStateChanged(CarriageState previous, CarriageState current) =>
        _lastMode = CarriageLabelMode.Hidden;

    private void HandleRepairProgressChanged(float previous, float current) =>
        _lastMode = CarriageLabelMode.Hidden;

    private void HandleRepairActiveChanged(bool previous, bool current) =>
        _lastMode = CarriageLabelMode.Hidden;

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
        // Sem pai: escala world fixa (igual selar / reviver).
        _promptTransform.SetParent(null, false);
        _promptTransform.localScale = Vector3.one;

        GameplayUiFonts.ConfigureWorldInteractionCanvas(_promptInstance.GetComponent<RectTransform>());

        DownedReviveUILabelView labelView = _promptInstance.GetComponentInChildren<DownedReviveUILabelView>(true);
        _label = labelView != null ? labelView.Label : _promptInstance.GetComponentInChildren<TextMeshProUGUI>(true);
        NormalizeLabelLayout(_label);
        _promptInstance.SetActive(false);
    }

    private void NormalizeLabelLayout(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        GameplayUiFonts.ApplyWorldInteraction(label);
    }

    private void LateUpdate()
    {
        if (_carriage == null || !_carriage.IsSpawned || _health == null)
        {
            ApplyMode(CarriageLabelMode.Hidden);
            return;
        }

        if (_repairManager == null)
            _repairManager = GetComponent<NetworkCarriageRepairManager>();

        if (_healthBar == null)
            _healthBar = GetComponent<EnemyHealthBarDisplay>();

        TrySubscribeCarriageState();
        TrySubscribeRepairProgress();

        if (_promptInstance == null)
        {
            InstantiatePromptIfNeeded();
            if (_promptInstance == null)
                return;
        }

        CarriageLabelMode mode = ResolveLabelModeForLocalViewer();
        ApplyMode(mode);

        if (mode == CarriageLabelMode.Hidden)
            return;

        _promptTransform.SetPositionAndRotation(ResolveLabelWorldPosition(mode), Quaternion.identity);
        _promptTransform.localScale = Vector3.one;
    }

    private Vector3 ResolveLabelWorldPosition(CarriageLabelMode mode)
    {
        Vector2 anchor = transform.position;
        Vector3 fallback = (Vector3)anchor + offset;

        bool usesRepairZones = mode == CarriageLabelMode.StayInArea || mode == CarriageLabelMode.RepairProgress;
        if (usesRepairZones &&
            _repairManager != null &&
            _repairManager.TryGetActiveSession(out CarriageRepairSession session))
        {
            session.CollectZones(ZoneBuffer);
            CarriageConfig config = ResolveConfig();
            float visualRadius = config != null
                ? config.GetRepairZoneVisualDiameter() * 0.5f
                : 1.5f;

            return CooperativeZoneLabelPlacementUtility.ResolvePosition(
                ZoneBuffer,
                visualRadius,
                anchor,
                offset,
                entityAnchorForSideChoice: anchor);
        }

        return RaiseAboveHealthBar(fallback);
    }

    private Vector3 RaiseAboveHealthBar(Vector3 candidate)
    {
        if (_healthBar == null)
            return candidate;

        Vector3 barPos = _healthBar.GetBarWorldPosition();
        float minY = barPos.y + _healthBar.GetBarHeight() * 0.5f + HealthBarLabelClearance;
        if (candidate.y < minY)
            candidate.y = minY;

        return candidate;
    }

    private CarriageLabelMode ResolveLabelModeForLocalViewer()
    {
        CarriageConfig config = ResolveConfig();
        if (config == null)
            return CarriageLabelMode.Hidden;

        // Interação e UI usam a mesma fonte: IsBroken (NetworkVariable autoritativa).
        if (_health.IsBroken)
        {
            if (_health.IsRepairActive)
            {
                float progress = _health.RepairProgress;
                // Enquanto progresso ainda é 0 no início, mostra "fique na área";
                // depois exibe a porcentagem.
                return progress > 0.001f
                    ? CarriageLabelMode.RepairProgress
                    : CarriageLabelMode.StayInArea;
            }

            NetworkPlayerHealth localAlly = ResolveLocalFightingPlayer();
            if (localAlly == null)
                return CarriageLabelMode.EscortBroken;

            float dist = Vector2.Distance(localAlly.transform.position, transform.position);
            if (dist <= config.repairPromptRadius)
                return CarriageLabelMode.AllyPressE;

            float visibilityRadius = config.GetRepairLabelVisibilityRadius();
            if (dist <= visibilityRadius)
                return CarriageLabelMode.AllyApproach;

            return CarriageLabelMode.EscortBroken;
        }

        return _carriage.CurrentState switch
        {
            CarriageState.Moving => CarriageLabelMode.EscortMoving,
            _ => CarriageLabelMode.EscortIdle
        };
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

    private void ApplyMode(CarriageLabelMode mode)
    {
        bool visible = mode != CarriageLabelMode.Hidden;
        SetVisible(visible);

        if (!visible || _label == null)
        {
            _lastMode = mode;
            return;
        }

        CarriageConfig config = ResolveConfig();
        if (mode == CarriageLabelMode.RepairProgress || mode == CarriageLabelMode.StayInArea || mode != _lastMode)
        {
            float progress = _health.RepairProgress;
            _label.text = mode switch
            {
                CarriageLabelMode.EscortIdle => config.GetEscortIdleText(),
                CarriageLabelMode.EscortMoving => config.GetEscortMovingText(),
                CarriageLabelMode.EscortBroken => config.GetEscortBrokenText(),
                CarriageLabelMode.AllyApproach => config.GetApproachText(),
                CarriageLabelMode.AllyPressE => config.GetPressEText(),
                CarriageLabelMode.StayInArea => config.GetStayInAreaText(),
                CarriageLabelMode.RepairProgress => config.FormatRepairProgressText(
                    Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100)),
                _ => string.Empty
            };
            GameplayUiFonts.ApplyWorldInteraction(_label);
        }

        _lastMode = mode;
    }

    private void SetVisible(bool visible)
    {
        if (_promptInstance != null)
            _promptInstance.SetActive(visible);
    }
}
