using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using UnityEngine;

/// <summary>
/// Inicializa GameAnalytics ao abrir o jogo e mantém o tracker vivo entre cenas.
/// </summary>
[DisallowMultipleComponent]
public class MidnightMeowAnalyticsBootstrap : MonoBehaviour
{
    private static MidnightMeowAnalyticsBootstrap _instance;

    [SerializeField] private MidnightMeowAnalyticsConfig config;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap() => EnsureExists();

    public static void EnsureExists()
    {
        if (_instance != null)
            return;

        MidnightMeowAnalyticsBootstrap existing =
            FindFirstObjectByType<MidnightMeowAnalyticsBootstrap>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        var go = new GameObject(nameof(MidnightMeowAnalyticsBootstrap));
        go.AddComponent<MidnightMeowAnalyticsBootstrap>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (config == null)
            config = Resources.Load<MidnightMeowAnalyticsConfig>("MidnightMeowAnalyticsConfig");

        if (config == null)
        {
            Debug.LogWarning("[Analytics] MidnightMeowAnalyticsConfig não encontrado — analytics desativado.");
            return;
        }

#if UNITY_EDITOR
        if (!config.enableInEditor)
        {
            Debug.Log("[Analytics] Desativado no Editor (enableInEditor = false). Faça um build para testar envio.");
            return;
        }
#endif

        MidnightMeowAnalytics.BindConfig(config);
        ConfigureGameAnalyticsSettings(config);
        EnsureGameAnalyticsComponents();
    }

    private void Start()
    {
        if (config == null)
            return;

#if UNITY_EDITOR
        if (!config.enableInEditor)
            return;
#endif

        GameAnalytics.Initialize();

        if (GetComponent<MidnightMeowAnalyticsTracker>() == null)
            gameObject.AddComponent<MidnightMeowAnalyticsTracker>();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private static void ConfigureGameAnalyticsSettings(MidnightMeowAnalyticsConfig cfg)
    {
        GameAnalyticsSDK.Setup.Settings settings = GameAnalytics.SettingsGA;
        int platformIndex = EnsurePlatformIndex(settings);

        settings.UpdateGameKey(platformIndex, cfg.gameKey);
        settings.UpdateSecretKey(platformIndex, cfg.secretKey);

        EnsureWhitelistEntry(settings.CustomDimensions01, "solo");
        EnsureWhitelistEntry(settings.CustomDimensions01, "multiplayer");
        EnsureWhitelistEntry(settings.ResourceCurrencies, "science");
        EnsureWhitelistEntry(settings.ResourceCurrencies, "magicula");
        EnsureWhitelistEntry(settings.ResourceCurrencies, "ammo");
        EnsureWhitelistEntry(settings.ResourceItemTypes, "pickup");
    }

    private static int EnsurePlatformIndex(GameAnalyticsSDK.Setup.Settings settings)
    {
        RuntimePlatform runtime = Application.platform;

        for (int i = 0; i < settings.Platforms.Count; i++)
        {
            if (settings.Platforms[i] == runtime)
                return i;
        }

        RuntimePlatform fallback = ResolveFallbackPlatform(runtime);
        for (int i = 0; i < settings.Platforms.Count; i++)
        {
            if (settings.Platforms[i] == fallback)
                return i;
        }

        settings.AddPlatform(fallback);
        return settings.Platforms.Count - 1;
    }

    private static RuntimePlatform ResolveFallbackPlatform(RuntimePlatform runtime)
    {
        if (runtime == RuntimePlatform.WindowsEditor || runtime == RuntimePlatform.OSXEditor ||
            runtime == RuntimePlatform.LinuxEditor)
        {
            return RuntimePlatform.WindowsPlayer;
        }

        return runtime;
    }

    private static void EnsureWhitelistEntry(System.Collections.Generic.List<string> list, string value)
    {
        if (list == null || string.IsNullOrEmpty(value) || list.Contains(value))
            return;

        list.Add(value);
    }

    private void EnsureGameAnalyticsComponents()
    {
        if (GetComponent<GameAnalytics>() == null)
            gameObject.AddComponent<GameAnalytics>();

        if (GetComponent<GA_SpecialEvents>() == null)
            gameObject.AddComponent<GA_SpecialEvents>();
    }
}
