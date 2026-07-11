//--------------------------------------------------
// FUNÇÃO: Helpers da fase de boss (Fase-3 / KillBoss) — solo e multiplayer.
//--------------------------------------------------

using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossPhaseUtility
{
    /// <summary>Hit básico Nix/Cora é 1; acima disso conta como marcante (habilidade etc.).</summary>
    public const float SignificantDamageAbsoluteFloor = 1.01f;

    /// <summary>Ou pelo menos 5% da vida máxima do alvo.</summary>
    public const float SignificantDamageMaxHealthRatio = 0.05f;

    public static bool IsKillBossPhaseActive()
    {
        PhaseWaveSettingsCatalog catalog = PhaseWaveSettingsCatalog.LoadCached();
        if (catalog == null)
            return false;

        string sceneName = SceneManager.GetActiveScene().name;
        return catalog.TryGetEntry(sceneName, out PhaseWaveSettingsCatalog.PhaseEntry entry)
               && entry.winCondition == PhaseWaveSettingsCatalog.PhaseWinCondition.KillBoss;
    }

    public static bool IsBossEnemy(Component component)
    {
        return component != null && component.GetComponent<BossEnemyMarker>() != null;
    }

    public static bool IsBossEnemy(GameObject go)
    {
        return go != null && go.GetComponent<BossEnemyMarker>() != null;
    }

    /// <summary>HUD de barra de boss em tela + regras especiais de blink.</summary>
    public static bool UsesCinematicBossPresentation(GameObject go)
    {
        return IsKillBossPhaseActive() && IsBossEnemy(go);
    }

    public static bool IsSignificantHit(float dealtDamage, float maxHealth)
    {
        if (dealtDamage <= 0f)
            return false;

        if (dealtDamage >= SignificantDamageAbsoluteFloor)
            return true;

        if (maxHealth > 0f && dealtDamage >= maxHealth * SignificantDamageMaxHealthRatio)
            return true;

        return false;
    }

    public static bool ShouldPlayBossBlink(GameObject target, float dealtDamage)
    {
        if (target == null || dealtDamage <= 0f)
            return false;

        if (!UsesCinematicBossPresentation(target))
            return true;

        float maxHealth = 0f;
        if (target.TryGetComponent<HealthComponent>(out var health))
            maxHealth = health.MaxHealth;

        return IsSignificantHit(dealtDamage, maxHealth);
    }
}
