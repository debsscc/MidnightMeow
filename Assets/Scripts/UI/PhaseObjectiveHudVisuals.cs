using UnityEngine;

/// <summary>
/// Sprites do HUD de objetivo por fase (Fase 1: selar buracos).
/// </summary>
[CreateAssetMenu(fileName = "PhaseObjectiveHudVisuals", menuName = "MidnightMeow/UI/Phase Objective HUD Visuals")]
public class PhaseObjectiveHudVisuals : ScriptableObject
{
    [Header("Fase 1 — Seal all holes")]
    [SerializeField] private Sprite sealHolesBanner;
    [SerializeField] private Sprite sealHolesCounterFrame;

    public Sprite SealHolesBanner => sealHolesBanner;
    public Sprite SealHolesCounterFrame => sealHolesCounterFrame;

    public static PhaseObjectiveHudVisuals LoadCached()
    {
        return Resources.Load<PhaseObjectiveHudVisuals>("PhaseObjectiveHudVisuals");
    }
}
