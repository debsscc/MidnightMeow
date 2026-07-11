// ----------------------------------------------------------------
// DESCRIÇÃO: Injeta UiButtonSfx em Buttons/Toggles de toda cena e Canvas.
// ----------------------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UiButtonSfxBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        UiSfxPlayer.EnsureExists();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        WireActiveScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WireScene(scene);
        WireDontDestroyOnLoadCanvases();
    }

    public static void WireActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            WireScene(scene);
        WireDontDestroyOnLoadCanvases();
    }

    private static void WireDontDestroyOnLoadCanvases()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
                continue;
            WireHierarchy(canvases[i].transform);
        }
    }

    public static void WireScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            WireHierarchy(roots[i].transform);
    }

    public static void WireHierarchy(Transform root)
    {
        if (root == null)
            return;

        EnsureCanvasWatchers(root);

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            EnsureOn(buttons[i]);

        Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
            EnsureOn(toggles[i]);
    }

    public static void EnsureOn(Selectable selectable)
    {
        if (selectable == null)
            return;

        GameObject go = selectable.gameObject;
        if (go.GetComponent<UiSfxIgnore>() != null)
            return;
        if (go.GetComponent<UiButtonSfx>() != null)
            return;

        // Sliders/scrollbars não são "botões" — não injetar.
        if (selectable is Slider or Scrollbar)
            return;

        go.AddComponent<UiButtonSfx>();
    }

    private static void EnsureCanvasWatchers(Transform root)
    {
        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
                continue;
            if (canvases[i].GetComponent<UiButtonSfxCanvasWatcher>() != null)
                continue;

            canvases[i].gameObject.AddComponent<UiButtonSfxCanvasWatcher>();
        }
    }
}

/// <summary>
/// Re-wire quando um Canvas é habilitado ou ganha filhos diretos (UI spawnada).
/// </summary>
[DisallowMultipleComponent]
public sealed class UiButtonSfxCanvasWatcher : MonoBehaviour
{
    private void OnEnable()
    {
        UiButtonSfxBootstrap.WireHierarchy(transform);
    }

    private void OnTransformChildrenChanged()
    {
        UiButtonSfxBootstrap.WireHierarchy(transform);
    }
}
