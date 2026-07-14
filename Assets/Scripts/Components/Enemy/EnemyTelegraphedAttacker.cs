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
    private NetworkEnemyController _networkEnemy;
    private NetworkEnemyTelegraphRelay _relay;
    private float _cooldownTimer;
    private Coroutine _patternRoutine;
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

        EnsureTelegraphWiring();
    }

    public void EnsureTelegraphWiring()
    {
        if (telegraphFactory == null)
            telegraphFactory = GetComponent<EnemyTelegraphZoneFactory>();
        if (_networkEnemy == null)
            _networkEnemy = GetComponent<NetworkEnemyController>();
        if (_relay == null)
            _relay = GetComponent<NetworkEnemyTelegraphRelay>();
    }

    private void Awake()
    {
        _targetFinder = GetComponent<EnemyTargetFinder>();
        _movement = GetComponent<EnemyMovement>();
        _hitStun = GetComponent<EnemyHitStun>();
        EnsureTelegraphWiring();

        if (attackOrigin == null)
            attackOrigin = transform;

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
        if (GameEvents.IsPaused) return;
        if (pattern == null || _isExecuting) return;
        if (_hitStun != null && _hitStun.IsStunned) return;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (!IsServerAuthority()) return;

        if (_targetFinder.CurrentTarget == null || _cooldownTimer > 0f) return;
        if (!IsTargetInRange()) return;

        _patternRoutine = StartCoroutine(ExecutePatternRoutine());
    }

    private void LateUpdate()
    {
        if (_isExecuting && _hitStun != null && _hitStun.IsStunned)
            FreezeForPause();
    }

    public void FreezeForPause()
    {
        if (_patternRoutine != null)
        {
            StopCoroutine(_patternRoutine);
            _patternRoutine = null;
        }

        _isExecuting = false;
        _cooldownTimer = Mathf.Max(_cooldownTimer, 0.25f);

        if (_movement != null)
            _movement.SetAttackPaused(false);
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

        EnsureTelegraphWiring();

        if (pattern.strikes != null)
        {
            if (telegraphFactory == null)
                telegraphFactory = GetComponent<EnemyTelegraphZoneFactory>();

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

                Vector2 travelSpawn = attackOrigin != null
                    ? (Vector2)attackOrigin.position
                    : (Vector2)transform.position;
                BroadcastTelegraphVisualToClients(strike, style, worldPos, rotation, travelSpawn);

                if (telegraphFactory == null)
                    continue;

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
        _patternRoutine = null;
    }

    private void BroadcastTelegraphVisualToClients(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPos,
        float rotation,
        Vector2 travelSpawnPosition)
    {
        EnsureTelegraphWiring();

        if (_networkEnemy != null)
        {
            _networkEnemy.BroadcastTelegraphToClients(
                strike, style, worldPos, rotation, travelSpawnPosition);
            return;
        }

        _relay?.BroadcastTelegraph(strike, style, worldPos, rotation, travelSpawnPosition);
    }

    /// <summary>Dispara um padrão arbitrário (útil para scripts de boss).</summary>
    public void TriggerPattern(EnemyAttackPatternDefinition overridePattern)
    {
        if (!IsServerAuthority() || _isExecuting) return;
        var previous = pattern;
        pattern = overridePattern;
        _patternRoutine = StartCoroutine(ExecutePatternRoutine());
        pattern = previous;
    }
}
