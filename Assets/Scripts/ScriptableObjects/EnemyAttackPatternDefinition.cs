using System;
using UnityEngine;

[Serializable]
public class TelegraphStrikeDefinition
{
    [Header("Forma")]
    public TelegraphShapeType shape = TelegraphShapeType.Circle;
    [Tooltip("Círculo: X = raio. Retângulo: X = largura, Y = comprimento (eixo local Y).")]
    public Vector2 size = new Vector2(1.2f, 1.2f);
    [Tooltip("Deslocamento local em relação à origem do ataque (antes da rotação).")]
    public Vector2 localOffset;
    [Tooltip("Rotação extra em graus (eixo Z). Para retângulos, Y local aponta na direção do ataque.")]
    public float rotationOffsetDegrees;

    [Header("Tempo")]
    [Tooltip("Espera antes de iniciar este strike (útil para rajadas / destroços).")]
    public float delayBeforeStart;
    [Tooltip("Tempo de preenchimento — janela de esquiva.")]
    public float fillDuration = 0.75f;

    [Header("Origem")]
    [Tooltip("Se true, recalcula posição no alvo no momento do início do strike.")]
    public bool anchorToTargetOnStart = true;
    [Tooltip("Se true e há alvo, aponta o eixo Y do retângulo para o alvo.")]
    public bool aimAtTarget = true;

    [Header("Resolução")]
    public EnemyTelegraphResolution resolution = EnemyTelegraphResolution.AreaDamage;
    public int damage = 10;
    [Tooltip("Layers que recebem dano em área.")]
    public LayerMask damageLayers;
    [Tooltip("Efeito na zona (AreaDamage): partículas/animação ao resolver — sem projétil físico.")]
    public GameObject effectPrefab;
    [Tooltip("Visual que viaja do inimigo até a zona (ProjectileToZone). Dano só na zona.")]
    public GameObject travelVisualPrefab;
    [Tooltip("Velocidade do visual em direção à zona.")]
    public float travelSpeed = 12f;
    [Tooltip("Legado: use travelVisualPrefab. Se só isto estiver preenchido, trata como travelVisualPrefab.")]
    public GameObject projectilePrefab;
    [Tooltip("Legado — ignorado; use travelSpeed.")]
    public float projectileSpeedOverride;

    [Header("Visual")]
    public TelegraphFillMode fillMode = TelegraphFillMode.ExpandFromOrigin;
}

[CreateAssetMenu(fileName = "EnemyAttackPattern", menuName = "MidnightMeow/Combat/Enemy Attack Pattern")]
public class EnemyAttackPatternDefinition : ScriptableObject
{
    [Header("Condições")]
    [Tooltip("Distância máxima ao alvo para iniciar o padrão.")]
    public float attackRange = 8f;
    public float cooldown = 2.5f;
    [Tooltip("Pausa IA durante o padrão inteiro.")]
    public bool stopMovementDuringPattern = true;

    [Header("Strikes")]
    [Tooltip("Um ou mais telegraphs (ex.: vários círculos de destroços).")]
    public TelegraphStrikeDefinition[] strikes = Array.Empty<TelegraphStrikeDefinition>();

    [Header("Visual padrão")]
    public EnemyTelegraphVisualStyle visualStyle;
}
