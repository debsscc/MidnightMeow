using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Avalia condição de vitória por fase no servidor e dispara <see cref="GameEvents.OnNightEnded"/>.
/// </summary>
[DisallowMultipleComponent]
public class PhaseObjectiveManager : MonoBehaviour
{
    public static PhaseObjectiveManager Instance { get; private set; }

    private PhaseWaveSettingsCatalog.PhaseEntry _entry;
    private bool _victoryTriggered;
    private float _statusBroadcastTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        GameEvents.OnCarriageArrived -= HandleCarriageArrived;
    }

    public void Configure(PhaseWaveSettingsCatalog.PhaseEntry entry)
    {
        _entry = entry;
        GameEvents.OnCarriageArrived -= HandleCarriageArrived;

        if (_entry != null && _entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.CarriageReachEnd)
            GameEvents.OnCarriageArrived += HandleCarriageArrived;
    }

    private void Update()
    {
        if (_entry == null || _victoryTriggered || !IsServer())
            return;

        _statusBroadcastTimer += Time.deltaTime;
        if (_statusBroadcastTimer >= 0.5f)
        {
            _statusBroadcastTimer = 0f;
            int alive = NetworkWaveManager.Instance != null ? NetworkWaveManager.Instance.EnemiesAlive : 0;
            PhaseObjectiveStatusUtility.BroadcastCurrentStatus(alive);
        }

        if (_entry.winCondition != PhaseWaveSettingsCatalog.PhaseWinCondition.SealAllHoles)
            return;

        PhaseObjectiveStatusUtility.CountSealedHoles(out int sealedCount, out int totalCount);
        if (totalCount > 0 && sealedCount >= totalCount)
            TriggerVictory("Todos os buracos selados.");
    }

    public void NotifyBossDefeated()
    {
        if (_victoryTriggered || _entry == null)
            return;

        if (_entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.KillBoss)
            TriggerVictory("Boss derrotado.");
    }

    private void HandleCarriageArrived()
    {
        if (_victoryTriggered || _entry == null)
            return;

        if (_entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.CarriageReachEnd)
            TriggerVictory("Carruagem chegou ao destino.");
    }

    private void TriggerVictory(string reason)
    {
        if (_victoryTriggered || !IsServer())
            return;

        _victoryTriggered = true;
        Debug.Log($"[PhaseObjectiveManager] Vitória: {reason}");
        NetworkWaveManager.Instance?.StopSpawning();
        GameEvents.InvokeNightEnded();
    }

    private static bool IsServer()
    {
        NetworkManager net = NetworkManager.Singleton;
        return net == null || net.IsServer;
    }
}
