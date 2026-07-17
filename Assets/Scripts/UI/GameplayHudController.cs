using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orquestra widgets de HUD de gameplay no canvas da cena.
/// Usa referências da cena/prefab quando existem; cria fallback só para o que faltar.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class GameplayHudController : MonoBehaviour
{
    public const string LayersRootName = "GameplayHudLayers";
    public const string AbilityLayerName = "AbilityHudLayer";
    public const string WaveLayerName = "WaveHudLayer";
    public const string FeedbackLayerName = "FeedbackHudLayer";
    public const string IndicatorsLayerName = "IndicatorsHudLayer";

    public const string ObjectiveLayerName = "ObjectiveHudLayer";
    public const string PauseOverlayLayerName = "PauseOverlayLayer";

    public const float ResponsiveScalerMatch = 0.5f;
    public static readonly Vector2 ResponsiveReferenceResolution = new Vector2(1920f, 1080f);

    [Header("Widgets (opcional — cena/prefab)")]
    [SerializeField] private HordeIndicator waveIndicator;
    [SerializeField] private PhaseObjectiveHud phaseObjectiveHud;
    [SerializeField] private PlayerAbilityHud abilityHud;
    [SerializeField] private PlaytestFeedbackButton feedbackButton;

    [Header("Tema (opcional)")]
    [SerializeField] private ScreenVisualTheme visualTheme;

    private RectTransform _layersRoot;

    private void Awake() => EnsureWidgets();

    public void EnsureWidgets()
    {
        if (!GameplaySceneBootstrap.IsGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            return;

        if (transform.localScale.sqrMagnitude < 0.01f)
            transform.localScale = Vector3.one;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;

        BindGameplayCamera(canvas);
        ApplyResponsiveCanvasScaler(canvas);
        EnsureLayersRoot();

        if (visualTheme == null)
            visualTheme = Resources.Load<ScreenVisualTheme>("DefaultScreenVisualTheme");

        EnsureWaveIndicator();
        EnsurePhaseObjectiveHud();
        EnsureBossHealthBarHud();
        EnsureAbilityHud(visualTheme != null ? visualTheme.abilityHudTheme : null);
        DisableOffscreenIndicators();
        EnsureFeedbackButton();
        EnsureDownedReviveTimerHud();
    }

    private void EnsureDownedReviveTimerHud()
    {
        Transform feedbackLayer = GetLayer(FeedbackLayerName);
        DownedReviveTimerHud.EnsureOnLayer(feedbackLayer, DownedPlayerConfigUtility.Resolve());
    }

    /// <summary>
    /// Garante que overlays (pause, baú, etc.) renderizem acima da HUD de habilidades.
    /// </summary>
    public static void BringOverlayToFront(Transform overlayRoot)
    {
        if (overlayRoot == null)
            return;

        GameplayHudController controller = FindFirstObjectByType<GameplayHudController>();
        if (controller != null)
        {
            controller.AttachOverlayToPauseLayer(overlayRoot);
            return;
        }

        overlayRoot.SetAsLastSibling();
    }

    public static void ApplyResponsiveCanvasScaler(Canvas canvas, float match = ResponsiveScalerMatch)
    {
        if (canvas == null)
            return;

        // Canvas com scale zero some a HUD inteira (Fase-3 chegou assim no disco).
        if (canvas.transform.localScale.sqrMagnitude < 0.01f)
            canvas.transform.localScale = Vector3.one;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        if (scaler.referenceResolution.x < 1f || scaler.referenceResolution.y < 1f)
            scaler.referenceResolution = ResponsiveReferenceResolution;

        scaler.matchWidthOrHeight = match;
    }

    private void AttachOverlayToPauseLayer(Transform overlayRoot)
    {
        EnsureLayersRoot();
        Transform pauseLayer = GetLayer(PauseOverlayLayerName);
        pauseLayer.SetAsLastSibling();

        if (overlayRoot.parent != pauseLayer)
            overlayRoot.SetParent(pauseLayer, false);

        overlayRoot.SetAsLastSibling();
    }

    private void ApplyResponsiveCanvasScaler(Canvas canvas) => ApplyResponsiveCanvasScaler(canvas, ResponsiveScalerMatch);

    private void BindGameplayCamera(Canvas canvas)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.worldCamera != null)
            return;

        Camera cam = MultiplayerCameraController.Resolve()?.MainCamera ?? Camera.main;
        if (cam != null)
            canvas.worldCamera = cam;
        else
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }

    private void EnsureLayersRoot()
    {
        if (_layersRoot != null)
            return;

        Transform existing = transform.Find(LayersRootName);
        if (existing != null)
        {
            _layersRoot = existing as RectTransform;
            _layersRoot.SetAsLastSibling();
            return;
        }

        GameObject root = new GameObject(LayersRootName, typeof(RectTransform));
        root.transform.SetParent(transform, false);
        _layersRoot = root.GetComponent<RectTransform>();
        Stretch(_layersRoot);
        _layersRoot.SetAsLastSibling();
    }

    private Transform GetLayer(string layerName)
    {
        EnsureLayersRoot();
        Transform layer = _layersRoot.Find(layerName);
        if (layer != null)
            return layer;

        GameObject layerGo = new GameObject(layerName, typeof(RectTransform));
        layerGo.transform.SetParent(_layersRoot, false);
        Stretch(layerGo.GetComponent<RectTransform>());
        return layerGo.transform;
    }

    private void EnsureWaveIndicator()
    {
        if (waveIndicator == null)
            waveIndicator = GetComponentInChildren<HordeIndicator>(true);

        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool useObjectiveHud = catalog != null &&
                               catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry) &&
                               !entry.useWaveSpawning;

        if (waveIndicator != null)
        {
            waveIndicator.gameObject.SetActive(!useObjectiveHud);
            if (!useObjectiveHud)
                waveIndicator.EnsureConfigured();
            return;
        }

        if (useObjectiveHud)
            return;

        GameObject go = new GameObject("HordeIndicator", typeof(RectTransform), typeof(HordeIndicator));
        go.transform.SetParent(GetLayer(WaveLayerName), false);
        waveIndicator = go.GetComponent<HordeIndicator>();
        waveIndicator.EnsureConfigured();
    }

    private void EnsurePhaseObjectiveHud()
    {
        if (phaseObjectiveHud == null)
            phaseObjectiveHud = GetComponentInChildren<PhaseObjectiveHud>(true);

        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isKillBoss = catalog != null &&
                          catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry) &&
                          entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.KillBoss;

        // Fase-3: sem HUD de buracos/poças — o foco é o boss (barra cinematográfica).
        if (isKillBoss)
        {
            if (phaseObjectiveHud != null)
                phaseObjectiveHud.gameObject.SetActive(false);
            return;
        }

        bool useObjectiveHud = catalog == null ||
                               !catalog.TryGetEntry(sceneName, out entry) ||
                               !entry.useWaveSpawning;

        if (!useObjectiveHud)
        {
            if (phaseObjectiveHud != null)
                phaseObjectiveHud.gameObject.SetActive(false);
            return;
        }

        if (phaseObjectiveHud != null)
        {
            phaseObjectiveHud.EnsureConfigured();
            return;
        }

        GameObject go = new GameObject("PhaseObjectiveHud", typeof(RectTransform), typeof(PhaseObjectiveHud));
        go.transform.SetParent(GetLayer(ObjectiveLayerName), false);
        phaseObjectiveHud = go.GetComponent<PhaseObjectiveHud>();
        phaseObjectiveHud.EnsureConfigured();
    }

    private void EnsureBossHealthBarHud()
    {
        if (!BossPhaseUtility.IsKillBossPhaseActive())
        {
            BossHealthBarHud existingInactive = GetComponentInChildren<BossHealthBarHud>(true);
            if (existingInactive != null)
                existingInactive.gameObject.SetActive(false);
            return;
        }

        Transform objectiveLayer = GetLayer(ObjectiveLayerName);
        BossHealthBarHud hud = GetComponentInChildren<BossHealthBarHud>(true);
        if (hud == null)
            hud = BossHealthBarHud.EnsureOnLayer(objectiveLayer);
        else if (hud.transform.parent != objectiveLayer)
            hud.transform.SetParent(objectiveLayer, false);

        hud.gameObject.SetActive(true);
        hud.transform.SetAsLastSibling();
    }

    private void EnsureAbilityHud(PlayerAbilityHudTheme theme)
    {
        if (!GameplaySceneBootstrap.IsGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            return;

        Transform abilityLayer = GetLayer(AbilityLayerName);

        if (abilityHud == null)
            abilityHud = GetComponentInChildren<PlayerAbilityHud>(true);

        if (abilityHud != null)
        {
            if (abilityHud.transform.parent != abilityLayer)
                abilityHud.transform.SetParent(abilityLayer, false);

            if (!abilityHud.gameObject.activeSelf)
                abilityHud.gameObject.SetActive(true);
            if (!abilityHud.enabled)
                abilityHud.enabled = true;
            if (theme != null)
                abilityHud.ApplyTheme(theme);
            abilityHud.EnsureBuilt();
            return;
        }

        // Fallback procedural só em cenas de fase — nunca em Menu/Victory.
        abilityHud = PlayerAbilityHud.CreateUnder(abilityLayer, theme);
    }

    private void DisableOffscreenIndicators()
    {
        OffscreenEnemyIndicator[] indicators = GetComponentsInChildren<OffscreenEnemyIndicator>(true);
        for (int i = 0; i < indicators.Length; i++)
        {
            if (indicators[i] != null)
                indicators[i].gameObject.SetActive(false);
        }
    }

    private void EnsureFeedbackButton()
    {
        if (feedbackButton == null)
            feedbackButton = GetComponentInChildren<PlaytestFeedbackButton>(true);

        if (feedbackButton != null)
            return;

        GameObject go = new GameObject("PlaytestFeedbackButton", typeof(RectTransform), typeof(PlaytestFeedbackButton));
        go.transform.SetParent(GetLayer(FeedbackLayerName), false);
        feedbackButton = go.GetComponent<PlaytestFeedbackButton>();
        if (visualTheme != null)
            feedbackButton.ApplyTheme(visualTheme);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
