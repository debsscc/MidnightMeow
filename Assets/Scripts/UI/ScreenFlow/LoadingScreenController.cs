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
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private void Awake()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Loading2")
            fallbackNextRouteId = SceneFlowRouteIds.Loading2ToGameplay;

        if (buildPlaceholderIfMissing && statusText == null)
            BuildPlaceholderUI();
    }

    private void Start()
    {
        StartCoroutine(RunLoadingRoutine());
    }

    private IEnumerator RunLoadingRoutine()
    {
        string nextRoute = string.IsNullOrEmpty(GameSessionContext.PendingRouteId)
            ? fallbackNextRouteId
            : GameSessionContext.PendingRouteId;

        if (statusText != null)
            statusText.text = "Carregando...";

        float timer = 0f;
        while (timer < minimumDisplaySeconds)
        {
            timer += Time.unscaledDeltaTime;
            if (statusText != null)
                statusText.text = $"Carregando... {Mathf.Clamp01(timer / minimumDisplaySeconds):P0}";
            yield return null;
        }

        GameSessionContext.PendingRouteId = string.Empty;

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

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "LoadingPanel", new Color(0.04f, 0.04f, 0.06f, 1f));

        nixPlaceholder = CreateArtPlaceholder(panel.transform, "NixPlaceholder", new Color(0.3f, 0.55f, 0.95f, 0.85f),
            new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(-180f, -220f), new Vector2(180f, 220f));
        coraPlaceholder = CreateArtPlaceholder(panel.transform, "CoraPlaceholder", new Color(0.95f, 0.35f, 0.45f, 0.85f),
            new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(-180f, -220f), new Vector2(180f, 220f));

        statusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Carregando...", 32, TextAlignmentOptions.Bottom, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400f, 40f), new Vector2(400f, 120f));
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
