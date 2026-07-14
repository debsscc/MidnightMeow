///* ----------------------------------------------------------------
// ATUALIZADO EM: 14-07-2026
// DESCRIÇÃO: FSM server-authoritative do Rei Rato — sorteio, fuga+5 faixas, investida+melee cone.
// ---------------------------------------------------------------- */

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador principal do Boss. Toda decisão/física no servidor; clientes só recebem telegraphs/anims.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BossEnemyMarker))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyTelegraphZoneFactory))]
public class RatKingController : MonoBehaviour
{
    private enum BossAttackKind
    {
        Ranged,
        Charge
    }

    [Header("Config")]
    [Tooltip("SO de balanceamento (pesos, ângulos, tempos, danos).")]
    [SerializeField] private RatKingBehaviorConfig config;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private EnemyTelegraphZoneFactory telegraphFactory;
    [SerializeField] private EnemyTelegraphedAttacker telegraphedAttacker;
    [SerializeField] private bool disableLegacyAttacks = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private EnemyMovement _movement;
    private NetworkEnemyController _networkEnemy;
    private NetworkEnemyTelegraphRelay _relay;
    private HealthComponent _health;
    private NavMeshAgent _agent;
    private EnemyHitStun _hitStun;

    private Coroutine _brainRoutine;
    private Transform _currentTarget;
    private bool _isBusy;
    private readonly HashSet<int> _dashHitInstanceIds = new HashSet<int>();

    /// <summary>True enquanto qualquer ataque do boss está em execução (anima IsAttacking).</summary>
    public bool IsAttackBusy => _isBusy;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _networkEnemy = GetComponent<NetworkEnemyController>();
        _relay = GetComponent<NetworkEnemyTelegraphRelay>();
        _health = GetComponent<HealthComponent>();
        _agent = GetComponent<NavMeshAgent>();
        _hitStun = GetComponent<EnemyHitStun>();

        if (telegraphFactory == null)
            telegraphFactory = GetComponent<EnemyTelegraphZoneFactory>();
        if (telegraphedAttacker == null)
            telegraphedAttacker = GetComponent<EnemyTelegraphedAttacker>();
        if (attackOrigin == null)
            attackOrigin = transform;

        if (disableLegacyAttacks)
        {
            if (TryGetComponent<EnemyAttack_Ranged>(out var ranged))
                ranged.enabled = false;
            if (TryGetComponent<EnemyAttack_Melee>(out var melee))
                melee.enabled = false;
        }

