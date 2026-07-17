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
    [SerializeField] private float minimumDisplaySeconds = 2.5f;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private RectTransform progressTrack;
    [SerializeField] private Image progressFill;
    [SerializeField] private RectTransform progressFollower;
    [SerializeField] private float followerYOffset = 72f;

    private bool _contentReady;

    private void Awake()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Loading2")
            fallbackNextRouteId = SceneFlowRouteIds.Loading2ToGameplay;

        TryAutoResolveReferences();
        EnsureProgressUi();

        if (progressFollower == null)
        {
            GameObject followerGo = GameObject.Find("Character_loading");
            if (followerGo != null)
                progressFollower = followerGo.GetComponent<RectTransform>();
        }

        EnsureFollowerParentedToTrack();
        EnsureCanvasOnTop();
        ResetProgressUi();
    }

    private void ReleaseTransitionOverlayLoading()
    {
        TransitionFadeOverlay overlay = TransitionFadeOverlay.Instance;
        overlay?.HideLoading();
    }

    private void Start()
    {
        ReleaseTransitionOverlayLoading();
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
            StartCoroutine(WaitForHostSceneSyncRoutine(nextRoute));
            return;
        }

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(nextRoute);
        else
            ScreenFlowController.Instance?.RequestRoute(nextRoute);
    }

    /// <summary>
    /// Cliente: mantém a UI oficial de Loading1/2 até o NGO ativar a cena do host
    /// (sem reabrir o overlay placeholder DDOL).
    /// </summary>
    private IEnumerator WaitForHostSceneSyncRoutine(string nextRoute)
    {
        if (statusText != null)
            statusText.text = UiLocalization.Get("loading.wait_host_sync", "Aguardando host...");

        UpdateProgressUi(0.99f, UiLocalization.FormatLoadingProgress(0.99f));

        string targetScene = ResolveRouteSceneName(nextRoute);
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning($"[LoadingScreen] Rota '{nextRoute}' sem cena destino; cliente não pode aguardar sync NGO.");
            yield break;
        }

        TransitionFadeOverlay.Instance?.HideLoading();
        yield return NetworkSceneSyncUtility.WaitForActiveScene(targetScene, fadeInOnArrival: true);
    }

    private static string ResolveRouteSceneName(string routeId)
    {
        if (routeId == SceneFlowRouteIds.Loading1ToPreparation)
            return "Preparation";

        if (routeId == SceneFlowRouteIds.Loading2ToGameplay)
        {
            return string.IsNullOrEmpty(GameSessionContext.ActiveGameplaySceneName)
                ? "Fase-1"
                : GameSessionContext.ActiveGameplaySceneName;
        }

        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null && flow.TryGetRouteSceneName(routeId, out string sceneName))
            return sceneName;

        return null;
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
}
