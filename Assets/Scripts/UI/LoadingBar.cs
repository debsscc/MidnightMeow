using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingBar : MonoBehaviour
{
    public Image loadingBar;
    public float fillSpeed = 0.1f;

    private void Start()
    {
        if (loadingBar == null)
            return;

        loadingBar.fillAmount = 0f;
        StartCoroutine(UpdateLoadingBar());
    }

    private IEnumerator UpdateLoadingBar()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow == null)
            yield break;

        while (flow.CurrentAsyncLoad == null)
            yield return null;

        AsyncOperation asyncLoad = flow.CurrentAsyncLoad;
        while (asyncLoad != null && asyncLoad.progress < 0.9f)
        {
            float target = asyncLoad.progress / 0.9f;
            loadingBar.fillAmount = Mathf.MoveTowards(loadingBar.fillAmount, target, fillSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        while (loadingBar.fillAmount < 1f)
        {
            loadingBar.fillAmount = Mathf.MoveTowards(loadingBar.fillAmount, 1f, fillSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
    }
}
