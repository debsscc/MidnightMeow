//---------------------------------
// FEITO POR: DEBS CARVALHO
// DATA: 17/07/2026
// DESCRIÇÃO: ScriptableObject para os sprites do HUD de objetivo por fase (Fase 1 buracos / Fase 2 carruagem).
//---------------------------------

using UnityEngine;


[CreateAssetMenu(fileName = "PhaseObjectiveHudVisuals", menuName = "MidnightMeow/UI/Phase Objective HUD Visuals")]
public class PhaseObjectiveHudVisuals : ScriptableObject
{
    [Header("Fase 1 — Seal all holes")]
    [SerializeField] private Sprite sealHolesBanner;
    [SerializeField] private Sprite sealHolesCounterFrame;

    [Header("Fase 2 — Protect carriage")]
    [SerializeField] private Sprite carriageBanner;
    [SerializeField] private Sprite carriageBarBackground;
    [SerializeField] private Sprite carriageBarFrame;
    [SerializeField] private Sprite carriageFollowerIcon;

    [Header("Fase 3 — Kill boss")]
    [SerializeField] private Sprite bossBanner;
    [SerializeField] private Sprite bossBarBackground;
    [SerializeField] private Sprite bossBarFrame;

    public Sprite SealHolesBanner => sealHolesBanner;
    public Sprite SealHolesCounterFrame => sealHolesCounterFrame;
    public Sprite CarriageBanner => carriageBanner;
    public Sprite CarriageBarBackground => carriageBarBackground;
    public Sprite CarriageBarFrame => carriageBarFrame;
    public Sprite CarriageFollowerIcon => carriageFollowerIcon;
    public Sprite BossBanner => bossBanner != null ? bossBanner : carriageBanner;
    public Sprite BossBarBackground => bossBarBackground != null ? bossBarBackground : carriageBarBackground;
    public Sprite BossBarFrame => bossBarFrame != null ? bossBarFrame : carriageBarFrame;

    public static PhaseObjectiveHudVisuals LoadCached()
    {
        return Resources.Load<PhaseObjectiveHudVisuals>("PhaseObjectiveHudVisuals");
    }
}
