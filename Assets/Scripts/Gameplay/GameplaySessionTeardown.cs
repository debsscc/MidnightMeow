using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Congela gameplay e oculta entidades antes de telas de vitória/derrota.
/// </summary>
public static class GameplaySessionTeardown
{
    public static void PrepareForEndGameScreen()
    {
        Time.timeScale = 0f;
        GameEvents.InvokePauseChanged(true);

        DisablePlayerControl();
        HideGameplayActors();
        StopLocalSpawners();
    }

    private static void DisablePlayerControl()
    {
        PlayerMovement[] movements = Object.FindObjectsByType<PlayerMovement>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < movements.Length; i++)
        {
            if (movements[i] != null)
                movements[i].enabled = false;
        }

        PlayerInputHandler[] inputs = Object.FindObjectsByType<PlayerInputHandler>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] != null)
                inputs[i].enabled = false;
        }

        PlayerShooting[] shooters = Object.FindObjectsByType<PlayerShooting>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < shooters.Length; i++)
        {
            if (shooters[i] != null)
                shooters[i].enabled = false;
        }

        PlayerAbilityHandler[] abilities = Object.FindObjectsByType<PlayerAbilityHandler>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i] != null)
                abilities[i].enabled = false;
        }
    }

    private static void HideGameplayActors()
    {
        NetworkManager net = NetworkManager.Singleton;
        bool isSinglePlayer = net == null || !net.IsListening;
        bool isServer = net == null || net.IsServer;

        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth player = players[i];
            if (player == null)
                continue;

            if (isSinglePlayer)
            {
                NetworkObject netObj = player.GetComponent<NetworkObject>();
                if (isServer && netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
                else
                    player.gameObject.SetActive(false);

                continue;
            }

            player.gameObject.SetActive(false);
        }

        if (isSinglePlayer)
            HideEnemies(isServer);

        if (!isSinglePlayer)
            return;

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < taggedPlayers.Length; i++)
        {
            if (taggedPlayers[i] != null)
                taggedPlayers[i].SetActive(false);
        }
    }

    private static void HideEnemies(bool isServer)
    {
        HealthComponent[] enemies = Object.FindObjectsByType<HealthComponent>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            HealthComponent health = enemies[i];
            if (health == null || health.CompareTag("Player"))
                continue;

            if (!health.CompareTag("Enemy"))
                continue;

            NetworkObject enemyNet = health.GetComponent<NetworkObject>();
            if (isServer && enemyNet != null && enemyNet.IsSpawned)
                enemyNet.Despawn(true);
            else
                health.gameObject.SetActive(false);
        }
    }

    private static void StopLocalSpawners()
    {
        RatHoleSpawnOrchestrator orchestrator = RatHoleSpawnOrchestrator.Instance;
        if (orchestrator != null)
            orchestrator.StopAll();

        WaveGenerator generator = Object.FindFirstObjectByType<WaveGenerator>(FindObjectsInactive.Include);
        if (generator != null)
            generator.StopSpawning();

        NightManager nightManager = Object.FindFirstObjectByType<NightManager>(FindObjectsInactive.Include);
        if (nightManager != null)
            nightManager.ForceStop();
    }
}
