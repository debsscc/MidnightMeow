/// <summary>
/// Canais de diagnóstico habilitáveis em runtime (flags).
/// </summary>
[System.Flags]
public enum GameplayDiagnosticChannel
{
    None = 0,
    ProjectileHits = 1 << 0,
    ProjectileNetwork = 1 << 1,
    EnemyDamage = 1 << 2,
    EnemyNetwork = 1 << 3,
    PlayerDash = 1 << 4,
    MeleeCombat = 1 << 5,
    All = ~0
}
