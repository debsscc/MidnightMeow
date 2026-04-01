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
    private string currentSceneName;
    
    protected override void Awake()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        base.Awake();
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("SceneTransition AWAKE — Singleton criado");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        if (scene.name == "MainMenu")
        {
            if (loadingScreen != null) loadingScreen.SetActive(false);
        }
    }

    void Start()
    {
    }

    IEnumerator DelayedFadeIn()
    {
        yield return null;
        StartCoroutine(FadeIn());
    }

    public void ChangeScene(string sceneName)
    {
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
        {
            return;
        }

        bool useLoadingScreen = loadingScreen != null && currentSceneName == "Menu2";
        currentSceneName = sceneName;
        StartCoroutine(useLoadingScreen ? FadeOutAndLoadScene(sceneName) : SimpleFadeTransition(sceneName));

    }

    IEnumerator FadeIn()
    {
        float t = fadeTime;
        Color c = fadeImage.color;

        while (t > 0)
        {
            t -= Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeTime);
            fadeImage.color = c;
            yield return null;
        }
    }

    IEnumerator SimpleFadeTransition(string sceneName)
    {
        // Fade out
        float t = 0;
        Color c = fadeImage.color;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeTime);
            fadeImage.color = c;
            yield return null;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;

        yield return StartCoroutine(FadeIn());
    }

    IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        // Fase 1: fade out do menu
        float t = 0;
        Color c = fadeImage.color;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeTime);
            fadeImage.color = c;
            yield return null;
        }

        // Fase 2: mostra loading screen (tela já preta)
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Inicia o carregamento em background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Aguarda o tempo mínimo da loading screen e a cena estar pronta
        float loadTimer = 0f;
        while (asyncLoad.progress < 0.9f || loadTimer < minLoadingTime)
        {
            loadTimer += Time.deltaTime;
            yield return null;
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
            yield return null;

        Debug.Log("Scene loaded");

        // Fade in: revela a nova cena
        yield return StartCoroutine(FadeIn());
    }
}