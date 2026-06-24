/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Tela de loading reutilizável — progresso mínimo e troca de cena por rota.
---------------------------------------------------------------- */

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private string fallbackNextRouteId = SceneFlowRouteIds.Loading1ToPreparation;
    [SerializeField] private float minimumDisplaySeconds = 7f;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RectTransform progressTrack;
    [SerializeField] private Image progressFill;
    [SerializeField] private RectTransform progressFollower;
    [SerializeField] private float followerYOffset = 72f;
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private bool _contentReady;

    private void Awake()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Loading2")
            fallbackNextRouteId = SceneFlowRouteIds.Loading2ToGameplay;

        TryAutoResolveReferences();

        if (buildPlaceholderIfMissing && statusText == null && progressFill == null)
            BuildPlaceholderUI();

        EnsureProgressUi();

        if (progressFollower == null)
        {
            GameObject followerGo = GameObject.Find("Character_loading");
            if (followerGo != null)
                progressFollower = followerGo.GetComponent<RectTransform>();
        }

        EnsureFollowerParentedToTrack();
        EnsureCanvasOnTop();
        HandoffTransitionOverlay();
        ResetProgressUi();
    }

    private void HandoffTransitionOverlay()
    {
        TransitionFadeOverlay overlay = TransitionFadeOverlay.Instance;
        if (overlay == null)
            return;

        float progress = overlay.LoadingProgress;
        overlay.HandoffToDedicatedLoadingScene(progress);
    }

    private void Start()
    {
        HideLegacyLoadingContent();
        ScreenFlowSceneReadiness.MarkReadyIfPending(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        StartCoroutine(RunLoadingRoutine());
    }

    private void EnsureCanvasOnTop()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = 400;
    }

    private IEnumerator RunLoadingRoutine()
    {
        string nextRoute = string.IsNullOrEmpty(GameSessionContext.PendingRouteId)
            ? fallbackNextRouteId
            : GameSessionContext.PendingRouteId;

        ResetProgressUi();
        _contentReady = false;
        StartCoroutine(PrepareContentRoutine(nextRoute, () => _contentReady = true));

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float timeProgress = minimumDisplaySeconds > 0f
                ? Mathf.Clamp01(elapsed / minimumDisplaySeconds)
                : 1f;

            float displayProgress = timeProgress;
            if (!_contentReady && timeProgress >= 1f)
                displayProgress = 0.99f;

            UpdateProgressUi(displayProgress, UiLocalization.FormatLoadingProgress(displayProgress));

            if (timeProgress >= 1f && _contentReady)
                break;

            yield return null;
        }

        UpdateProgressUi(1f, UiLocalization.FormatLoadingProgress(1f));
        GameSessionContext.PendingRouteId = string.Empty;
        ProceedAfterLoadingComplete(nextRoute);
    }

    private IEnumerator PrepareContentRoutine(string nextRoute, Action onReady)
    {
        if (nextRoute == SceneFlowRouteIds.Loading2ToGameplay)
        {
            yield return GameplaySessionStarter.EnsureReadyForGameplay();

            if (NetworkSceneSyncUtility.IsNetworkClientAwaitingHost)
            {
                string gameplayScene = string.IsNullOrEmpty(GameSessionContext.ActiveGameplaySceneName)
                    ? "Fase-1"
                    : GameSessionContext.ActiveGameplaySceneName;

                if (statusText != null)
                    statusText.text = UiLocalization.Get("loading.wait_host", "Aguardando host iniciar partida...");

                yield return NetworkSceneSyncUtility.WaitForActiveScene(gameplayScene);
                onReady?.Invoke();
                yield break;
            }
        }

        onReady?.Invoke();
    }

    private void ProceedAfterLoadingComplete(string nextRoute)
    {
        if (nextRoute == SceneFlowRouteIds.Loading2ToGameplay)
        {
            if (NetworkSceneSyncUtility.IsNetworkClientAwaitingHost)
                return;

            if (statusText != null)
                statusText.text = UiLocalization.Get("loading.starting", "Iniciando partida...");

            ScreenFlowStateMachine.EnterGameplay();
            return;
        }

        if (ShouldWaitForHostSceneSync(nextRoute))
        {
            if (statusText != null)
                statusText.text = UiLocalization.Get("loading.wait_host_sync", "Aguardando host...");
            return;
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
        TransitionFadeOverlay overlay = TransitionFadeOverlay.Instance;
        if (overlay != null)
            overlay.HideLoading();

        Canvas ownedCanvas = ResolveOwnedLoadingCanvas();
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas.GetComponentInParent<TransitionFadeOverlay>() != null)
                continue;

            if (canvas.gameObject.name == "FadeManager")
                continue;

            if (ownedCanvas != null && canvas == ownedCanvas)
                continue;

            canvas.gameObject.SetActive(false);
        }
    }

    private Canvas ResolveOwnedLoadingCanvas()
    {
        if (progressTrack != null)
        {
            Canvas trackCanvas = progressTrack.GetComponentInParent<Canvas>();
            if (trackCanvas != null)
                return trackCanvas;
        }

        if (statusText != null)
        {
            Canvas statusCanvas = statusText.GetComponentInParent<Canvas>();
            if (statusCanvas != null)
                return statusCanvas;
        }

        GameObject canvasUi = GameObject.Find("Canvas_UI");
        if (canvasUi != null && canvasUi.TryGetComponent(out Canvas namedCanvas))
            return namedCanvas;

        return GetComponentInChildren<Canvas>(true);
    }

    private void TryAutoResolveReferences()
    {
        if (statusText == null)
            statusText = FindTmpByName("Text_Loading") ?? FindTmpByName("StatusText");

        if (progressTrack == null)
        {
            GameObject trackGo = GameObject.Find("ProgressTrack");
            if (trackGo != null)
                progressTrack = trackGo.GetComponent<RectTransform>();
        }

        if (progressFill == null && progressTrack != null)
            progressFill = LoadingProgressUtility.ResolveOrCreateFill(progressTrack);

        if (progressFill != null && progressTrack == null)
            progressTrack = progressFill.transform.parent as RectTransform;

        if (progressFollower == null)
        {
            GameObject followerGo = GameObject.Find("Character_loading");
            if (followerGo != null)
                progressFollower = followerGo.GetComponent<RectTransform>();
        }

        EnsureFollowerParentedToTrack();
    }

    private void EnsureFollowerParentedToTrack()
    {
        if (progressTrack == null || progressFollower == null)
            return;

        if (progressFollower.parent == progressTrack)
            return;

        progressFollower.SetParent(progressTrack, false);
        progressFollower.anchorMin = new Vector2(0.5f, 0.5f);
        progressFollower.anchorMax = new Vector2(0.5f, 0.5f);
        progressFollower.pivot = new Vector2(0.5f, 0f);
    }

    private void EnsureProgressUi()
    {
        if (progressFill != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
            return;

        Transform existingTrack = canvas.transform.Find("ProgressTrack");
        if (existingTrack != null)
        {
            progressTrack = existingTrack as RectTransform;
            progressFill = LoadingProgressUtility.ResolveOrCreateFill(existingTrack);
            return;
        }

        progressFill = LoadingProgressUtility.CreateBottomProgressBar(
            canvas.transform,
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Loading1" ? 130f : LoadingProgressUtility.BottomBarCenterY);
        progressTrack = progressFill != null ? progressFill.transform.parent as RectTransform : null;
    }

    private void ResetProgressUi()
    {
        if (progressFill != null)
            LoadingProgressUtility.ResetProgress(progressFill);

        UpdateProgressUi(0f, UiLocalization.FormatLoadingProgress(0f));
    }

    private void UpdateProgressUi(float progress, string label)
    {
        if (statusText != null)
            statusText.text = label;

        if (progressFill != null)
            LoadingProgressUtility.SetProgress(progressFill, progress);

        if (progressTrack != null && progressFollower != null)
            LoadingProgressUtility.SetFollowerAlongTrack(progressTrack, progressFollower, progress, followerYOffset);
    }

    private static TMP_Text FindTmpByName(string objectName)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].gameObject.name == objectName)
                return texts[i];
        }

        return null;
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        canvas.overrideSorting = true;
        canvas.sortingOrder = 400;
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "LoadingPanel", new Color(0.04f, 0.04f, 0.06f, 1f));

        statusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, UiLocalization.FormatLoadingProgress(0f), 32, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-400f, LoadingProgressUtility.BottomStatusTextMinY),
            new Vector2(400f, LoadingProgressUtility.BottomStatusTextMaxY));

        progressFill = LoadingProgressUtility.CreateBottomProgressBar(panel.transform);
        progressTrack = progressFill != null ? progressFill.transform.parent as RectTransform : null;
    }
}
