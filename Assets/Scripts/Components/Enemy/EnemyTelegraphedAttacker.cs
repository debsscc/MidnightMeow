using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Ataques inimigos com telegraph estilo Hades: zonas preenchíveis antes do dano/projétil.
/// Substitui <see cref="EnemyAttack_Ranged"/> / <see cref="EnemyAttack_Melee"/> quando um
/// <see cref="EnemyAttackPatternDefinition"/> está atribuído.
/// </summary>
[RequireComponent(typeof(EnemyTargetFinder))]
public class EnemyTelegraphedAttacker : MonoBehaviour
{
    [SerializeField] private EnemyAttackPatternDefinition pattern;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private EnemyTelegraphZoneFactory telegraphFactory;
    [SerializeField] private EnemyTelegraphVisualStyle fallbackVisualStyle;

    [Header("Legado (opcional)")]
    [Tooltip("Se preenchido, desativa EnemyAttack_Ranged/Melee no Awake.")]
    [SerializeField] private bool disableLegacyAttackComponents = true;

    private EnemyTargetFinder _targetFinder;
    private EnemyMovement _movement;
    private EnemyHitStun _hitStun;
    private NetworkEnemyTelegraphRelay _relay;
    private float _cooldownTimer;
    private bool _isExecuting;

    public event Action OnAttackWindup;
    public event Action OnAttackResolved;

    /// <summary>Spawn de projétil em rede (servidor). Se null, usa Instantiate local.</summary>
    public Func<GameObject, Vector3, Quaternion, GameObject> ProjectileSpawnDelegate;

    public bool IsExecuting => _isExecuting;
    public bool HasActivePattern => pattern != null && pattern.strikes != null && pattern.strikes.Length > 0;

    public void ConfigureFromInstaller(
        EnemyAttackPatternDefinition newPattern,
        EnemyTelegraphVisualStyle style,
        Transform origin)
    {
        pattern = newPattern;
        if (style != null)
            fallbackVisualStyle = style;
        if (origin != null)
            attackOrigin = origin;
    }

    private void Awake()
    {
        _targetFinder = GetComponent<EnemyTargetFinder>();
        _movement = GetComponent<EnemyMovement>();
        _hitStun = GetComponent<EnemyHitStun>();
        _relay = GetComponent<NetworkEnemyTelegraphRelay>();

        if (attackOrigin == null)
            attackOrigin = transform;

        if (telegraphFactory == null)
            telegraphFactory = GetComponent<EnemyTelegraphZoneFactory>();

        if (disableLegacyAttackComponents && pattern != null)
        {
            if (TryGetComponent<EnemyAttack_Ranged>(out var ranged))
                ranged.enabled = false;
            if (TryGetComponent<EnemyAttack_Melee>(out var melee))
                melee.enabled = false;
        }
    }

    private void Update()
    {
        if (pattern == null || _isExecuting) return;
        if (_hitStun != null && _hitStun.IsStunned) return;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (!IsServerAuthority()) return;

        if (_targetFinder.CurrentTarget == null || _cooldownTimer > 0f) return;
        if (!IsTargetInRange()) return;

        StartCoroutine(ExecutePatternRoutine());
    }

    private bool IsServerAuthority()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsSpawned)
            return true;
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }

    private bool IsTargetInRange()
    {
        var target = _targetFinder.CurrentTarget;
        if (target == null) return false;
        float dist = Vector2.Distance(attackOrigin.position, target.position);
        return dist <= pattern.attackRange;
    }

    private IEnumerator ExecutePatternRoutine()
    {
        _isExecuting = true;
        OnAttackWindup?.Invoke();

        if (_movement != null)
            _movement.SetAttackPaused(true);

        var target = _targetFinder.CurrentTarget;
        var style = pattern.visualStyle != null ? pattern.visualStyle : fallbackVisualStyle;

        if (pattern.strikes != null && telegraphFactory != null)
        {
            foreach (var strike in pattern.strikes)
            {
                if (strike == null) continue;

                if (strike.delayBeforeStart > 0f)
                    yield return new WaitForSeconds(strike.delayBeforeStart);

                TelegraphPoseUtility.TryComputeStrikePose(
                    strike,
                    attackOrigin.position,
                    target,
                    out var worldPos,
                    out var rotation);

                if (_relay != null)
                    _relay.BroadcastTelegraph(strike, style, worldPos, rotation);

                var zone = telegraphFactory.Spawn(
                    strike,
                    style,
                    worldPos,
                    rotation,
                    gameObject,
                    attackOrigin,
                    visualOnly: false,
                    ProjectileSpawnDelegate);

                if (zone != null)
                    yield return new WaitUntil(() => zone == null || zone.IsResolved);
            }
        }

        _cooldownTimer = pattern.cooldown;
        OnAttackResolved?.Invoke();

        if (_movement != null)
            _movement.SetAttackPaused(false);

        _isExecuting = false;
    }

    /// <summary>Dispara um padrão arbitrário (útil para scripts de boss).</summary>
    public void TriggerPattern(EnemyAttackPatternDefinition overridePattern)
    {
        if (!IsServerAuthority() || _isExecuting) return;
        var previous = pattern;
        pattern = overridePattern;
        StartCoroutine(ExecutePatternRoutine());
        pattern = previous;
    }
}
