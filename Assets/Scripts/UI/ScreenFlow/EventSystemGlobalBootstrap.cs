using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Mantém um único EventSystem DDOL e remove duplicatas ao carregar cenas (evita aviso do UGUI).
/// </summary>
public static class EventSystemGlobalBootstrap
{
    private const string GlobalObjectName = "GlobalEventSystem";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Reconcile();
    }

    public static void Reconcile() => EnsureGlobalEventSystem();

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DestroySceneEventSystems(scene);
        Reconcile();
    }

    private static void EnsureGlobalEventSystem()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem keeper = null;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;

            if (systems[i].gameObject.scene.name == "DontDestroyOnLoad")
            {
                keeper = systems[i];
                break;
            }
        }

        if (keeper == null)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                {
                    keeper = systems[i];
                    break;
                }
            }
        }

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null || systems[i] == keeper)
                continue;

            Object.Destroy(systems[i].gameObject);
        }

        if (keeper == null)
        {
            GameObject go = new GameObject(
                GlobalObjectName,
                typeof(EventSystem),
                typeof(InputSystemUIInputModule),
                typeof(EventSystemSingletonGuard),
                typeof(GamepadUiAutoSelect),
                typeof(GamepadCursorDriver));

            // EventSystem criado via código não vem com as ações de UI; atribui as padrão
            // (Navigate/Submit/Cancel/Point/Click) para a navegação por gamepad/teclado funcionar.
            InputSystemUIInputModule createdModule = go.GetComponent<InputSystemUIInputModule>();
            if (createdModule != null)
                ProjectInputActions.ApplyToUiModule(createdModule);

            Object.DontDestroyOnLoad(go);
            return;
        }

        if (keeper.GetComponent<EventSystemSingletonGuard>() == null)
            keeper.gameObject.AddComponent<EventSystemSingletonGuard>();

        if (keeper.gameObject.scene.name != "DontDestroyOnLoad")
            Object.DontDestroyOnLoad(keeper.gameObject);

        if (keeper.GetComponent<InputSystemUIInputModule>() == null
            && keeper.GetComponent<StandaloneInputModule>() == null)
            keeper.gameObject.AddComponent<InputSystemUIInputModule>();

        if (keeper.GetComponent<GamepadUiAutoSelect>() == null)
            keeper.gameObject.AddComponent<GamepadUiAutoSelect>();

        if (keeper.GetComponent<GamepadCursorDriver>() == null)
            keeper.gameObject.AddComponent<GamepadCursorDriver>();

        InputSystemUIInputModule uiModule = keeper.GetComponent<InputSystemUIInputModule>();
        if (uiModule != null && !ProjectInputActions.UiModuleActionsValid(uiModule))
            ProjectInputActions.ApplyToUiModule(uiModule);
    }

    private static void DestroySceneEventSystems(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name == "DontDestroyOnLoad")
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null)
                continue;

            EventSystem[] systems = roots[i].GetComponentsInChildren<EventSystem>(true);
            for (int j = 0; j < systems.Length; j++)
            {
                if (systems[j] == null)
                    continue;

                Object.Destroy(systems[j].gameObject);
            }
        }
    }
}
