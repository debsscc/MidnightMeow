using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Bootstrap por cena: garante EventSystem, serviços persistentes e controller adequado ao fluxo de telas.
/// </summary>
[DisallowMultipleComponent]
public class ScreenFlowSceneBootstrap : MonoBehaviour
{
    [SerializeField] private bool ensureEventSystem = true;
    [SerializeField] private bool ensurePersistenceServices = true;

    private void Awake()
    {
        ScreenFlowLegacySceneCleanup.ApplyForActiveScene();
        SuppressLegacyMenuButton();

        if (ensureEventSystem)
            EnsureEventSystem();

        if (ensurePersistenceServices)
            EnsurePersistenceServices();

        EnsureSceneController();
    }

    private static void SuppressLegacyMenuButton()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene is not ("Loading1" or "Loading2" or "Preparation" or "Characters"))
            return;

        Button legacyMenu = ScreenFlowUiLookup.FindButton("Button_Menu");
        if (legacyMenu != null)
            legacyMenu.gameObject.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void EnsurePersistenceServices()
    {
        if (SaveProfileStore.Instance == null)
        {
            GameObject saveGo = new GameObject("SaveProfileStore");
            saveGo.AddComponent<SaveProfileStore>();
            DontDestroyOnLoad(saveGo);
        }

        if (GameFlowOrchestrator.Instance == null)
        {
            GameObject orchGo = new GameObject("GameFlowOrchestrator");
            orchGo.AddComponent<GameFlowOrchestrator>();
            DontDestroyOnLoad(orchGo);
        }
    }

    private void EnsureSceneController()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        switch (scene)
        {
            case "Menu2":
                if (FindFirstObjectByType<MainMenuController>() == null)
                {
                    GameObject go = new GameObject("MainMenuController");
                    go.AddComponent<MainMenuController>();
                }
                break;

            case "Lobby":
                if (FindFirstObjectByType<LobbySceneUIController>() == null
                    && FindFirstObjectByType<LobbyFlowController>() == null)
                {
                    GameObject go = new GameObject("LobbyFlowController");
                    go.AddComponent<LobbyFlowController>();
                }
                break;

            case "Loading1":
            case "Loading2":
                if (FindFirstObjectByType<LoadingScreenController>() == null)
                {
                    GameObject go = new GameObject("LoadingScreenController");
                    var loading = go.AddComponent<LoadingScreenController>();
                    if (scene == "Loading2")
                    {
                        // Loading 2 usa rota padrão para gameplay
                    }
                }
                break;

            case "Preparation":
                EnsurePreparationNetwork();
                if (FindFirstObjectByType<PreparationScreenController>() == null)
                {
                    GameObject go = new GameObject("PreparationScreenController");
                    go.AddComponent<PreparationScreenController>();
                }
                break;

            case "Characters":
                EnsureCharactersNetwork();
                if (FindFirstObjectByType<CharactersScreenController>() == null)
                {
                    GameObject go = new GameObject("CharactersScreenController");
                    go.AddComponent<CharactersScreenController>();
                }
                break;

            case "VictoryScene":
            case "GameOver":
                if (FindFirstObjectByType<EndGameScreenController>() == null)
                {
                    GameObject go = new GameObject("EndGameScreenController");
                    go.AddComponent<EndGameScreenController>();
                }
                break;
        }
    }

    private static void EnsurePreparationNetwork() => EnsureHubSessionManagers();

    private static void EnsureCharactersNetwork() => EnsureHubSessionManagers();

    private static void EnsureHubSessionManagers()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        EnsureHubManager<PreparationSessionManager>("PreparationSessionManager");
        EnsureHubManager<CharactersSessionManager>("CharactersSessionManager");
    }

    private static void EnsureHubManager<T>(string objectName) where T : Unity.Netcode.NetworkBehaviour
    {
        if (UnityEngine.Object.FindFirstObjectByType<T>() != null)
            return;

        GameObject go = new GameObject(objectName);
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Unity.Netcode.NetworkObject>();
        go.AddComponent<T>();
        go.GetComponent<Unity.Netcode.NetworkObject>().Spawn();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrapAfterSceneLoad()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene is not ("Menu2" or "Lobby" or "Loading1" or "Loading2" or "Preparation" or "Characters" or "VictoryScene" or "GameOver"))
            return;

        ScreenFlowLegacySceneCleanup.ApplyForActiveScene();

        if (FindFirstObjectByType<ScreenFlowSceneBootstrap>() != null)
            return;

        GameObject bootstrap = new GameObject("ScreenFlowBootstrap");
        bootstrap.AddComponent<ScreenFlowSceneBootstrap>();
    }
}