        // Boss assume o combate; evita o Update automático do pattern genérico.
        if (telegraphedAttacker != null)
            telegraphedAttacker.enabled = false;
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnDied.AddListener(HandleDied);
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDied.RemoveListener(HandleDied);
        StopBrain();
    }

    private void Start()
    {
        // Solo sem NetworkObject spawnado, ou host já spawnado.
        TryStartBrainIfAuthoritative();
    }

    /// <summary>Chamado por <see cref="NetworkEnemyController"/> no OnNetworkSpawn do servidor.</summary>
    public void ServerEnsureBrain()
    {
        TryStartBrainIfAuthoritative();
    }

    private void TryStartBrainIfAuthoritative()
    {
        if (!IsServerAuthority())
            return;

        if (_brainRoutine != null)
            return;

        if (config == null)
        {
            Debug.LogError($"[RatKingController] {name}: atribua um RatKingBehaviorConfig.", this);
            enabled = false;
            return;
        }

        _brainRoutine = StartCoroutine(BrainLoop());
    }

    private void HandleDied()
    {
        StopBrain();
        _movement?.EndManualNavigation();
        _movement?.ResetSpeedMultiplier();
        _movement?.SetAttackPaused(false);
        _networkEnemy?.ServerNotifyChargeEnd();
    }

    private void StopBrain()
    {
        if (_brainRoutine != null)
        {
            StopCoroutine(_brainRoutine);
            _brainRoutine = null;
        }

        _isBusy = false;
    }

    private bool IsServerAuthority()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj == null)
            return true;

        if (!netObj.IsSpawned)
        {
            // Offline / sem sessão NGO ativa: permite rodar no Editor solo.
            return NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;
        }

        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }

    private IEnumerator BrainLoop()
    {
        // Pequeno atraso para NavMesh/spawn estabilizarem.
        yield return null;

        while (enabled && _health != null && _health.IsAlive)
        {
            if (GameEvents.IsPaused || (_hitStun != null && _hitStun.IsStunned))
            {
                yield return null;
                continue;
            }

            yield return DecisionAndExecute();

            float pause = Mathf.Max(0f, config.DecisionPause);
            if (pause > 0f)
                yield return new WaitForSeconds(pause);
        }
    }

    private IEnumerator DecisionAndExecute()
    {
        _currentTarget = FindNearestLivingPlayer();
        if (_currentTarget == null)
        {
            yield return null;
            yield break;
        }

        BossAttackKind kind = config.RollRangedAttack() ? BossAttackKind.Ranged : BossAttackKind.Charge;
        if (debugLogs)
            Debug.Log($"[RatKing] Decision → {kind} target={_currentTarget.name}", this);

        _isBusy = true;
        if (kind == BossAttackKind.Ranged)
            yield return ExecuteRangedAttack(_currentTarget);
        else
            yield return ExecuteChargeAttack(_currentTarget);
        _isBusy = false;
    }

    #region Ranged

    private IEnumerator ExecuteRangedAttack(Transform target)
    {
        // 1) Fuga: timer como fallback + early exit por distância (threshold do SO).
        float fleeTime = Random.Range(config.MinFleeTime, config.MaxFleeTime);
        float attackThreshold = config.MaxRangedDistance * config.FleeDistanceThreshold;
        _movement.BeginManualNavigation();
        _movement.ResetSpeedMultiplier();

        float elapsed = 0f;
        while (elapsed < fleeTime && target != null && _health.IsAlive)
        {
            if (GameEvents.IsPaused)
            {
                yield return null;
                continue;
            }

            // Early exit: já está longe o bastante para atacar sem sair da arena.
            float currentDistance = Vector3.Distance(transform.position, target.position);
            if (currentDistance >= attackThreshold)
            {
                if (debugLogs)
                    Debug.Log($"[RatKing] Flee early-exit dist={currentDistance:F2} >= threshold={attackThreshold:F2}", this);
                break;
            }

            Vector2 away = ((Vector2)transform.position - (Vector2)target.position).normalized;
            if (away.sqrMagnitude < 0.0001f)
                away = Random.insideUnitCircle.normalized;

            _movement.SetManualDirection(away, config.FleeSampleDistance);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Para o NavMesh imediatamente (zera velocidade) e entra no ataque.
        _movement.EndManualNavigation();
        _movement.SetAttackPaused(true);

        // 2) 5 faixas simultâneas
        if (target != null)
            _movement.FaceDirection((Vector2)target.position - (Vector2)transform.position);

        _networkEnemy?.ServerNotifySpellCast();

        if (config.RangedPatternOverride != null && telegraphedAttacker != null)
        {
            telegraphedAttacker.enabled = true;
            telegraphedAttacker.TriggerPattern(config.RangedPatternOverride);
            yield return new WaitUntil(() => !telegraphedAttacker.IsExecuting);
            telegraphedAttacker.enabled = false;
        }
        else
        {
            yield return SpawnFiveLaneTelegraphs(target);
        }

        _movement.SetAttackPaused(false);
    }

    private IEnumerator SpawnFiveLaneTelegraphs(Transform target)
    {
        if (target == null || telegraphFactory == null)
            yield break;

        Vector2 origin = attackOrigin.position;
        Vector2 toTarget = ((Vector2)target.position - origin);
        if (toTarget.sqrMagnitude < 0.0001f)
            toTarget = Vector2.up;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;

        float[] angleOffsets =
        {
            -config.RangedAngle2,
            -config.RangedAngle1,
            0f,
            config.RangedAngle1,
            config.RangedAngle2
        };

        var style = config.RangedVisualStyle;
        var zones = new List<EnemyTelegraphZoneInstance>(5);

        for (int i = 0; i < angleOffsets.Length; i++)
        {
            var strike = BuildRangedLaneStrike();
            float rotation = baseAngle + angleOffsets[i];
            float rad = (rotation + 90f) * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 worldPos = origin + forward * (config.RangedLaneLength * 0.5f);

            BroadcastTelegraph(strike, style, worldPos, rotation, origin);

            var zone = telegraphFactory.Spawn(
                strike,
                style,
                worldPos,
                rotation,
                gameObject,
                attackOrigin,
                visualOnly: false);
            if (zone != null)
                zones.Add(zone);
        }

        yield return WaitUntilAllResolved(zones);
    }

    private TelegraphStrikeDefinition BuildRangedLaneStrike()
    {
        return new TelegraphStrikeDefinition
        {
            shape = TelegraphShapeType.Rectangle,
            size = new Vector2(config.RangedLaneWidth, config.RangedLaneLength),
            fillDuration = config.RangedFillDuration,
            anchorToTargetOnStart = false,
            aimAtTarget = false,
            resolution = EnemyTelegraphResolution.AreaDamage,
            damage = config.RangedDamage,
            damageLayers = config.ResolveDamageLayers(config.RangedDamageLayers),
            fillMode = TelegraphFillMode.ExpandFromOrigin
        };
    }

    #endregion

    #region Charge

    private IEnumerator ExecuteChargeAttack(Transform target)
    {
        if (target == null)
            yield break;

        float approachThreshold = config.ChargeRange * 0.6f;

        // 1) Aproximação rápida
        _movement.BeginManualNavigation();
        _movement.SetSpeedMultiplier(config.ChargeApproachSpeedMultiplier);

        float approachTimeout = 8f;
        float approachElapsed = 0f;
        while (target != null && _health.IsAlive && approachElapsed < approachTimeout)
        {
            if (GameEvents.IsPaused)
            {
                yield return null;
                continue;
            }

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= approachThreshold)
                break;

            _movement.SetManualDestination(target.position);
            approachElapsed += Time.deltaTime;
            yield return null;
        }

        _movement.ResetSpeedMultiplier();
        _movement.EndManualNavigation();
        _movement.SetAttackPaused(true);

        // Trava direção no instante do charge-up
        Vector2 lockDir = target != null
            ? ((Vector2)target.position - (Vector2)transform.position).normalized
            : Vector2.right;
        if (lockDir.sqrMagnitude < 0.0001f)
            lockDir = Vector2.right;

        _movement.FaceDirection(lockDir);
        float rotation = Mathf.Atan2(lockDir.y, lockDir.x) * Mathf.Rad2Deg - 90f;

        // 2) Charge-up + telegraph da trajetória
        _networkEnemy?.ServerNotifyChargeStart();

        var chargeStrike = BuildChargeLaneStrike();
        Vector2 origin = attackOrigin.position;
        Vector2 telegraphCenter = origin + lockDir * (config.ChargeRange * 0.5f);
        var chargeStyle = config.ChargeVisualStyle;

        BroadcastTelegraph(chargeStrike, chargeStyle, telegraphCenter, rotation, origin);

        EnemyTelegraphZoneInstance chargeZone = null;
        if (telegraphFactory != null)
        {
            chargeZone = telegraphFactory.Spawn(
                chargeStrike,
                chargeStyle,
                telegraphCenter,
                rotation,
                gameObject,
                attackOrigin,
                visualOnly: false);
        }

        // Windup = fill do telegraph; ao completar, inicia o dash.
        if (chargeZone != null)
            yield return new WaitUntil(() => chargeZone == null || chargeZone.IsResolved);
        else
            yield return new WaitForSeconds(Mathf.Max(0.05f, config.ChargeWindupDuration));

        // 3) Dash
        yield return PerformDash(lockDir);

        // 4) Melee tronco de cone
        yield return SpawnMeleeConeFollowUp(lockDir, rotation);

        _networkEnemy?.ServerNotifyChargeEnd();
        _movement.SetAttackPaused(false);
    }

    private TelegraphStrikeDefinition BuildChargeLaneStrike()
    {
        return new TelegraphStrikeDefinition
        {
            shape = TelegraphShapeType.Rectangle,
            size = new Vector2(config.ChargeLaneWidth, config.ChargeRange),
            fillDuration = config.ChargeWindupDuration,
            anchorToTargetOnStart = false,
            aimAtTarget = false,
            // Aviso visual apenas — o dano da investida é aplicado no dash (overlap).
            resolution = EnemyTelegraphResolution.AreaDamage,
            damage = 0,
            damageLayers = config.ResolveDamageLayers(config.ChargeDamageLayers),
            fillMode = TelegraphFillMode.ExpandFromOrigin
        };
    }

    private IEnumerator PerformDash(Vector2 direction)
    {
        _dashHitInstanceIds.Clear();
        Vector2 start = transform.position;
        float distance = config.ChargeRange;
        float speed = Mathf.Max(1f, config.ChargeDashSpeed);
        float duration = distance / speed;
        float elapsed = 0f;

        bool agentWasEnabled = _agent != null && _agent.enabled;
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.enabled = false;
        }

        LayerMask mask = config.ResolveDamageLayers(config.ChargeDamageLayers);
        float hitboxWidth = config.ChargeLaneWidth;

        while (elapsed < duration && _health != null && _health.IsAlive)
        {
            if (GameEvents.IsPaused)
            {
                yield return null;
                continue;
            }

            float t = Mathf.Clamp01(elapsed / duration);
            Vector2 next = start + direction * (distance * t);
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            ApplyDashOverlapDamage(next, direction, hitboxWidth, mask);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector2 end = start + direction * distance;
        transform.position = new Vector3(end.x, end.y, transform.position.z);
        ApplyDashOverlapDamage(end, direction, hitboxWidth, mask);

        if (_agent != null && agentWasEnabled)
        {
            _agent.enabled = true;
            if (_agent.isOnNavMesh)
                _agent.Warp(transform.position);
        }
    }

    private void ApplyDashOverlapDamage(Vector2 center, Vector2 direction, float width, LayerMask mask)
    {
        if (config.ChargeDashDamage <= 0)
            return;

        Vector2 size = new Vector2(width, Mathf.Max(0.4f, width));
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, mask);
        foreach (var col in hits)
        {
            if (col == null)
                continue;

            int id = col.GetInstanceID();
            if (!_dashHitInstanceIds.Add(id))
                continue;

            // I-frames / dash: PlayerCombatUtility → NetworkPlayerHealth já rejeita NetworkIsDashing.
            PlayerCombatUtility.TryApplyDamage(col, config.ChargeDashDamage, gameObject);
        }
    }

    private IEnumerator SpawnMeleeConeFollowUp(Vector2 direction, float rotationDegrees)
    {
        _networkEnemy?.ServerNotifyMeleeAttack();

        var strike = BuildMeleeConeStrike();
        Vector2 origin = attackOrigin.position;
        Vector2 worldPos = origin + direction * (config.MeleeLength * 0.5f);
        var style = config.MeleeVisualStyle;

        BroadcastTelegraph(strike, style, worldPos, rotationDegrees, origin);

        EnemyTelegraphZoneInstance zone = null;
        if (telegraphFactory != null)
        {
            zone = telegraphFactory.Spawn(
                strike,
                style,
                worldPos,
                rotationDegrees,
                gameObject,
                attackOrigin,
                visualOnly: false);
        }

        if (zone != null)
            yield return new WaitUntil(() => zone == null || zone.IsResolved);
        else
            yield return new WaitForSeconds(Mathf.Max(0.05f, config.MeleeFillDuration));
    }

    private TelegraphStrikeDefinition BuildMeleeConeStrike()
    {
        return new TelegraphStrikeDefinition
        {
            shape = TelegraphShapeType.ConeFrustum,
            size = new Vector2(config.MeleeInnerRadius, config.MeleeLength),
            coneInnerRadius = config.MeleeInnerRadius,
            coneOuterRadius = config.MeleeOuterRadius,
            coneOpeningAngleDegrees = config.MeleeOpeningAngleDegrees,
            fillDuration = config.MeleeFillDuration,
            anchorToTargetOnStart = false,
            aimAtTarget = false,
            resolution = EnemyTelegraphResolution.AreaDamage,
            damage = config.MeleeDamage,
            damageLayers = config.ResolveDamageLayers(config.MeleeDamageLayers),
            fillMode = TelegraphFillMode.ExpandFromOrigin
        };
    }

    #endregion

    #region Targeting / telegraph helpers

    private Transform FindNearestLivingPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float best = float.MaxValue;

        foreach (var go in players)
        {
            if (!IsValidLivingPlayer(go))
                continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);
            if (dist < best)
            {
                best = dist;
                nearest = go.transform;
            }
        }

        return nearest;
    }

    private static bool IsValidLivingPlayer(GameObject go)
    {
        if (go == null || !go.activeInHierarchy)
            return false;

        var netHealth = go.GetComponent<NetworkPlayerHealth>();
        if (netHealth != null && netHealth.IsSpawned)
            return netHealth.CanBeTargeted;

        var health = go.GetComponent<HealthComponent>();
        return health == null || health.IsAlive;
    }

    private void BroadcastTelegraph(
        TelegraphStrikeDefinition strike,
        EnemyTelegraphVisualStyle style,
        Vector2 worldPos,
        float rotation,
        Vector2 travelSpawn)
    {
        if (_networkEnemy != null)
        {
            _networkEnemy.BroadcastTelegraphToClients(strike, style, worldPos, rotation, travelSpawn);
            return;
        }

        _relay?.BroadcastTelegraph(strike, style, worldPos, rotation, travelSpawn);
    }

    private static IEnumerator WaitUntilAllResolved(List<EnemyTelegraphZoneInstance> zones)
    {
        if (zones == null || zones.Count == 0)
            yield break;

        bool anyPending;
        do
        {
            anyPending = false;
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z != null && !z.IsResolved)
                {
                    anyPending = true;
                    break;
                }
            }

            if (anyPending)
                yield return null;
        } while (anyPending);
    }

    #endregion
}
