///* ----------------------------------------------------------------
// ATUALIZADO EM: 14-07-2026
// DESCRIÇÃO: ScriptableObject de balanceamento do Rei Rato (pesos, fuga com early-exit, 5 faixas, investida, melee).
// ---------------------------------------------------------------- */

using UnityEngine;

[CreateAssetMenu(fileName = "RatKingBehaviorConfig", menuName = "MidnightMeow/Combat/Rat King Behavior Config")]
public class RatKingBehaviorConfig : ScriptableObject
{
    [Header("Roleta de ataques")]
    [Tooltip("Peso do ataque a distância (ex.: 70).")]
    [SerializeField] private float rangedWeight = 70f;
    [Tooltip("Peso da investida (ex.: 30).")]
    [SerializeField] private float chargeWeight = 30f;
    [Tooltip("Pausa curta após um ataque antes do próximo sorteio.")]
    [SerializeField] private float decisionPause = 0.35f;

    [Header("Ataque a distância — fuga")]
    [SerializeField] private float minFleeTime = 1f;
    [SerializeField] private float maxFleeTime = 4f;
    [Tooltip("Distância de amostragem NavMesh ao fugir.")]
    [SerializeField] private float fleeSampleDistance = 5f;
    [Tooltip("Alcance máximo do ataque a distância (deve casar com o comprimento visual do telegraph / rangedLaneLength).")]
    [SerializeField] private float maxRangedDistance = 8f;
    [Tooltip("Fração de maxRangedDistance: ao atingir (ex.: 0.75 = 75%), interrompe a fuga e ataca. Timer de fuga continua como fallback.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float fleeDistanceThreshold = 0.75f;

    [Header("Ataque a distância — 5 faixas")]
    [Tooltip("Ângulo interno (graus) relativo ao alvo. Disparos em ±angle1.")]
    [SerializeField] private float rangedAngle1 = 30f;
    [Tooltip("Ângulo externo (graus). Disparos em ±angle2.")]
    [SerializeField] private float rangedAngle2 = 60f;
    [SerializeField] private float rangedLaneWidth = 0.85f;
    [Tooltip("Comprimento visual de cada faixa (telegraph). Idealmente igual a maxRangedDistance.")]
    [SerializeField] private float rangedLaneLength = 8f;
    [SerializeField] private float rangedFillDuration = 0.9f;
    [SerializeField] private int rangedDamage = 12;
    [SerializeField] private LayerMask rangedDamageLayers;
    [SerializeField] private EnemyTelegraphVisualStyle rangedVisualStyle;
    [Tooltip("Opcional: se preenchido, dispara este pattern em vez de gerar as 5 faixas no código.")]
    [SerializeField] private EnemyAttackPatternDefinition rangedPatternOverride;

    [Header("Investida")]
    [Tooltip("Comprimento total da trajetória de dash (telegraph + avanço).")]
    [SerializeField] private float chargeRange = 7f;
    [Tooltip("Multiplicador de velocidade ao aproximar (buff).")]
    [SerializeField] private float chargeApproachSpeedMultiplier = 1.75f;
    [Tooltip("Velocidade do dash (unidades/segundo).")]
    [SerializeField] private float chargeDashSpeed = 18f;
    [Tooltip("Tempo de charge-up / fill do telegraph antes do dash.")]
    [SerializeField] private float chargeWindupDuration = 1f;
    [SerializeField] private float chargeLaneWidth = 1.1f;
    [SerializeField] private int chargeDashDamage = 15;
    [SerializeField] private LayerMask chargeDamageLayers;
    [SerializeField] private EnemyTelegraphVisualStyle chargeVisualStyle;

    [Header("Follow-up melee (tronco de cone)")]
    [SerializeField] private float meleeInnerRadius = 0.35f;
    [SerializeField] private float meleeOuterRadius = 1.8f;
    [SerializeField] private float meleeLength = 2.4f;
    [SerializeField] private float meleeOpeningAngleDegrees = 40f;
    [SerializeField] private float meleeFillDuration = 0.45f;
    [SerializeField] private int meleeDamage = 18;
    [SerializeField] private LayerMask meleeDamageLayers;
    [SerializeField] private EnemyTelegraphVisualStyle meleeVisualStyle;

    public float RangedWeight => rangedWeight;
    public float ChargeWeight => chargeWeight;
    public float DecisionPause => decisionPause;
    public float MinFleeTime => minFleeTime;
    public float MaxFleeTime => maxFleeTime;
    public float FleeSampleDistance => fleeSampleDistance;
    public float MaxRangedDistance => maxRangedDistance;
    public float FleeDistanceThreshold => fleeDistanceThreshold;
    public float RangedAngle1 => rangedAngle1;
    public float RangedAngle2 => rangedAngle2;
    public float RangedLaneWidth => rangedLaneWidth;
    public float RangedLaneLength => rangedLaneLength;
    public float RangedFillDuration => rangedFillDuration;
    public int RangedDamage => rangedDamage;
    public LayerMask RangedDamageLayers => rangedDamageLayers;
    public EnemyTelegraphVisualStyle RangedVisualStyle => rangedVisualStyle;
    public EnemyAttackPatternDefinition RangedPatternOverride => rangedPatternOverride;
    public float ChargeRange => chargeRange;
    public float ChargeApproachSpeedMultiplier => chargeApproachSpeedMultiplier;
    public float ChargeDashSpeed => chargeDashSpeed;
    public float ChargeWindupDuration => chargeWindupDuration;
    public float ChargeLaneWidth => chargeLaneWidth;
    public int ChargeDashDamage => chargeDashDamage;
    public LayerMask ChargeDamageLayers => chargeDamageLayers;
    public EnemyTelegraphVisualStyle ChargeVisualStyle => chargeVisualStyle;
    public float MeleeInnerRadius => meleeInnerRadius;
    public float MeleeOuterRadius => meleeOuterRadius;
    public float MeleeLength => meleeLength;
    public float MeleeOpeningAngleDegrees => meleeOpeningAngleDegrees;
    public float MeleeFillDuration => meleeFillDuration;
    public int MeleeDamage => meleeDamage;
    public LayerMask MeleeDamageLayers => meleeDamageLayers;
    public EnemyTelegraphVisualStyle MeleeVisualStyle => meleeVisualStyle;

    /// <summary>Sorteia ataque: true = ranged, false = charge.</summary>
    public bool RollRangedAttack()
    {
        float total = Mathf.Max(0f, rangedWeight) + Mathf.Max(0f, chargeWeight);
        if (total <= 0.0001f)
            return true;

        return Random.value * total < Mathf.Max(0f, rangedWeight);
    }

    public LayerMask ResolveDamageLayers(LayerMask configured)
    {
        if (configured.value != 0)
            return configured;
        return 1 << LayerMask.NameToLayer("Player");
    }
}
