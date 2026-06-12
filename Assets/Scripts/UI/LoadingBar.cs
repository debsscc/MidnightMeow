using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sincroniza a barra de loading da cena com <see cref="ScreenFlowController.LoadingProgress"/>.
/// Reinicia sempre que a tela de loading é exibida.
/// </summary>
[DisallowMultipleComponent]
public class LoadingBar : MonoBehaviour
{
    [SerializeField] private Image loadingBar;
    [SerializeField] private float fillSpeed = 2.5f;

    private Coroutine _routine;
    private float _smoothedProgress;

    private void Awake()
    {
        if (loadingBar == null)
            loadingBar = GetComponentInChildren<Image>(true);

        if (loadingBar != null)
            loadingBar = LoadingProgressUtility.EnsureFillFromLegacyImage(
                loadingBar,
                LoadingProgressUtility.DefaultTrackColor,
                LoadingProgressUtility.DefaultFillColor);

        ResetBarVisual();
    }

    private void OnEnable()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null)
            flow.OnLoadingScreenVisibilityChanged += HandleLoadingScreenVisibilityChanged;

        if (_routine == null)
            _routine = StartCoroutine(SyncWithScreenFlow());
    }

    private void OnDisable()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null)
            flow.OnLoadingScreenVisibilityChanged -= HandleLoadingScreenVisibilityChanged;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private void HandleLoadingScreenVisibilityChanged(bool visible)
    {
        if (visible)
            ResetBarVisual();
    }

    private void ResetBarVisual()
    {
        _smoothedProgress = 0f;

        if (loadingBar != null)
            LoadingProgressUtility.ResetProgress(loadingBar);
    }

    private IEnumerator SyncWithScreenFlow()
    {
        while (true)
        {
            ScreenFlowController flow = ScreenFlowController.Instance;
            if (flow != null && flow.IsLoadingScreenVisible && loadingBar != null)
            {
                float target = flow.LoadingProgress;
                _smoothedProgress = Mathf.MoveTowards(
                    _smoothedProgress,
                    target,
                    fillSpeed * Time.unscaledDeltaTime);
                LoadingProgressUtility.SetProgress(loadingBar, _smoothedProgress);
            }

            yield return null;
        }
    }
}
