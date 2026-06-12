using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de carregamento reutilizável. Lê a rota destino de <see cref="GameSessionContext.PendingRouteId"/>.
/// </summary>
[DisallowMultipleComponent]
public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private string fallbackNextRouteId = SceneFlowRouteIds.Loading1ToPreparation;
    [SerializeField] private float minimumDisplaySeconds = 3f;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image nixPlaceholder;
    [SerializeField] private Image coraPlaceholder;
    [SerializeField] private Image progressFill;
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private void Awake()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Loading2")
            fallbackNextRouteId = SceneFlowRouteIds.Loading2ToGameplay;

        if (buildPlaceholderIfMissing && statusText == null)
            BuildPlaceholderUI();

        EnsureProgressUi();
    }

    private void Start()
    {
        HideLegacyLoadingContent();
        StartCoroutine(RunLoadingRoutine());
    }

    private IEnumerator RunLoadingRoutine()
    {
        string nextRoute = string.IsNullOrEmpty(GameSessionContext.PendingRouteId)
            ? fallbackNextRouteId
            : GameSessionContext.PendingRouteId;

        ResetProgressUi();
        yield return null;

        float timer = 0f;
        while (timer < minimumDisplaySeconds)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / minimumDisplaySeconds);
            UpdateProgressUi(progress, $"Carregando... {progress:P0}");
            yield return null;
        }

        UpdateProgressUi(1f, "Carregando... 100%");

        GameSessionContext.PendingRouteId = string.Empty;

        if (nextRoute == SceneFlowRouteIds.Loading2ToGameplay)
        {
            yield return GameplaySessionStarter.EnsureReadyForGameplay();

            if (NetworkSceneSyncUtility.IsNetworkClientAwaitingHost)
            {
                string gameplayScene = string.IsNullOrEmpty(GameSessionContext.ActiveGameplaySceneName)
                    ? "Fase-1"
                    : GameSessionContext.ActiveGameplaySceneName;

                if (statusText != null)
                    statusText.text = "Aguardando host iniciar partida...";

                yield return NetworkSceneSyncUtility.WaitForActiveScene(gameplayScene);
                yield break;
            }

            if (statusText != null)
                statusText.text = "Iniciando partida...";

            ScreenFlowStateMachine.EnterGameplay();
            yield break;
        }

        if (ShouldWaitForHostSceneSync(nextRoute))
        {
            if (statusText != null)
                statusText.text = "Aguardando host...";
            yield break;
        }

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(nextRoute);
        else
            ScreenFlowController.Instance?.RequestRoute(nextRoute);
    }

    private static bool ShouldWaitForHostSceneSync(string routeId)
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null || !flow.TryGetRouteLoadKind(routeId, out SceneLoadKind loadKind))
            return false;

        if (loadKind != SceneLoadKind.NetcodeHost)
            return false;

        return Unity.Netcode.NetworkManager.Singleton != null
               && Unity.Netcode.NetworkManager.Singleton.IsListening
               && !Unity.Netcode.NetworkManager.Singleton.IsServer;
    }

    private void HideLegacyLoadingContent()
    {
        Canvas ownedCanvas = GetComponentInChildren<Canvas>(true);
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas.gameObject.name == "FadeManager")
                continue;

            if (ownedCanvas != null && canvas == ownedCanvas)
                continue;

            canvas.gameObject.SetActive(false);
        }
    }

    private void EnsureProgressUi()
    {
        if (progressFill != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
            return;

        Transform panel = canvas.transform.childCount > 0 ? canvas.transform.GetChild(0) : canvas.transform;
        progressFill = CreateProgressBar(panel);
    }

    private void ResetProgressUi()
    {
        if (progressFill != null)
        {
            ConfigureFilledImage(progressFill);
            progressFill.fillAmount = 0f;
        }

        UpdateProgressUi(0f, "Carregando... 0%");
    }

    private void UpdateProgressUi(float progress, string label)
    {
        if (statusText != null)
            statusText.text = label;

        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(progress);
    }

    private static void ConfigureFilledImage(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private static Image CreateProgressBar(Transform parent)
    {
        GameObject trackGo = new GameObject("ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackGo.transform.SetParent(parent, false);
        RectTransform trackRt = trackGo.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0.5f, 0f);
        trackRt.anchorMax = new Vector2(0.5f, 0f);
        trackRt.pivot = new Vector2(0.5f, 0f);
        trackRt.anchoredPosition = new Vector2(0f, 80f);
        trackRt.sizeDelta = new Vector2(640f, 18f);
        trackGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

        GameObject fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(trackGo.transform, false);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        ScreenFlowPlaceholderFactory.StretchFull(fillRt);
        Image fill = fillGo.GetComponent<Image>();
        fill.color = new Color(0.85f, 0.2f, 0.2f, 1f);
        ConfigureFilledImage(fill);
        fill.fillAmount = 0f;
        return fill;
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        canvas.sortingOrder = 300;
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "LoadingPanel", new Color(0.04f, 0.04f, 0.06f, 1f));

        nixPlaceholder = CreateArtPlaceholder(panel.transform, "NixPlaceholder", new Color(0.3f, 0.55f, 0.95f, 0.85f),
            new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(-180f, -220f), new Vector2(180f, 220f));
        coraPlaceholder = CreateArtPlaceholder(panel.transform, "CoraPlaceholder", new Color(0.95f, 0.35f, 0.45f, 0.85f),
            new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(-180f, -220f), new Vector2(180f, 220f));

        statusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Carregando...", 32, TextAlignmentOptions.Bottom, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400f, 110f), new Vector2(400f, 190f));

        progressFill = CreateProgressBar(panel.transform);
    }

    private static Image CreateArtPlaceholder(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }
}
