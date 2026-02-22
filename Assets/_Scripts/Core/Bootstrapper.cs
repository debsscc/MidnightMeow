using UnityEngine;

[DisallowMultipleComponent]
public class Bootstrapper : MonoBehaviour
{
    // ---> LINHA FALTANTE ADICIONADA AQUI <---
    private static Bootstrapper _instance;

    [Tooltip("If set, bootstrapper will register this GameFlowManager instance. Otherwise it will look for one on the same GameObject.")]
    [SerializeField] private GameFlowManager gameFlowManager;
    [Tooltip("Optional: assign the PlayerProgressionData asset to register it as a global service")]
    [SerializeField] private PlayerProgressionData progressionData;

    private void Awake()
    {
        // Agora _instance existe e o código compilará
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // Ensure persistence of the bootstrapper GameObject
        DontDestroyOnLoad(gameObject);

        if (gameFlowManager == null)
            gameFlowManager = GetComponent<GameFlowManager>();

        if (gameFlowManager == null)
        {
            Debug.LogError("Bootstrapper: GameFlowManager not found on GameObject. Attach a GameFlowManager to the bootstrapper.");
            return;
        }

        // Register into ServiceLocator. If a GameFlowManager is already registered, replace it.
        try
        {
            ServiceLocator.RegisterService<GameFlowManager>(gameFlowManager);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Bootstrapper: Failed to register GameFlowManager: {ex.Message}");
        }

        if (progressionData != null)
        {
            try
            {
                ServiceLocator.RegisterService<PlayerProgressionData>(progressionData);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Bootstrapper: Failed to register PlayerProgressionData: {ex.Message}");
            }
        }
    }

    private void Start()
    {
        // Delegate the initial flow to the GameFlowManager
        var gf = ServiceLocator.GetService<GameFlowManager>();
        if (gf != null)
        {
            gf.LoadMenu();
        }
    }
}