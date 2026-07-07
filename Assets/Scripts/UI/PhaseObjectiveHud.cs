// ----------------------------------------------------------------
// CRIADO POR: Pedro Caurio
// DESCRIÇÃO: HUD de objetivo da fase: buracos selados e inimigos ativos.
// ---------------------------------------------------------------- 


using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PhaseObjectiveHud : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private bool buildTextIfMissing = true;

    private string _status = "Buracos: -/-  |  Inimigos: -";
    private float _carriageProgressPercent;
    private int _holesSealed;
    private int _totalHoles;
    private int _enemiesAlive;
    private NetworkCarriage _subscribedCarriage;

    private void Awake() => EnsureConfigured();

    private void OnEnable()
    {
        GameEvents.OnPhaseObjectiveStatusChanged += HandleObjectiveStatusChanged;
        NetworkCarriage.OnInstanceAvailable += HandleCarriageAvailable;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        if (PhaseObjectiveStatusUtility.HasNetworkObjectiveStatus)
            _enemiesAlive = PhaseObjectiveStatusUtility.CachedEnemiesAlive;

        PhaseObjectiveStatusUtility.CountSealedHoles(out _holesSealed, out _totalHoles);

        TrySubscribeCarriage(NetworkCarriage.Instance);
        RebuildStatusText();
    }

    private void OnDisable()
    {
        GameEvents.OnPhaseObjectiveStatusChanged -= HandleObjectiveStatusChanged;
        NetworkCarriage.OnInstanceAvailable -= HandleCarriageAvailable;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        UnsubscribeCarriage();
    }

    private void HandleCarriageAvailable(NetworkCarriage carriage) => TrySubscribeCarriage(carriage);

    private void TrySubscribeCarriage(NetworkCarriage carriage)
    {
        if (carriage == null || carriage == _subscribedCarriage)
            return;

        UnsubscribeCarriage();
        _subscribedCarriage = carriage;
        _subscribedCarriage.PathProgressChanged += HandleCarriageProgressChanged;
        HandleCarriageProgressChanged(_subscribedCarriage.PathProgress);
    }

    private void UnsubscribeCarriage()
    {
        if (_subscribedCarriage == null)
            return;

        _subscribedCarriage.PathProgressChanged -= HandleCarriageProgressChanged;
        _subscribedCarriage = null;
    }

    private void HandleLocaleChanged(Locale _) => RebuildStatusText();

    private void HandleObjectiveStatusChanged(int holesSealed, int totalHoles, int enemiesAlive)
    {
        _holesSealed = holesSealed;
        _totalHoles = totalHoles;
        _enemiesAlive = enemiesAlive;
        RebuildStatusText();
    }

    private void HandleCarriageProgressChanged(float normalizedProgress)
    {
        _carriageProgressPercent = Mathf.Clamp01(normalizedProgress) * 100f;
        RebuildStatusText();
    }

    public void EnsureConfigured()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (text == null)
            text = GetComponent<Text>();

        EnsureRectTransform();
        if (text == null && buildTextIfMissing)
            text = CreateFallbackText();

        UpdateUI();
    }

    private void EnsureRectTransform()
    {
        RectTransform rt = transform as RectTransform;
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(900f, 48f);
    }

    private Text CreateFallbackText()
    {
        GameObject textGo = new GameObject("PhaseObjectiveText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text label = textGo.GetComponent<Text>();
        label.alignment = TextAnchor.UpperCenter;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 22;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.text = _status;
        return label;
    }

    private void RebuildStatusText()
    {
        bool pt = IsPortuguese();
        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (catalog != null && catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry) &&
            entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.KillBoss)
        {
            _status = pt
                ? $"Derrote o Boss  |  Inimigos: {_enemiesAlive}"
                : $"Defeat the Boss  |  Enemies: {_enemiesAlive}";
        }
        else if (catalog != null && catalog.TryGetEntry(sceneName, out entry) &&
                 entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.CarriageReachEnd)
        {
            int remaining = Mathf.Max(0, _totalHoles - _holesSealed);
            _status = pt
                ? $"Carruagem: {_carriageProgressPercent:0}%  |  Buracos: {_holesSealed}/{_totalHoles} ({remaining} faltando)  |  Inimigos: {_enemiesAlive}"
                : $"Carriage: {_carriageProgressPercent:0}%  |  Holes: {_holesSealed}/{_totalHoles} ({remaining} left)  |  Enemies: {_enemiesAlive}";
        }
        else
        {
            int remaining = Mathf.Max(0, _totalHoles - _holesSealed);
            _status = pt
                ? $"Buracos: {_holesSealed}/{_totalHoles} selados ({remaining} faltando)  |  Inimigos: {_enemiesAlive}"
                : $"Holes: {_holesSealed}/{_totalHoles} sealed ({remaining} left)  |  Enemies: {_enemiesAlive}";
        }

        UpdateUI();
    }

    private static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateUI()
    {
        if (text != null)
            text.text = _status;
    }
}
