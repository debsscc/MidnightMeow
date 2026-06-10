///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla o comportamento de um projétil que pode quicar em paredes e ser coletado como munição.
// ---------------------------------------------------------------- */

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileStats stats;
    private float _damageMultiplier = 1f;

    private Rigidbody2D _rb;
    private int _currentBounces = 0;
    private int _maxBounces;
    private bool _canBeCollected = false;

    [Header("Animation")]
    [SerializeField] private Animator _projectileAnimator;
    [SerializeField] private float _hitAnimDuration = 0.3f;
    [SerializeField] private bool _playHitOnExpire = false;

    private bool _hasHit = false;
    private readonly HashSet<int> _damagedEnemyRootIds = new HashSet<int>();
    private static readonly int _hashOnHit = Animator.StringToHash("OnHit");

    private static int _enemyLayer = -1;
    private static int _wallLayer = -1;
    private static int _structureLayer = -1;
    private static bool _projectileLayerCollisionConfigured;

    private enum ProjectileState { Fired, Seeking }
    private ProjectileState _currentState = ProjectileState.Fired;

    private Transform _seekTarget;
    private float _seekSpeed;
    private Vector2 _travelDirection;
    private bool _hasTravelDirection;
    private Vector2 _spawnPosition;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _maxBounces = stats.maxBounces;
        EnsureProjectileLayerCollisions();
        ConfigureCombatColliders();
        if (_rb != null)
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void ConfigureCombatColliders()
    {
        EnsureProjectileLayerCollisions();
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col != null)
                col.excludeLayers = 0;
        }
    }

    private static void EnsureProjectileLayerCollisions()
    {
        if (_projectileLayerCollisionConfigured) return;

        int projectileLayer = LayerMask.NameToLayer("Projectile");
        if (projectileLayer < 0) return;

        Physics2D.IgnoreLayerCollision(projectileLayer, projectileLayer, true);

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            Physics2D.IgnoreLayerCollision(projectileLayer, playerLayer, true);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(projectileLayer, enemyLayer, false);

        _enemyLayer = enemyLayer;
        _wallLayer = LayerMask.NameToLayer("Wall");
        _structureLayer = LayerMask.NameToLayer("Structure");
        _projectileLayerCollisionConfigured = true;
    }

    private void Start()
    {
        _spawnPosition = transform.position;
        Vector2 initialDirection = _hasTravelDirection ? _travelDirection : (Vector2)transform.up;
        SetTravelDirection(initialDirection, stats.moveSpeed);
    }

    private void FixedUpdate()
    {
        TrySweepHitAlongVelocity();
    }

    private void TrySweepHitAlongVelocity()
    {
        if (_hasHit || _rb == null || !_rb.simulated) return;

        Vector2 velocity = _rb.linearVelocity;
        float speed = velocity.magnitude;
        if (speed < 0.01f) return;

        if (!CanApplyGameplayHit(out _))
            return;

        if (_enemyLayer < 0)
            _enemyLayer = LayerMask.NameToLayer("Enemy");

        float distance = speed * Time.fixedDeltaTime * 1.25f + 0.05f;
        var hit = Physics2D.Raycast(transform.position, velocity.normalized, distance, 1 << _enemyLayer);
        if (hit.collider != null)
            ProcessEnemyHit(hit.collider, null);
    }

    private void Update()
    {
        if (stats.maxDistance > 0 && _currentState != ProjectileState.Seeking &&
            Vector2.Distance(_spawnPosition, transform.position) >= stats.maxDistance)
        {
            if (_playHitOnExpire)
                TriggerHitAndDestroy();
            else
                Destroy(gameObject);
        }

        if (_currentState == ProjectileState.Seeking && _seekTarget != null)
        {
            Vector2 direction = (_seekTarget.position - transform.position).normalized;
            _rb.linearVelocity = direction * _seekSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasHit || _currentState == ProjectileState.Seeking) return;
        if (IsOtherPlayerProjectile(collision.collider)) return;

        if (IsEnemyCollider(collision.collider))
        {
            ProcessEnemyHit(collision.collider, collision);
            return;
        }

        if (collision.gameObject.layer == _wallLayer || collision.gameObject.layer == _structureLayer)
            HandleWallBounce();
    }

    private void OnTriggerEnter2D(Collider2D other) => TryTriggerInteraction(other);

    private void TryTriggerInteraction(Collider2D other)
    {
        if (_hasHit || IsOtherPlayerProjectile(other)) return;

        if (IsEnemyCollider(other))
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (_canBeCollected && stats.collectable)
            {
                GameEvents.InvokeAmmoCollected();
                Destroy(gameObject);
            }
            return;
        }

        if (other.gameObject.layer == _wallLayer || other.gameObject.layer == _structureLayer)
            return;
    }

    private void ProcessEnemyHit(Collider2D hitCollider, Collision2D collision)
    {
        if (_hasHit || hitCollider == null || !CanApplyGameplayHit(out _))
            return;

        if (!TryRegisterEnemyHit(hitCollider))
        {
            EmitHitDiagnostic("Skipped_AlreadyHit", hitCollider, true, true, true, false, false,
                "Inimigo já atingido por este projétil");
            return;
        }

        bool applied = TryApplyEnemyDamage(hitCollider);
        if (!applied)
            return;

        IgnorePhysicsWithCollider(hitCollider);

        bool exhaustAfterThisHit = !stats.infinityBounces && (_currentBounces + 1 >= _maxBounces);
        _currentBounces++;

        ApplyEnemyBounce(hitCollider, collision);

        if (exhaustAfterThisHit)
            TriggerHitAndDestroy();
    }

    private bool TryApplyEnemyDamage(Collider2D hitCollider)
    {
        var networkProjectile = GetComponent<NetworkProjectileController>();
        bool isNetworkSpawned = networkProjectile != null && networkProjectile.IsSpawned;
        bool isServer = !isNetworkSpawned || networkProjectile.IsServer;

        var networkEnemy = hitCollider.GetComponentInParent<NetworkEnemyController>();
        bool enemyDead = networkEnemy != null && networkEnemy.IsDeadOnNetwork;

        if (enemyDead)
        {
            EmitHitDiagnostic("Skipped_DeadEnemy", hitCollider, isNetworkSpawned, isServer, true, true, false,
                "Inimigo já morto na rede");
            return false;
        }

        if (networkEnemy != null)
        {
            var enemyNetObj = networkEnemy.NetworkObject;
            bool enemyOnNetwork = enemyNetObj != null && enemyNetObj.IsSpawned;

            if (isNetworkSpawned && isServer && enemyNetObj != null && !enemyOnNetwork)
            {
                EmitHitDiagnostic("Rejected_EnemyNotSpawned", hitCollider, isNetworkSpawned, isServer, true, false,
                    false,
                    "Inimigo sem NetworkObject.Spawn (provável WaveGenerator local). Use apenas NetworkWaveManager em MP.");
                return false;
            }

            if (isNetworkSpawned && !enemyOnNetwork)
                return false;

            bool applied = isNetworkSpawned
                ? networkProjectile.ServerApplyEnemyHit(networkEnemy, stats.damage)
                : ApplyDamageToHealth(networkEnemy.GetComponent<HealthComponent>());

            string hpInfo = "";
            if (networkEnemy.TryGetComponent<HealthComponent>(out var enemyHealth))
                hpInfo = $" hp={enemyHealth.CurrentHealth:0.##}/{enemyHealth.MaxHealth:0.##}";

            EmitHitDiagnostic("NetworkEnemy", hitCollider, isNetworkSpawned, isServer, true, false, applied,
                (applied ? "Dano via NetworkEnemyController" : "ServerApplyDamage falhou") + hpInfo);

            return applied;
        }

        var health = hitCollider.GetComponentInParent<HealthComponent>();
        if (health == null || health.IsDead)
        {
            EmitHitDiagnostic("NoHealth", hitCollider, isNetworkSpawned, isServer, false, false, false,
                "Sem HealthComponent ou já morto");
            return false;
        }

        bool healthApplied = ApplyDamageToHealth(health);
        EmitHitDiagnostic("HealthComponent", hitCollider, isNetworkSpawned, isServer, false, false, healthApplied,
            "Dano direto no HealthComponent (offline)");
        return healthApplied;
    }

    private void ApplyEnemyBounce(Collider2D hitCollider, Collision2D collision)
    {
        Vector2 normal;
        if (collision != null && collision.contactCount > 0)
            normal = collision.GetContact(0).normal;
        else
            normal = ((Vector2)transform.position - (Vector2)hitCollider.bounds.center).normalized;

        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector2.up;

        Vector2 incoming = _rb.linearVelocity.sqrMagnitude > 0.01f
            ? _rb.linearVelocity
            : _travelDirection * stats.moveSpeed;

        Vector2 reflected = Vector2.Reflect(incoming, normal).normalized;
        SetTravelDirection(reflected, stats.moveSpeed);
    }

    private bool TryRegisterEnemyHit(Collider2D hitCollider)
    {
        var networkEnemy = hitCollider.GetComponentInParent<NetworkEnemyController>();
        Transform root = networkEnemy != null ? networkEnemy.transform : hitCollider.transform;
        int rootId = root.GetInstanceID();
        if (_damagedEnemyRootIds.Contains(rootId))
            return false;

        _damagedEnemyRootIds.Add(rootId);
        return true;
    }

    private bool IsEnemyCollider(Collider2D col)
    {
        if (col == null) return false;

        if (col.GetComponentInParent<NetworkEnemyController>() != null)
            return true;

        if (col.GetComponentInParent<HealthComponent>() != null &&
            col.GetComponentInParent<NetworkPlayerHealth>() == null)
            return true;

        if (_enemyLayer < 0)
            _enemyLayer = LayerMask.NameToLayer("Enemy");

        return _enemyLayer >= 0 && col.gameObject.layer == _enemyLayer;
    }

    private bool CanApplyGameplayHit(out NetworkProjectileController networkProjectile)
    {
        networkProjectile = GetComponent<NetworkProjectileController>();
        bool isNetworkSpawned = networkProjectile != null && networkProjectile.IsSpawned;

        if (isNetworkSpawned && !networkProjectile.IsServer)
            return false;

        return true;
    }

    private void IgnorePhysicsWithCollider(Collider2D other)
    {
        if (other == null) return;
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col != null)
                Physics2D.IgnoreCollision(col, other, true);
        }
    }

    private bool ApplyDamageToHealth(HealthComponent health)
    {
        if (health == null || health.IsDead) return false;
        health.TakeDamage(stats.damage * _damageMultiplier, gameObject);
        return true;
    }

    private void EmitHitDiagnostic(
        string stage,
        Collider2D hitCollider,
        bool isNetworkSpawned,
        bool isServer,
        bool foundNetworkEnemy,
        bool enemyDeadOnNetwork,
        bool damageApplied,
        string detail)
    {
        GameplayDiagnosticHub.Emit(new ProjectileHitDiagnostic(
            stage,
            hitCollider != null ? hitCollider.name : "null",
            hitCollider != null ? hitCollider.gameObject.layer : -1,
            isNetworkSpawned,
            isServer,
            foundNetworkEnemy,
            enemyDeadOnNetwork,
            damageApplied,
            stats != null ? stats.damage * _damageMultiplier : 0f,
            detail));
    }

    private void HandleWallBounce()
    {
        _currentBounces++;
        if (!stats.infinityBounces && _currentBounces >= _maxBounces)
        {
            TriggerHitAndDestroy();
            return;
        }

        if (!_canBeCollected && stats.collectable)
            _canBeCollected = true;
    }

    private static bool IsOtherPlayerProjectile(Collider2D other)
    {
        if (other == null) return false;
        return other.gameObject.layer == LayerMask.NameToLayer("Projectile");
    }

    private void TriggerHitAndDestroy()
    {
        if (_hasHit) return;
        _hasHit = true;

        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;
        float yFlip = _travelDirection.x >= 0f ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0f, yFlip, 0f);

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (_projectileAnimator != null)
            _projectileAnimator.SetTrigger(_hashOnHit);

        var networkProjectile = GetComponent<NetworkProjectileController>();
        if (networkProjectile != null && networkProjectile.IsSpawned)
            networkProjectile.DespawnAfterHit(_hitAnimDuration);
        else
            Destroy(gameObject, _hitAnimDuration);
    }

    public void ActivatePull(Transform target, float speed)
    {
        _currentState = ProjectileState.Seeking;
        _seekTarget = target;
        _seekSpeed = speed;
        _canBeCollected = true;
    }

    public void ActivateReflect(Vector2 newDirection, float speedMultiplier)
    {
        SetTravelDirection(newDirection, stats.moveSpeed * speedMultiplier);
    }

    public void InitializeDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = Vector2.up;

        _travelDirection = direction.normalized;
        _hasTravelDirection = true;

        if (_rb != null)
            SetTravelDirection(_travelDirection, stats.moveSpeed);
    }

    public void AddBonusBounces(int bonusBounces) => _maxBounces += bonusBounces;

    public void SetDamageMultiplier(float multiplier) => _damageMultiplier = Mathf.Max(0f, multiplier);

    private void SetTravelDirection(Vector2 direction, float speed)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude <= Mathf.Epsilon ? Vector2.up : direction.normalized;
        _travelDirection = normalizedDirection;
        _hasTravelDirection = true;
        _rb.linearVelocity = normalizedDirection * speed;

        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
