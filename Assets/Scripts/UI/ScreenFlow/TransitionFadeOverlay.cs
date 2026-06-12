using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay persistente (fade + loading) para todas as transições de cena.
/// Singleton DDOL — inicializado no Bootstrap ou ao abrir Menu2.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class TransitionFadeOverlay : Singleton<TransitionFadeOverlay>
{
    public bool IsLoadingVisible { get; private set; }
    public float LoadingProgress { get; private set; }

    public event Action<bool> OnLoadingVisibilityChanged;

    private Canvas _canvas;
    private Image _fadeImage;
    private GameObject _loadingRoot;
    private Image _progressFill;
    private TMP_Text _statusText;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        var go = new GameObject(nameof(TransitionFadeOverlay));
        go.AddComponent<TransitionFadeOverlay>();
    }

    protected override void Awake()
    {
        base.Awake();
        BuildOverlay();
    }

    public IEnumerator FadeOut(float duration)
    {
        EnsureFadeReady();
        _fadeImage.raycastTarget = true;

        float t = 0f;
        Color c = _fadeImage.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            _fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        _fadeImage.color = c;
    }

    public IEnumerator FadeIn(float duration)
    {
        if (_fadeImage == null)
            yield break;

        float t = duration;
        Color c = _fadeImage.color;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / duration);
            _fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        _fadeImage.color = c;
        _fadeImage.raycastTarget = false;
    }

    public void ShowLoading()
    {
        EnsureOverlayBuilt();
        ResetLoadingProgress();
        _loadingRoot.SetActive(true);
        _loadingRoot.transform.SetAsLastSibling();

        if (!IsLoadingVisible)
        {
            IsLoadingVisible = true;
            OnLoadingVisibilityChanged?.Invoke(true);
        }
    }

    public void HideLoading()
    {
        if (_loadingRoot != null)
            _loadingRoot.SetActive(false);

        if (IsLoadingVisible)
        {
            IsLoadingVisible = false;
            OnLoadingVisibilityChanged?.Invoke(false);
        }
    }

    public void SetLoadingProgress(float progress)
    {
        LoadingProgress = Mathf.Clamp01(progress);

        if (_progressFill != null)
            LoadingProgressUtility.SetProgress(_progressFill, LoadingProgress);

        if (_statusText != null)
            _statusText.text = $"Carregando... {LoadingProgress:P0}";
    }

    public void ResetLoadingProgress() => SetLoadingProgress(0f);

    public void ResetOverlay()
    {
        HideLoading();
        ResetFade();
    }

    public void ResetFade()
    {
        if (_fadeImage == null)
            return;

        Color c = _fadeImage.color;
        c.a = 0f;
        _fadeImage.color = c;
        _fadeImage.raycastTarget = false;
    }

    private void BuildOverlay()
    {
        if (_fadeImage != null)
            return;

        GameObject root = new GameObject("OverlayRoot");
        root.transform.SetParent(transform, false);

        _canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(root.transform, "TransitionOverlay");
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 1000;

        GameObject fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeGo.transform.SetParent(_canvas.transform, false);
        RectTransform fadeRect = fadeGo.GetComponent<RectTransform>();
        ScreenFlowPlaceholderFactory.StretchFull(fadeRect);

        _fadeImage = fadeGo.GetComponent<Image>();
        LoadingProgressUtility.ApplySolidSprite(_fadeImage);
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;

        _loadingRoot = ScreenFlowPlaceholderFactory.CreatePanel(
            _canvas.transform, "Loading", new Color(0.04f, 0.05f, 0.1f, 0.98f));
        _statusText = ScreenFlowPlaceholderFactory.CreateText(_loadingRoot.transform, "Carregando... 0%",
            48, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -40f), new Vector2(300f, 40f));
        _progressFill = LoadingProgressUtility.CreateProgressBar(
            _loadingRoot.transform,
            new Vector2(0f, -120f),
            new Vector2(640f, 24f),
            LoadingProgressUtility.DefaultTrackColor,
            LoadingProgressUtility.DefaultFillColor);
        _loadingRoot.SetActive(false);
    }

    private void EnsureOverlayBuilt()
    {
        if (_fadeImage == null)
            BuildOverlay();
    }

    private void EnsureFadeReady()
    {
        EnsureOverlayBuilt();
        LoadingProgressUtility.ApplySolidSprite(_fadeImage);
    }
}
