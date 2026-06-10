using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante que apenas a MainCamera do MultiplayerCameraRig renderiza a fase (evita MainCamera DDOL de menus).
/// </summary>
public static class GameplayCameraSceneUtility
{
    public static void TakeOverGameplayRendering(Camera gameplayMainCamera)
    {
        if (gameplayMainCamera == null || !GameplaySceneBootstrap.IsGameplayScene(SceneManager.GetActiveScene().name))
            return;

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || cam == gameplayMainCamera)
                continue;

            if (cam == TransitionCameraKeeper.FallbackCamera)
                continue;

            if (cam.enabled)
                cam.enabled = false;

            if (cam.CompareTag("MainCamera"))
                cam.tag = "Untagged";
        }

        if (!gameplayMainCamera.gameObject.activeInHierarchy)
            gameplayMainCamera.gameObject.SetActive(true);

        gameplayMainCamera.enabled = true;
        gameplayMainCamera.tag = "MainCamera";
        gameplayMainCamera.depth = 0;

        TransitionCameraKeeper.Refresh();
    }
}
