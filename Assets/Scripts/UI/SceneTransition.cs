///* ----------------------------------------------------------------
// AUTOR: Débora Carvalho
// DATA: 2026-04-01
//DESCRIÇÃO: Gerenciador de transições entre cenas com fade e loading screen opcional. 
// Permite controle centralizado de transições, desacoplando a lógica de mudança de cena do restante do código.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : Singleton<SceneTransition>
{
    public float fadeTime = 1f;
    public float minLoadingTime = 2f;
    public Image fadeImage;
    public GameObject loadingScreen;

    public bool IsTransitioning { get; private set; }
    public string TargetSceneName { get; private set; }
    public AsyncOperation CurrentAsyncLoad { get; private set; }

    private string _activeSceneName;
    private Coroutine _transitionRoutine;

    protected override void Awake()
    {
        _activeSceneName = SceneManager.GetActiveScene().name;
        base.Awake();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _activeSceneName = scene.name;
        IsTransitioning = false;
        TargetSceneName = null;
        _transitionRoutine = null;

        if (scene.name == "MainMenu" && loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(DelayedFadeIn());
    }

    private IEnumerator DelayedFadeIn()
    {
        yield return null;
        yield return FadeIn();
    }

    public bool TryBeginTransition(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
            return false;

        if (IsTransitioning)
            return false;

        if (_activeSceneName == sceneName)
            return false;

        IsTransitioning = true;
        TargetSceneName = sceneName;

        bool useLoadingScreen = loadingScreen != null && _activeSceneName == "Menu2";
        _transitionRoutine = StartCoroutine(useLoadingScreen
            ? FadeOutAndLoadScene(sceneName)
            : SimpleFadeTransition(sceneName));

        return true;
    }

    public void ChangeScene(string sceneName)
    {
        TryBeginTransition(sceneName);
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null)
            yield break;

        float t = fadeTime;
        Color c = fadeImage.color;

        while (t > 0)
        {
            t -= Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(t / fadeTime);
            fadeImage.color = c;
            yield return null;
        }
    }

    private IEnumerator SimpleFadeTransition(string sceneName)
    {
        if (fadeImage != null)
        {
            float t = 0;
            Color c = fadeImage.color;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(t / fadeTime);
                fadeImage.color = c;
                yield return null;
            }
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        CurrentAsyncLoad = asyncLoad;
        while (!asyncLoad.isDone)
            yield return null;

        CurrentAsyncLoad = null;
        yield return FadeIn();
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        if (fadeImage != null)
        {
            float t = 0;
            Color c = fadeImage.color;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(t / fadeTime);
                fadeImage.color = c;
                yield return null;
            }
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        CurrentAsyncLoad = asyncLoad;
        asyncLoad.allowSceneActivation = false;

        float loadTimer = 0f;
        while (asyncLoad.progress < 0.9f || loadTimer < minLoadingTime)
        {
            loadTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
            yield return null;

        CurrentAsyncLoad = null;
        yield return FadeIn();
    }
}
