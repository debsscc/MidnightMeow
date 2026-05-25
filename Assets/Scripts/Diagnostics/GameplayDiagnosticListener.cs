/// <summary>
/// Listener modular: assina GameplayDiagnosticHub e emite logs filtráveis no Console.
/// Coloque na cena de teste multiplayer (ex.: junto ao MultiplayerLogger).
/// </summary>
using Unity.Netcode;
using UnityEngine;

public class GameplayDiagnosticListener : MonoBehaviour
{
    [SerializeField] private GameplayDiagnosticConfig config;
    [SerializeField] private bool useConfigAsset = true;

    [Header("Override manual (se useConfigAsset = false)")]
    [SerializeField] private bool masterEnabled = true;
    [SerializeField] private bool logProjectileHits = true;
    [SerializeField] private bool logProjectileNetwork = true;
    [SerializeField] private bool logEnemyDamage = true;

    [Header("Formatação")]
    [SerializeField] private string prefix = "[DIAG]";

    private void OnEnable()
    {
        ApplyChannelMask();
        GameplayDiagnosticHub.OnProjectileHit += OnProjectileHit;
        GameplayDiagnosticHub.OnEnemyDamage += OnEnemyDamage;
        GameplayDiagnosticHub.OnProjectileNetwork += OnProjectileNetwork;
        GameplayDiagnosticHub.OnPlayerDash += OnPlayerDash;
        GameplayDiagnosticHub.OnMeleeHit += OnMeleeHit;
    }

    private void OnDisable()
    {
        GameplayDiagnosticHub.OnProjectileHit -= OnProjectileHit;
        GameplayDiagnosticHub.OnEnemyDamage -= OnEnemyDamage;
        GameplayDiagnosticHub.OnProjectileNetwork -= OnProjectileNetwork;
        GameplayDiagnosticHub.OnPlayerDash -= OnPlayerDash;
        GameplayDiagnosticHub.OnMeleeHit -= OnMeleeHit;
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
            ApplyChannelMask();
    }

    public void ApplyChannelMask()
    {
        if (useConfigAsset && config != null)
            GameplayDiagnosticHub.EnabledChannels = config.BuildChannelMask();
        else if (!masterEnabled)
            GameplayDiagnosticHub.EnabledChannels = GameplayDiagnosticChannel.None;
        else
        {
            GameplayDiagnosticChannel mask = GameplayDiagnosticChannel.None;
            if (logProjectileHits) mask |= GameplayDiagnosticChannel.ProjectileHits;
            if (logProjectileNetwork) mask |= GameplayDiagnosticChannel.ProjectileNetwork;
            if (logEnemyDamage) mask |= GameplayDiagnosticChannel.EnemyDamage;
            GameplayDiagnosticHub.EnabledChannels = mask;
        }
    }

    private void OnProjectileHit(ProjectileHitDiagnostic d)
    {
        string net = NetworkManager.Singleton != null
            ? (NetworkManager.Singleton.IsServer ? "server" : "client")
            : "offline";

        string msg =
            $"{NetPrefix()} [{d.Stage}] target={d.TargetName} layer={d.TargetLayer} " +
            $"netSpawned={d.IsNetworkSpawned} isServer={d.IsServer} netEnemy={d.FoundNetworkEnemy} " +
            $"enemyDead={d.EnemyDeadOnNetwork} dmg={d.BaseDamage:0.##} applied={d.DamageApplied} | {d.Detail}";

        if (d.DamageApplied)
            Log(msg);
        else
            Warn(msg);
    }

    private void OnEnemyDamage(EnemyDamageDiagnostic d)
    {
        string msg = $"{NetPrefix()} [EnemyDamage] {d.EnemyName} netId={d.NetworkObjectId} " +
            $"amount={d.Amount:0.##} hp={d.HealthBefore:0.##}->{d.HealthAfter:0.##} dead={d.IsDead} | {d.Source}";

        if (d.Source.StartsWith("REJECTED", System.StringComparison.Ordinal))
            Warn(msg);
        else
            Log(msg);
    }

    private void OnProjectileNetwork(ProjectileNetworkDiagnostic d)
    {
        Log($"{NetPrefix()} [ProjectileNet] {d.Stage} id={d.NetworkObjectId} owner={d.OwnerClientId} " +
            $"server={d.IsServer} rb={d.RigidbodySimulated} proj={d.ProjectileEnabled} " +
            $"colliders={d.CollidersEnabled}/{d.CollidersTotal}");
    }

    private void OnPlayerDash(PlayerDashDiagnostic d)
    {
        Log($"{NetPrefix()} [PlayerDash] {d.PlayerName} stage={d.Stage} dur={d.Duration:0.##} " +
            $"speed={d.Speed:0.##} owner={d.IsOwner} server={d.IsServer}");
    }

    private void OnMeleeHit(MeleeHitDiagnostic d)
    {
        if (d.HitConfirmed)
            Log($"{NetPrefix()} [MeleeHit] {d.AttackerName} -> {d.TargetName} dmg={d.Damage:0.##} | {d.Detail}");
    }

    private string NetPrefix() => $"{prefix} {ContextoRede()}";

    private static string ContextoRede()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return "[offline]";
        string mode = nm.IsHost ? "host" : (nm.IsServer ? "server" : "client");
        return $"[{mode} id={nm.LocalClientId}]";
    }

    private void Log(string message) => Debug.Log(message);

    private void Warn(string message) => Debug.LogWarning(message);
}
