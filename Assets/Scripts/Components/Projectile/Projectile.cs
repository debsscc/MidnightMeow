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
    private SpriteRenderer _spriteRenderer;
    private int _currentBounces = 0;
    private int _maxBounces;
    private bool _canBeCollected = false;

    [Header("Animation")]
    [SerializeField] private Animator _projectileAnimator;
    [SerializeField] private float _hitAnimDuration = 0.5f;
    [SerializeField] private float _vanishAnimDuration = 0.6f;
    [Tooltip("Added to Atan2 angle. 0 = art points right (+X); -90 = art points up (+Y).")]
    [SerializeField] private float _spriteFacingOffsetDegrees;

    private bool _hasHit = false;
    private readonly HashSet<int> _damagedEnemyRootIds = new HashSet<int>();
    private static readonly int _hashOnHit = Animator.StringToHash("OnHit");
    private const float DefaultHitClipLength = 0.5f;
    private const float DefaultVanishClipLength = 0.6f;

    private static int _enemyLayer = -1;
    private static int _wallLayer = -1;
    private static int _structureLayer = -1;

    private enum ProjectileState { Fired, Seeking }
    private ProjectileState _currentState = ProjectileState.Fired;

    private Transform _seekTarget;
    private float _seekSpeed;
    private Vector2 _travelDirection;
    private bool _hasTravelDirection;
    private Vector2 _spawnPosition;

    private bool _splashOnHit;
    private int _splashCount;
    private float _splashRange;
    private float _splashDamagePercentage;
    private bool _prioritizeDifferentEnemies;
    private GameObject _splashProjectilePrefab;
    private LayerMask _splashEnemyLayers;
    private bool _isSplashSeeker;

    private static Sprite _fallbackCircleSprite;
    private static float _fallbackCircleDiameter = -1f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _maxBounces = stats.maxBounces;
        CombatLayerCollision.Apply();
        CacheLayerIndices();
        if (_rb != null)
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        EnsureVisibleSprite();
    }

    public void ConfigureCombatColliders()
    {
        CombatLayerCollision.Apply();
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col != null)
                col.excludeLayers = 0;
        }
    }

    private static void CacheLayerIndices()
    {
        if (_enemyLayer < 0)
            _enemyLayer = LayerMask.NameToLayer("Enemy");

        if (_wallLayer < 0)
            _wallLayer = LayerMask.NameToLayer("Wall");

        if (_structureLayer < 0)
            _structureLayer = LayerMask.NameToLayer("Structure");
    }

    private void Start()
    {
        _spawnPosition = transform.position;
        Vector2 initialDirection = _hasTravelDirection ? _travelDirection : (Vector2)transform.right;
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
        if (_spriteRenderer != null && _spriteRenderer.sprite == null)
            EnsureVisibleSprite();

        if (ShouldExpireByMaxDistance())
        {
            // Sem impacto: some no ar com Vanish (não Hit).
            TriggerVanishAndDestroy();
            return;
        }

        if (_currentState == ProjectileState.Seeking)
        {
            if (!IsValidSeekTarget(_seekTarget))
            {
                // Sem alvo: continua na direção atual (como o projétil normal).
                _currentState = ProjectileState.Fired;
                _seekTarget = null;
                float speed = stats != null ? stats.moveSpeed : Mathf.Max(0.01f, _seekSpeed);
                SetTravelDirection(
                    _travelDirection.sqrMagnitude > 0.0001f ? _travelDirection : Vector2.up,
                    speed);
            }
            else
            {
                Vector2 direction = (_seekTarget.position - transform.position).normalized;
                _rb.linearVelocity = direction * _seekSpeed;
                _travelDirection = direction;
                ApplyFacingRotation();
            }
        }
    }

    private bool ShouldExpireByMaxDistance()
    {
        if (_hasHit || stats == null || stats.maxDistance <= 0f)
            return false;

        // Pull de munição (Seeking legado) não expira por distância.
        // Respingos (splash) sempre respeitam maxDistance, mesmo teleguiados.
        if (!_isSplashSeeker && _currentState == ProjectileState.Seeking)
            return false;

        return Vector2.Distance(_spawnPosition, transform.position) >= stats.maxDistance;
    }

    private static bool IsValidSeekTarget(Transform target)
    {
        if (target == null)
            return false;

        if (target.TryGetComponent<HealthComponent>(out var health) && health.IsDead)
            return false;

        if (target.TryGetComponent<NetworkEnemyController>(out var networkEnemy) && networkEnemy.IsDeadOnNetwork)
            return false;

        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasHit) return;
        if (collision.collider == null) return;
        if (IsPlayerCollider(collision.collider) || IsOtherPlayerProjectile(collision.collider)) return;

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
        if (_hasHit || other == null || IsOtherPlayerProjectile(other)) return;
        if (IsPlayerCollider(other))
        {
            if (_canBeCollected && stats.collectable)
            {
                GameEvents.InvokeAmmoCollected();
                Destroy(gameObject);
            }
            return;
        }

        if (IsEnemyCollider(other))
            return;

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

        Vector2 impactDirection = ResolveImpactDirection();

        // Passiva Cora (splash): destrói o projétil original e instancia respingos teleguiados.
        if (_splashOnHit && !_isSplashSeeker)
        {
            Transform primaryRoot = ResolveEnemyRootTransform(hitCollider);
            TrySpawnSplashProjectiles(primaryRoot);
            TriggerHitAndDestroy(impactDirection);
            return;
        }

        // Splash seekers e ataque normal: sem ricochete em inimigo — destrói no impacto.
        bool exhaustAfterThisHit = !stats.infinityBounces && (_currentBounces + 1 >= _maxBounces);
        _currentBounces++;

        if (exhaustAfterThisHit || _isSplashSeeker)
        {
            TriggerHitAndDestroy(impactDirection);
            return;
        }

        ApplyEnemyBounce(hitCollider, collision);
    }

    private static Transform ResolveEnemyRootTransform(Collider2D hitCollider)
    {
        var networkEnemy = hitCollider.GetComponentInParent<NetworkEnemyController>();
        if (networkEnemy != null)
            return networkEnemy.transform;

        var health = hitCollider.GetComponentInParent<HealthComponent>();
        return health != null ? health.transform : hitCollider.transform;
    }

    private void TrySpawnSplashProjectiles(Transform primaryHitRoot)
    {
        if (_splashProjectilePrefab == null || _splashCount <= 0)
            return;

        Vector2 fallbackDirection = ResolveImpactDirection();

        var networkProjectile = GetComponent<NetworkProjectileController>();
        if (networkProjectile != null && networkProjectile.IsSpawned)
        {
            if (networkProjectile.IsServer)
            {
                networkProjectile.ServerSpawnSplashProjectiles(
                    transform.position,
                    primaryHitRoot,
                    _splashCount,
                    _splashRange,
                    _splashDamagePercentage,
                    _prioritizeDifferentEnemies,
                    _splashEnemyLayers,
                    _splashProjectilePrefab,
                    fallbackDirection);
            }
            return;
        }

        // Offline / singleplayer — sempre spawna splashCount; sem alvo = segue a direção do impacto.
        var targets = new List<Transform>(_splashCount);
        ProjectileSplashUtility.CollectSplashTargets(
            transform.position,
            _splashRange,
            _splashCount,
            _prioritizeDifferentEnemies,
            _splashEnemyLayers,
            primaryHitRoot,
            targets);

        float splashDamageMul = _damageMultiplier * _splashDamagePercentage;
        for (int i = 0; i < _splashCount; i++)
        {
            Transform target = i < targets.Count ? targets[i] : null;
            Vector2 dir = fallbackDirection;
            if (target != null)
            {
                Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position);
                if (toTarget.sqrMagnitude > 0.0001f)
                    dir = toTarget.normalized;
            }

            Quaternion rotation = ProjectileAimUtility.RotationFromDirection(dir);
            GameObject splashObj = Instantiate(_splashProjectilePrefab, transform.position, rotation);
            if (splashObj.TryGetComponent<Projectile>(out var splash))
            {
                splash.InitializeDirection(dir);
                splash.ConfigureAsSplashSeeker(target, splashDamageMul, dir);
            }
        }
    }

    private Vector2 ResolveImpactDirection()
    {
        if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.01f)
            return _rb.linearVelocity.normalized;

        if (_travelDirection.sqrMagnitude > Mathf.Epsilon)
            return _travelDirection.normalized;

        return Vector2.right;
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
        if (col == null || IsPlayerCollider(col)) return false;

        if (col.GetComponentInParent<NetworkEnemyController>() != null)
            return true;

        if (col.GetComponentInParent<HealthComponent>() != null &&
            col.GetComponentInParent<NetworkPlayerHealth>() == null)
            return true;

        if (_enemyLayer < 0)
            _enemyLayer = LayerMask.NameToLayer("Enemy");

        return _enemyLayer >= 0 && col.gameObject.layer == _enemyLayer;
    }

    private static bool IsPlayerCollider(Collider2D col)
    {
        if (col == null) return false;
        if (col.GetComponentInParent<NetworkPlayerHealth>() != null) return true;
        if (col.GetComponentInParent<NetworkPlayerController>() != null) return true;
        if (col.CompareTag("Player")) return true;

        int playerLayer = LayerMask.NameToLayer("Player");
        return playerLayer >= 0 && col.gameObject.layer == playerLayer;
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
        health.TakeDamage(stats.damage * _damageMultiplier, gameObject, DamageType.Ranged);
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
            TriggerHitAndDestroy(ResolveImpactDirection());
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

    private void TriggerHitAndDestroy() => TriggerHitAndDestroy(ResolveImpactDirection());

    private void TriggerHitAndDestroy(Vector2 impactDirection)
    {
        if (_hasHit) return;
        _hasHit = true;

        PlayHitPresentation(impactDirection);
        float delay = ResolveStateClipLength("Hit", _hitAnimDuration, DefaultHitClipLength);

        var networkProjectile = GetComponent<NetworkProjectileController>();
        if (networkProjectile != null && networkProjectile.IsSpawned)
            networkProjectile.NotifyHitAndDespawn(delay, impactDirection);
        else
            Destroy(gameObject, delay);
    }

    /// <summary>
    /// Expira por <see cref="ProjectileStats.maxDistance"/> sem acertar nada: só Vanish.
    /// </summary>
    private void TriggerVanishAndDestroy()
    {
        if (_hasHit) return;
        _hasHit = true;

        PlayVanishPresentation();
        float delay = ResolveStateClipLength("Vanish", _vanishAnimDuration, DefaultVanishClipLength);

        var networkProjectile = GetComponent<NetworkProjectileController>();
        if (networkProjectile != null && networkProjectile.IsSpawned)
            networkProjectile.NotifyVanishAndDespawn(delay);
        else
            Destroy(gameObject, delay);
    }

    public Vector2 TravelDirection => _travelDirection;

    /// <summary>
    /// Impacto (inimigo/parede esgotada): só animação Hit. Não encadeia Vanish.
    /// Safe on clients even when this component is disabled.
    /// </summary>
    public void PlayHitPresentation() => PlayHitPresentation(_travelDirection);

    public void PlayHitPresentation(Vector2 impactDirection)
    {
        Vector2 dir = impactDirection.sqrMagnitude > Mathf.Epsilon
            ? impactDirection.normalized
            : ResolveImpactDirection();
        _travelDirection = dir;

        FreezeForEndPresentation();
        ApplyImpactFacing();

        if (_projectileAnimator == null)
            _projectileAnimator = GetComponent<Animator>();

        if (_projectileAnimator == null)
            return;

        _projectileAnimator.enabled = true;
        _projectileAnimator.ResetTrigger(_hashOnHit);
        _projectileAnimator.Play("Hit", 0, 0f);
        _projectileAnimator.Update(0f);
    }

    /// <summary>Ignores physics with the shooter so the projectile never hits its owner on spawn.</summary>
    public void IgnoreOwnerColliders(GameObject owner)
    {
        if (owner == null) return;

        var ownerCols = owner.GetComponentsInChildren<Collider2D>(true);
        foreach (var myCol in GetComponents<Collider2D>())
        {
            if (myCol == null) continue;
            for (int i = 0; i < ownerCols.Length; i++)
            {
                if (ownerCols[i] != null)
                    Physics2D.IgnoreCollision(myCol, ownerCols[i], true);
            }
        }
    }

    private void FreezeForEndPresentation()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        var sparkTrail = GetComponent<ProjectileSparkTrail>();
        if (sparkTrail != null)
            sparkTrail.StopTrail();

        foreach (var col in GetComponents<Collider2D>())
        {
            if (col != null)
                col.enabled = false;
        }
    }

    private void ApplyImpactFacing()
    {
        // Hit/Vanish seguem a mesma rotação de voo.
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _spriteRenderer.flipX = false;
            _spriteRenderer.flipY = false;
        }

        ApplyFacingRotation();
    }

    private float ResolveStateClipLength(string stateName, float serializedFallback, float defaultFallback)
    {
        if (_projectileAnimator == null)
            return serializedFallback > 0.05f ? serializedFallback : defaultFallback;

        var info = _projectileAnimator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(stateName) && info.length > 0.05f)
            return info.length;

        return serializedFallback > 0.05f ? serializedFallback : defaultFallback;
    }

    public void PlayVanishPresentation()
    {
        FreezeForEndPresentation();
        ApplyImpactFacing();

        if (_projectileAnimator == null)
            _projectileAnimator = GetComponent<Animator>();
        if (_projectileAnimator == null)
            return;

        _projectileAnimator.enabled = true;
        _projectileAnimator.Play("Vanish", 0, 0f);
        _projectileAnimator.Update(0f);
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

    /// <summary>
    /// Habilita respingos no impacto (passiva Cora). Prefab deve ter NetworkObject em MP.
    /// </summary>
    public void ConfigureSplashOnHit(
        GameObject splashPrefab,
        int splashCount,
        float splashRange,
        float splashDamagePercentage,
        bool prioritizeDifferentEnemies,
        LayerMask enemyLayers)
    {
        _splashOnHit = splashPrefab != null && splashCount > 0;
        _splashProjectilePrefab = splashPrefab;
        _splashCount = splashCount;
        _splashRange = splashRange;
        _splashDamagePercentage = Mathf.Max(0f, splashDamagePercentage);
        _prioritizeDifferentEnemies = prioritizeDifferentEnemies;
        _splashEnemyLayers = enemyLayers.value != 0 ? enemyLayers : (LayerMask)(1 << LayerMask.NameToLayer("Enemy"));
    }

    /// <summary>
    /// Configura este projétil como respingo: teleguia se houver alvo válido;
    /// caso contrário segue na direção informada e expira em parede / maxDistance.
    /// </summary>
    public void ConfigureAsSplashSeeker(Transform target, float damageMultiplier, Vector2 fallbackDirection)
    {
        _isSplashSeeker = true;
        _splashOnHit = false;
        _maxBounces = 1;
        _canBeCollected = false;
        SetDamageMultiplier(damageMultiplier);

        float speed = stats != null ? stats.moveSpeed : 15f;
        Vector2 dir = fallbackDirection.sqrMagnitude > 0.0001f
            ? fallbackDirection.normalized
            : (_travelDirection.sqrMagnitude > 0.0001f ? _travelDirection.normalized : Vector2.up);

        if (IsValidSeekTarget(target))
        {
            _currentState = ProjectileState.Seeking;
            _seekTarget = target;
            _seekSpeed = speed;

            Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position);
            if (toTarget.sqrMagnitude > 0.0001f)
                dir = toTarget.normalized;
        }
        else
        {
            _currentState = ProjectileState.Fired;
            _seekTarget = null;
        }

        if (_rb != null)
            SetTravelDirection(dir, speed);
        else
        {
            _travelDirection = dir;
            _hasTravelDirection = true;
        }
    }

    /// <summary>Compat: respingo com direção atual já inicializada.</summary>
    public void ConfigureAsSplashSeeker(Transform target, float damageMultiplier)
    {
        ConfigureAsSplashSeeker(target, damageMultiplier, _travelDirection);
    }

    private void SetTravelDirection(Vector2 direction, float speed)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude <= Mathf.Epsilon ? Vector2.up : direction.normalized;
        _travelDirection = normalizedDirection;
        _hasTravelDirection = true;
        _rb.linearVelocity = normalizedDirection * speed;
        ApplyFacingRotation();
    }

    private void ApplyFacingRotation()
    {
        float angle = Mathf.Atan2(_travelDirection.y, _travelDirection.x) * Mathf.Rad2Deg
                      + _spriteFacingOffsetDegrees;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void EnsureVisibleSprite()
    {
        if (_spriteRenderer == null)
            return;

        if (_spriteRenderer.sprite != null)
            return;

        float diameter = ResolveVisualDiameter();
        _spriteRenderer.sprite = GetOrCreateFallbackCircleSprite(diameter);
    }

    private float ResolveVisualDiameter()
    {
        if (TryGetComponent(out CircleCollider2D circle) && circle.radius > 0f)
        {
            Vector3 scale = transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            return circle.radius * 2f * maxScale;
        }

        return 0.5f;
    }

    private static Sprite GetOrCreateFallbackCircleSprite(float diameterWorldUnits)
    {
        diameterWorldUnits = Mathf.Max(0.05f, diameterWorldUnits);

        if (_fallbackCircleSprite != null && Mathf.Approximately(_fallbackCircleDiameter, diameterWorldUnits))
            return _fallbackCircleSprite;

        const int textureSize = 64;
        var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (textureSize - 1) * 0.5f;
        float radius = center - 1f;
        float radiusSq = radius * radius;
        var pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                pixels[y * textureSize + x] = dx * dx + dy * dy <= radiusSq
                    ? new Color(0.95f, 0.85f, 0.35f, 1f)
                    : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        float pixelsPerUnit = textureSize / diameterWorldUnits;
        _fallbackCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        _fallbackCircleDiameter = diameterWorldUnits;
        return _fallbackCircleSprite;
    }
}
