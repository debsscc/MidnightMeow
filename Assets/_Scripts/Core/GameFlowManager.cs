using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameFlowManager : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Name of the menu scene to load on startup")]
    [SerializeField] private string menuSceneName = "Menu";

    public event Action OnSceneLoadStarted;
    public event Action OnSceneLoadCompleted;

    private bool _isLoading;

    public void LoadMenu()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("GameFlowManager: menuSceneName is empty. Cannot LoadMenu().");
            return;
        }
        StartCoroutine(LoadSceneAsync(menuSceneName));
    }

    public void LoadPhase(string phaseName)
    {
        if (string.IsNullOrEmpty(phaseName))
        {
            Debug.LogError("GameFlowManager: phaseName is empty. Cannot LoadPhase().");
            return;
        }
        StartCoroutine(LoadSceneAsync(phaseName));
    }

    public void LoadPhase(int buildIndex)
    {
        StartCoroutine(LoadSceneAsync(buildIndex));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (_isLoading) yield break;
        _isLoading = true;

        OnSceneLoadStarted?.Invoke();

        var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (async == null)
        {
            Debug.LogError($"GameFlowManager: Failed to load scene '{sceneName}'.");
            _isLoading = false;
            yield break;
        }

        async.allowSceneActivation = true;
        while (!async.isDone)
        {
            yield return null;
        }

        _isLoading = false;
        OnSceneLoadCompleted?.Invoke();
    }

    private IEnumerator LoadSceneAsync(int buildIndex)
    {
        if (_isLoading) yield break;
        _isLoading = true;

        OnSceneLoadStarted?.Invoke();

        var async = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        if (async == null)
        {
            Debug.LogError($"GameFlowManager: Failed to load scene (buildIndex) '{buildIndex}'.");
            _isLoading = false;
            yield break;
        }

        async.allowSceneActivation = true;
        while (!async.isDone)
        {
            yield return null;
        }

        _isLoading = false;
        OnSceneLoadCompleted?.Invoke();
    }
}
