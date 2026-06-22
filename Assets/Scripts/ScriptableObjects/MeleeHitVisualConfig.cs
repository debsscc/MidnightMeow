using UnityEngine;

/// <summary>
/// Visual do ataque melee da Nixie — onda expansiva configurável (range vem de <see cref="MeleeCombatStats"/>).
/// </summary>
[CreateAssetMenu(fileName = "MeleeHitVisualConfig", menuName = "MidnightMeow/Combat/Melee Hit Visual Config")]
public class MeleeHitVisualConfig : ScriptableObject
{
    [Header("Onda")]
    [Tooltip("Duração total do efeito em segundos.")]
    public float displayDuration = 0.38f;

    [Tooltip("Velocidade da frente de onda (multiplicador).")]
    public float waveSpeed = 1.15f;

    [Tooltip("Largura da borda brilhante da onda (0–1 em UV).")]
    [Range(0.02f, 0.25f)] public float waveEdgeWidth = 0.08f;

    [Tooltip("Multiplicador visual do alcance (1 = igual ao attackRange do SO de combate).")]
    [Range(0.5f, 1.5f)] public float rangeVisualMultiplier = 1f;

    [Header("Cores — normal")]
    public Color fillColor = new Color(1f, 0.42f, 0.12f, 0.28f);
    public Color waveEdgeColor = new Color(1f, 0.88f, 0.35f, 0.92f);
    public Color outlineColor = new Color(1f, 0.72f, 0.2f, 0.55f);

    [Header("Cores — passiva ativa")]
    public Color passiveFillColor = new Color(0.35f, 0.85f, 1f, 0.38f);
    public Color passiveWaveEdgeColor = new Color(0.75f, 1f, 1f, 0.98f);
    public Color passiveOutlineColor = new Color(0.5f, 0.95f, 1f, 0.72f);

    [Header("Render")]
    public int sortingOrder = 48;

    [Header("Debug")]
    [Tooltip("Gizmos no editor (desligado em produção).")]
    public bool drawDebugGizmos;
}
