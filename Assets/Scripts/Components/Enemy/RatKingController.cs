///* ----------------------------------------------------------------
// ATUALIZADO EM: 17-07-2026
// DESCRIÇÃO: FSM server-authoritative do Rei Rato — sorteio, fuga+5 faixas, investida+melee cone.
// Fuga/Dash: CircleCast 2D contra obstáculos + Rigidbody2D.MovePosition (sem transform.position).
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
[RequireComponent(typeof(Rigidbody2D))]
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

    [Header("Obstáculos (anti-tunneling)")]
    [Tooltip("Layers de parede/cenário. Deve incluir Wall (e DashableWall se o boss não puder atravessar).")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Raio do CircleCast — case com ~metade do menor eixo do CapsuleCollider2D (escala world).")]
    [SerializeField] private float obstacleCheckRadius = 0.55f;
    [Tooltip("Folga extra ao recuar o destino do Cast (evita clip na textura da parede).")]
    [SerializeField] private float obstacleSkin = 0.08f;
    [Tooltip("Distância livre mínima na fuga; abaixo disso ataca a distância imediatamente.")]
    [SerializeField] private float minFleeClearance = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private EnemyMovement _movement;
    private NetworkEnemyController _networkEnemy;
    private NetworkEnemyTelegraphRelay _relay;
    private HealthComponent _health;
    private NavMeshAgent _agent;
    private EnemyHitStun _hitStun;
    private Rigidbody2D _rb;
    private EnemyPhysicsBody _physicsBody;

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
        _rb = GetComponent<Rigidbody2D>();
        _physicsBody = GetComponent<EnemyPhysicsBody>();

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
        EndPhysicsDrivenMotion();
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
        // 1) Fuga: timer como fallback + early exit por distância / parede.
        float fleeTime = Random.Range(config.MinFleeTime, config.MaxFleeTime);
        float attackThreshold = config.MaxRangedDistance * config.FleeDistanceThreshold;
        _movement.BeginManualNavigation();
        _movement.ResetSpeedMultiplier();
        BeginPhysicsDrivenMotion();

        float moveSpeed = _movement.Stats != null ? _movement.Stats.moveSpeed : 5f;
        float elapsed = 0f;
        while (elapsed < fleeTime && target != null && _health.IsAlive)
        {
            if (GameEvents.IsPaused)
            {
                yield return null;
                continue;
            }

            Vector2 bossPos = _rb != null ? _rb.position : (Vector2)transform.position;
            float currentDistance = Vector2.Distance(bossPos, target.position);
            if (currentDistance >= attackThreshold)
            {
                if (debugLogs)
                    Debug.Log($"[RatKing] Flee early-exit dist={currentDistance:F2} >= threshold={attackThreshold:F2}", this);
                break;
            }

            Vector2 away = (bossPos - (Vector2)target.position).normalized;
            if (away.sqrMagnitude < 0.0001f)
                away = Random.insideUnitCircle.normalized;

            Vector2 fleeDir = ResolveFleeDirection(bossPos, away);
            if (fleeDir.sqrMagnitude < 0.0001f)
            {
                if (debugLogs)
                    Debug.Log("[RatKing] Flee blocked by obstacle — attacking early.", this);
                break;
            }

            float clear = GetClearDistance(bossPos, fleeDir, config.FleeSampleDistance);
            float step = moveSpeed * Time.deltaTime;
            float maxStep = Mathf.Max(0f, clear - obstacleSkin);
            if (maxStep <= 0.001f)
            {
                if (debugLogs)
                    Debug.Log("[RatKing] Flee clearance exhausted — attacking early.", this);
                break;
            }

            Vector2 next = bossPos + fleeDir * Mathf.Min(step, maxStep);
            MoveBossPosition(next);
            _movement.FaceDirection(fleeDir);

            elapsed += Time.deltaTime;
            yield return null;
        }

        EndPhysicsDrivenMotion();
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

        // 1) Aproximação rápida (NavMesh — ainda dentro da walkable area)
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

        // Destino do dash limitado por parede — telegraph usa o comprimento real.
        Vector2 origin = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        float dashDistance = ClampTravelDistance(origin, lockDir, config.ChargeRange);

        // 2) Charge-up + telegraph da trajetória (já encurtada)
        _networkEnemy?.ServerNotifyChargeStart();

        var chargeStrike = BuildChargeLaneStrike(dashDistance);
        Vector2 telegraphCenter = origin + lockDir * (dashDistance * 0.5f);
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

        // 3) Dash físico até o destino clampado
        yield return PerformDash(lockDir, dashDistance);

        // 4) Melee tronco de cone
        yield return SpawnMeleeConeFollowUp(lockDir, rotation);

        _networkEnemy?.ServerNotifyChargeEnd();
        _movement.SetAttackPaused(false);
    }

    private TelegraphStrikeDefinition BuildChargeLaneStrike(float dashDistance)
    {
        return new TelegraphStrikeDefinition
        {
            shape = TelegraphShapeType.Rectangle,
            size = new Vector2(config.ChargeLaneWidth, dashDistance),
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

    private IEnumerator PerformDash(Vector2 direction, float distance)
    {
        _dashHitInstanceIds.Clear();
        Vector2 start = _rb != null ? _rb.position : (Vector2)transform.position;
        distance = Mathf.Max(0f, distance);
        float speed = Mathf.Max(1f, config.ChargeDashSpeed);
        float duration = distance > 0.001f ? distance / speed : 0f;
        float elapsed = 0f;

        bool agentWasEnabled = _agent != null && _agent.enabled;
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.enabled = false;
        }

        BeginPhysicsDrivenMotion();

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
            MoveBossPosition(next);

            ApplyDashOverlapDamage(next, direction, hitboxWidth, mask);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector2 end = start + direction * distance;
        MoveBossPosition(end);
        ApplyDashOverlapDamage(end, direction, hitboxWidth, mask);

        EndPhysicsDrivenMotion();

        if (_agent != null && agentWasEnabled)
        {
            _agent.enabled = true;
            if (_agent.isOnNavMesh)
                _agent.Warp(_rb != null ? (Vector3)_rb.position : transform.position);
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

    #region Obstacle casts / physics motion

    /// <summary>
    /// Limita a distância de viagem com CircleCast 2D (equivalente a SphereCast em 3D).
    /// Retorna a distância que o centro do corpo pode avançar sem clipar na parede.
    /// </summary>
    private float ClampTravelDistance(Vector2 origin, Vector2 direction, float desiredDistance)
    {
        if (desiredDistance <= 0f || direction.sqrMagnitude < 0.0001f)
            return 0f;

        if (obstacleLayer.value == 0)
        {
            if (debugLogs)
                Debug.LogWarning("[RatKing] obstacleLayer não configurada — dash sem clamp de parede.", this);
            return desiredDistance;
        }

        if (!TryGetObstacleHit(origin, direction.normalized, desiredDistance, out RaycastHit2D hit))
            return desiredDistance;

        // hit.distance = quanto o centro do círculo andou até o contato; recua skin + fração do raio.
        float radius = GetCastRadius();
        float clamped = hit.distance - obstacleSkin - radius * 0.15f;
        return Mathf.Clamp(clamped, 0f, desiredDistance);
    }

    private float GetClearDistance(Vector2 origin, Vector2 direction, float maxDistance)
    {
        if (direction.sqrMagnitude < 0.0001f || maxDistance <= 0f)
            return 0f;

        if (obstacleLayer.value == 0)
            return maxDistance;

        if (!TryGetObstacleHit(origin, direction.normalized, maxDistance, out RaycastHit2D hit))
            return maxDistance;

        float radius = GetCastRadius();
        return Mathf.Max(0f, hit.distance - obstacleSkin - radius * 0.15f);
    }

    /// <summary>
    /// Direção de fuga: tenta afastamento; se parede próxima, desliza tangencialmente; se bloqueado, zero.
    /// </summary>
    private Vector2 ResolveFleeDirection(Vector2 origin, Vector2 away)
    {
        float clearAway = GetClearDistance(origin, away, config.FleeSampleDistance);
        if (clearAway >= minFleeClearance)
            return away;

        Vector2 tangentA = new Vector2(-away.y, away.x);
        Vector2 tangentB = -tangentA;
        float clearA = GetClearDistance(origin, tangentA, config.FleeSampleDistance);
        float clearB = GetClearDistance(origin, tangentB, config.FleeSampleDistance);

        if (clearA >= clearB && clearA >= minFleeClearance)
            return tangentA;
        if (clearB >= minFleeClearance)
            return tangentB;

        return Vector2.zero;
    }

    private bool TryGetObstacleHit(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D closest)
    {
        closest = default;
        float radius = GetCastRadius();
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, direction, distance, obstacleLayer);
        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null)
                continue;
            if (IsOwnCollider(hit.collider))
                continue;

            if (hit.distance < best)
            {
                best = hit.distance;
                closest = hit;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Usa o maior entre o valor do Inspector e metade do menor eixo do CapsuleCollider (world),
    /// para o Cast não “caber” num vão menor que o corpo real.
    /// </summary>
    private float GetCastRadius()
    {
        float configured = Mathf.Max(0.05f, obstacleCheckRadius);
        if (!TryGetComponent<CapsuleCollider2D>(out var capsule))
            return configured;

        Vector3 lossy = transform.lossyScale;
        float halfMin = Mathf.Min(
            capsule.size.x * Mathf.Abs(lossy.x),
            capsule.size.y * Mathf.Abs(lossy.y)) * 0.5f;
        return Mathf.Max(configured, halfMin);
    }

    private bool IsOwnCollider(Collider2D col)
    {
        if (col == null)
            return false;
        if (col.transform == transform || col.transform.IsChildOf(transform))
            return true;
        if (_rb != null && col.attachedRigidbody == _rb)
            return true;
        return false;
    }

    private void MoveBossPosition(Vector2 worldPosition)
    {
        if (_rb != null && _rb.simulated)
        {
            _rb.MovePosition(worldPosition);
            return;
        }

        // Fallback extremo (sem RB) — não deve ocorrer com RequireComponent.
        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
    }

    private void BeginPhysicsDrivenMotion()
    {
        if (_physicsBody != null && !_physicsBody.IsExternalPhysicsActive)
            _physicsBody.BeginExternalPhysics();

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.updatePosition = false;
        }
    }

    private void EndPhysicsDrivenMotion()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        if (_physicsBody != null && _physicsBody.IsExternalPhysicsActive)
            _physicsBody.EndExternalPhysics();

        if (_agent != null && _agent.enabled)
        {
            if (_agent.isOnNavMesh)
                _agent.Warp(_rb != null ? (Vector3)_rb.position : transform.position);
            _agent.updatePosition = true;
            _agent.isStopped = false;
        }
    }

    #endregion
}
