/// <summary>
/// Hub event-driven para diagnóstico de gameplay/combate.
/// Scripts emitem snapshots; listeners (ex.: GameplayDiagnosticListener) decidem se logam.
/// </summary>
using System;

public static class GameplayDiagnosticHub
{
    public static GameplayDiagnosticChannel EnabledChannels { get; set; } = GameplayDiagnosticChannel.None;

    public static bool IsChannelEnabled(GameplayDiagnosticChannel channel)
        => channel != GameplayDiagnosticChannel.None && (EnabledChannels & channel) != 0;

    public static event Action<ProjectileHitDiagnostic> OnProjectileHit;
    public static event Action<EnemyDamageDiagnostic> OnEnemyDamage;
    public static event Action<ProjectileNetworkDiagnostic> OnProjectileNetwork;
    public static event Action<PlayerDashDiagnostic> OnPlayerDash;
    public static event Action<MeleeHitDiagnostic> OnMeleeHit;

    public static void Emit(ProjectileHitDiagnostic diagnostic)
    {
        if (!IsChannelEnabled(GameplayDiagnosticChannel.ProjectileHits)) return;
        OnProjectileHit?.Invoke(diagnostic);
    }

    public static void Emit(EnemyDamageDiagnostic diagnostic)
    {
        if (!IsChannelEnabled(GameplayDiagnosticChannel.EnemyDamage)) return;
        OnEnemyDamage?.Invoke(diagnostic);
    }

    public static void Emit(ProjectileNetworkDiagnostic diagnostic)
    {
        if (!IsChannelEnabled(GameplayDiagnosticChannel.ProjectileNetwork)) return;
        OnProjectileNetwork?.Invoke(diagnostic);
    }

    public static void EmitPlayerDash(PlayerDashDiagnostic diagnostic)
    {
        if (!IsChannelEnabled(GameplayDiagnosticChannel.PlayerDash)) return;
        OnPlayerDash?.Invoke(diagnostic);
    }

    public static void EmitMelee(MeleeHitDiagnostic diagnostic)
    {
        if (!IsChannelEnabled(GameplayDiagnosticChannel.MeleeCombat)) return;
        OnMeleeHit?.Invoke(diagnostic);
    }
}

public readonly struct PlayerDashDiagnostic
{
    public readonly string PlayerName;
    public readonly string Stage;
    public readonly float Duration;
    public readonly float Speed;
    public readonly bool IsOwner;
    public readonly bool IsServer;

    public PlayerDashDiagnostic(string playerName, string stage, float duration, float speed, bool isOwner, bool isServer)
    {
        PlayerName = playerName;
        Stage = stage;
        Duration = duration;
        Speed = speed;
        IsOwner = isOwner;
        IsServer = isServer;
    }
}

public readonly struct MeleeHitDiagnostic
{
    public readonly string AttackerName;
    public readonly string TargetName;
    public readonly float Damage;
    public readonly bool HitConfirmed;
    public readonly string Detail;

    public MeleeHitDiagnostic(string attackerName, string targetName, float damage, bool hitConfirmed, string detail)
    {
        AttackerName = attackerName;
        TargetName = targetName;
        Damage = damage;
        HitConfirmed = hitConfirmed;
        Detail = detail;
    }
}

public readonly struct ProjectileHitDiagnostic
{
    public readonly string Stage;
    public readonly string TargetName;
    public readonly int TargetLayer;
    public readonly bool IsNetworkSpawned;
    public readonly bool IsServer;
    public readonly bool FoundNetworkEnemy;
    public readonly bool EnemyDeadOnNetwork;
    public readonly bool DamageApplied;
    public readonly float BaseDamage;
    public readonly string Detail;

    public ProjectileHitDiagnostic(
        string stage,
        string targetName,
        int targetLayer,
        bool isNetworkSpawned,
        bool isServer,
        bool foundNetworkEnemy,
        bool enemyDeadOnNetwork,
        bool damageApplied,
        float baseDamage,
        string detail)
    {
        Stage = stage;
        TargetName = targetName;
        TargetLayer = targetLayer;
        IsNetworkSpawned = isNetworkSpawned;
        IsServer = isServer;
        FoundNetworkEnemy = foundNetworkEnemy;
        EnemyDeadOnNetwork = enemyDeadOnNetwork;
        DamageApplied = damageApplied;
        BaseDamage = baseDamage;
        Detail = detail;
    }
}

public readonly struct EnemyDamageDiagnostic
{
    public readonly string EnemyName;
    public readonly ulong NetworkObjectId;
    public readonly bool IsServer;
    public readonly float Amount;
    public readonly float HealthBefore;
    public readonly float HealthAfter;
    public readonly bool IsDead;
    public readonly string Source;

    public EnemyDamageDiagnostic(
        string enemyName,
        ulong networkObjectId,
        bool isServer,
        float amount,
        float healthBefore,
        float healthAfter,
        bool isDead,
        string source)
    {
        EnemyName = enemyName;
        NetworkObjectId = networkObjectId;
        IsServer = isServer;
        Amount = amount;
        HealthBefore = healthBefore;
        HealthAfter = healthAfter;
        IsDead = isDead;
        Source = source;
    }
}

public readonly struct ProjectileNetworkDiagnostic
{
    public readonly string Stage;
    public readonly ulong NetworkObjectId;
    public readonly ulong OwnerClientId;
    public readonly bool IsServer;
    public readonly bool RigidbodySimulated;
    public readonly bool ProjectileEnabled;
    public readonly int CollidersEnabled;
    public readonly int CollidersTotal;

    public ProjectileNetworkDiagnostic(
        string stage,
        ulong networkObjectId,
        ulong ownerClientId,
        bool isServer,
        bool rigidbodySimulated,
        bool projectileEnabled,
        int collidersEnabled,
        int collidersTotal)
    {
        Stage = stage;
        NetworkObjectId = networkObjectId;
        OwnerClientId = ownerClientId;
        IsServer = isServer;
        RigidbodySimulated = rigidbodySimulated;
        ProjectileEnabled = projectileEnabled;
        CollidersEnabled = collidersEnabled;
        CollidersTotal = collidersTotal;
    }
}
