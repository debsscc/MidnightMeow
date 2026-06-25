using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD de objetivo da fase: buracos selados e inimigos ativos.
/// </summary>
[DisallowMultipleComponent]
public class PhaseObjectiveHud : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private bool buildTextIfMissing = true;
    [SerializeField] private float refreshInterval = 0.35f;

    private string _status = "Buracos: -/-  |  Inimigos: -";
    private float _refreshTimer;
    private float _carriageProgressPercent;

    private void Awake() => EnsureConfigured();

    private void OnEnable()
    {
        GameEvents.OnPhaseObjectiveStatusChanged += UpdateStatus;
        GameEvents.OnCarriagePathProgressChanged += HandleCarriageProgressChanged;
        RefreshFromLocalState();
    }

    private void OnDisable()
    {
        GameEvents.OnPhaseObjectiveStatusChanged -= UpdateStatus;
        GameEvents.OnCarriagePathProgressChanged -= HandleCarriageProgressChanged;
    }

    private void HandleCarriageProgressChanged(float normalizedProgress)
    {
        _carriageProgressPercent = Mathf.Clamp01(normalizedProgress) * 100f;
        RefreshFromLocalState();
    }

    private void Update()
    {
        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer < refreshInterval)
            return;

        _refreshTimer = 0f;
        RefreshFromLocalState();
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

    private void UpdateStatus(int holesSealed, int totalHoles, int enemiesAlive)
    {
        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (catalog != null && catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry) &&
            entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.KillBoss)
        {
            _status = $"Derrote o Boss  |  Inimigos: {enemiesAlive}";
        }
        else if (catalog != null && catalog.TryGetEntry(sceneName, out entry) &&
                 entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.CarriageReachEnd)
        {
            int remaining = Mathf.Max(0, totalHoles - holesSealed);
            _status = $"Carruagem: {_carriageProgressPercent:0}%  |  Buracos: {holesSealed}/{totalHoles} ({remaining} faltando)  |  Inimigos: {enemiesAlive}";
        }
        else
        {
            int remaining = Mathf.Max(0, totalHoles - holesSealed);
            _status = $"Buracos: {holesSealed}/{totalHoles} selados ({remaining} faltando)  |  Inimigos: {enemiesAlive}";
        }

        UpdateUI();
    }

    private void RefreshFromLocalState()
    {
        NetworkCarriage carriage = NetworkCarriage.Instance;
        if (carriage != null)
            _carriageProgressPercent = carriage.PathProgress * 100f;

        PhaseObjectiveStatusUtility.CountSealedHoles(out int sealedCount, out int totalCount);
        int alive = PhaseObjectiveStatusUtility.CountAliveNetworkEnemies();
        UpdateStatus(sealedCount, totalCount, alive);
    }

    private void UpdateUI()
    {
        if (text != null)
            text.text = _status;
    }
}
