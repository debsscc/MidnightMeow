using UnityEngine;

[DisallowMultipleComponent]
public class Bootstrapper : MonoBehaviour
{
    private static Bootstrapper _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureScreenFlowServices()
    {
        TransitionFadeOverlay.EnsureExists();
        ScreenFlowController.EnsureExists();
    }

    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private ScreenFlowController screenFlowController;
    [SerializeField] private SceneFlowCatalog sceneFlowCatalog;
    [SerializeField] private PlayerProgressionData progressionData;
    [SerializeField] private SaveProfileStore saveProfileStore;
    [SerializeField] private GameFlowOrchestrator gameFlowOrchestrator;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        TransitionFadeOverlay.EnsureExists();

        if (screenFlowController == null)
            screenFlowController = GetComponent<ScreenFlowController>();

        if (screenFlowController == null)
            screenFlowController = gameObject.AddComponent<ScreenFlowController>();

        if (sceneFlowCatalog != null)
            screenFlowController.SetCatalog(sceneFlowCatalog);

        if (gameFlowManager == null)
            gameFlowManager = GetComponent<GameFlowManager>();

        if (gameFlowManager == null)
        {
            Debug.LogError("Bootstrapper: GameFlowManager não encontrado.");
            return;
        }

        RegisterService(gameFlowManager);
        RegisterService(screenFlowController);

        if (progressionData != null)
            RegisterService(progressionData);

        if (saveProfileStore == null)
            saveProfileStore = GetComponent<SaveProfileStore>();
        if (saveProfileStore == null)
            saveProfileStore = gameObject.AddComponent<SaveProfileStore>();
        RegisterService(saveProfileStore);

        if (gameFlowOrchestrator == null)
            gameFlowOrchestrator = GetComponent<GameFlowOrchestrator>();
        if (gameFlowOrchestrator == null)
            gameFlowOrchestrator = gameObject.AddComponent<GameFlowOrchestrator>();
        RegisterService(gameFlowOrchestrator);
    }

    private void Start()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null)
        {
            flow.RequestRoute(SceneFlowRouteIds.BootstrapToMenu);
            return;
        }

        GameFlowManager gf = ServiceLocator.GetService<GameFlowManager>();
        gf?.LoadMenu();
    }

    private static void RegisterService<T>(T service) where T : class
    {
        try
        {
            ServiceLocator.RegisterService<T>(service);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Bootstrapper: falha ao registrar {typeof(T).Name}: {ex.Message}");
        }
    }
}
