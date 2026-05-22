using UnityEngine;

/// <summary>
/// Configuração data-driven dos canais de diagnóstico (sem números mágicos no código de gameplay).
/// </summary>
[CreateAssetMenu(fileName = "GameplayDiagnosticConfig", menuName = "Config/Gameplay Diagnostic Config")]
public class GameplayDiagnosticConfig : ScriptableObject
{
    [Tooltip("Master switch — desliga todos os logs do hub.")]
    public bool masterEnabled = true;

    [Header("Canais")]
    public bool projectileHits = true;
    public bool projectileNetwork = true;
    public bool enemyDamage = true;
    public bool enemyNetwork = false;
    public bool playerDash = true;
    public bool meleeCombat = true;

    [Header("Câmera (MultiplayerCameraRig)")]
    [Tooltip("Logs [CAM-DIAG] no MultiplayerCameraController (follow, tick periódico).")]
    public bool cameraDiagnostics = false;

    public GameplayDiagnosticChannel BuildChannelMask()
    {
        if (!masterEnabled) return GameplayDiagnosticChannel.None;

        GameplayDiagnosticChannel mask = GameplayDiagnosticChannel.None;
        if (projectileHits) mask |= GameplayDiagnosticChannel.ProjectileHits;
        if (projectileNetwork) mask |= GameplayDiagnosticChannel.ProjectileNetwork;
        if (enemyDamage) mask |= GameplayDiagnosticChannel.EnemyDamage;
        if (enemyNetwork) mask |= GameplayDiagnosticChannel.EnemyNetwork;
        if (playerDash) mask |= GameplayDiagnosticChannel.PlayerDash;
        if (meleeCombat) mask |= GameplayDiagnosticChannel.MeleeCombat;
        return mask;
    }
}
