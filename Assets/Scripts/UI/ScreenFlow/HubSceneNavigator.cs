using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Em multiplayer, Preparation ↔ Characters usa carga aditiva para não destruir
/// o estado de rede (session managers, NetworkList, contrato replicado).
/// </summary>
public static class HubSceneNavigator
{
    public const string HubBaseScene = "Preparation";
    public const string HubOverlayScene = "Characters";

    public static bool ShouldUseAdditiveNavigation(string sceneName, SceneLoadKind loadKind)
    {
        if (loadKind != SceneLoadKind.SinglePlayer)
            return false;

        if (GameSessionContext.IsSinglePlayer)
            return false;

        NetworkManager net = NetworkManager.Singleton;
        if (net == null || !net.IsListening)
            return false;

        return sceneName is HubBaseScene or HubOverlayScene;
    }

    public static bool IsOverlayLoaded()
    {
        Scene overlay = SceneManager.GetSceneByName(HubOverlayScene);
        return overlay.IsValid() && overlay.isLoaded;
    }

    public static bool IsBaseLoaded()
    {
        Scene hub = SceneManager.GetSceneByName(HubBaseScene);
        return hub.IsValid() && hub.isLoaded;
    }

    public static IEnumerator RunAdditiveTransition(string sceneName, float minLoadingTime, bool useLoading)
    {
        if (sceneName == HubOverlayScene)
        {
            yield return LoadOverlay(minLoadingTime, useLoading);
            yield break;
        }

        if (sceneName == HubBaseScene)
            yield return ReturnToBase(minLoadingTime, useLoading);
    }

    private static IEnumerator LoadOverlay(float minLoadingTime, bool useLoading)
    {
        if (IsOverlayLoaded())
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(HubOverlayScene));
            yield break;
        }

        AsyncOperation load = SceneManager.LoadSceneAsync(HubOverlayScene, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError("[HubSceneNavigator] Falha ao carregar Characters em modo aditivo.");
            yield break;
        }

        if (useLoading)
        {
            load.allowSceneActivation = false;
            float timer = 0f;
            while (load.progress < 0.9f || timer < minLoadingTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            load.allowSceneActivation = true;
        }

        while (!load.isDone)
            yield return null;

        Scene overlay = SceneManager.GetSceneByName(HubOverlayScene);
        if (overlay.IsValid())
            SceneManager.SetActiveScene(overlay);
    }

    private static IEnumerator ReturnToBase(float minLoadingTime, bool useLoading)
    {
        if (IsOverlayLoaded())
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(HubOverlayScene);
            if (unload != null)
            {
                if (useLoading)
                {
                    float timer = 0f;
                    while (!unload.isDone || timer < minLoadingTime)
                    {
                        timer += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
                else
                {
                    while (!unload.isDone)
                        yield return null;
                }
            }
        }

        if (IsBaseLoaded())
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(HubBaseScene));
    }
}
