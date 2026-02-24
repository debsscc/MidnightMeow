using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameFlowManager : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Name of the menu scene to load on startup")]
    [SerializeField] private string menuSceneName = "Menu2";

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
        Debug.Log($"GameFlowManager: Carregando cena '{phaseName}'.");
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
        Debug.Log($"GameFlowManager: Starting async load for scene '{sceneName}'");

        var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (async == null)
        {
            Debug.LogError($"GameFlowManager: Failed to load scene '{sceneName}'.");
            _isLoading = false;
            yield break;
        }

        Debug.Log($"GameFlowManager: LoadAsync returned. allowSceneActivation={async.allowSceneActivation}, progress={async.progress}");

        async.allowSceneActivation = true;
        int frame = 0;
        while (!async.isDone)
        {
            // Log progress every 10 frames to avoid spamming
            if (frame % 10 == 0)
            {
                Debug.Log($"GameFlowManager: Loading '{sceneName}' progress={async.progress}, isDone={async.isDone}");
            }
            frame++;
            yield return null;
        }

        Debug.Log($"GameFlowManager: Scene '{sceneName}' load completed. isDone={async.isDone}, progress={async.progress}");
        _isLoading = false;
        OnSceneLoadCompleted?.Invoke();
    }

    private IEnumerator LoadSceneAsync(int buildIndex)
    {
        if (_isLoading) yield break;
        _isLoading = true;

        OnSceneLoadStarted?.Invoke();
        Debug.Log($"GameFlowManager: Starting async load for buildIndex '{buildIndex}'");

        var async = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        if (async == null)
        {
            Debug.LogError($"GameFlowManager: Failed to load scene (buildIndex) '{buildIndex}'.");
            _isLoading = false;
            yield break;
        }

        Debug.Log($"GameFlowManager: LoadAsync returned for buildIndex {buildIndex}. allowSceneActivation={async.allowSceneActivation}, progress={async.progress}");

        async.allowSceneActivation = true;
        int frame = 0;
        while (!async.isDone)
        {
            if (frame % 10 == 0)
            {
                Debug.Log($"GameFlowManager: Loading buildIndex {buildIndex} progress={async.progress}, isDone={async.isDone}");
            }
            frame++;
            yield return null;
        }

        Debug.Log($"GameFlowManager: Scene (buildIndex) '{buildIndex}' load completed. isDone={async.isDone}, progress={async.progress}");

        _isLoading = false;
        OnSceneLoadCompleted?.Invoke();
    }
}
