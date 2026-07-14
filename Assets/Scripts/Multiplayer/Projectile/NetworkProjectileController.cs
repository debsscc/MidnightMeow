/// <summary>
/// NetworkProjectileController.cs
/// NetworkBehaviour que gerencia o comportamento de projéteis do jogador na rede.
/// No servidor: ativa a física (Rigidbody2D) e a lógica de colisão do Projectile existente.
///   Ao detectar colisão com inimigo, aplica dano via ServerRpc e despawna se necessário.
/// Nos clientes: desativa física e lógica de colisão; apenas recebe posição via NetworkTransform.
/// Projéteis coletáveis são despawnados via ServerRpc quando um jogador os coleta.
/// SRP: controla exclusivamente o ciclo de vida de rede do projétil do jogador.
/// </summary>

using Unity.Netcode;
using UnityEngine;
using System;

[RequireComponent(typeof(Projectile), typeof(Rigidbody2D))]
public class NetworkProjectileController : NetworkBehaviour
{
    public readonly struct ProjectileNetworkSnapshot
    {
        public readonly string Stage;
        public readonly ulong NetworkObjectId;
        public readonly ulong OwnerClientId;
        public readonly bool IsServer;
        public readonly Vector3 Position;
        public readonly Vector3 RotationEuler;
        public readonly Vector2 Direction;
        public readonly float DamageMultiplier;
        public readonly int BonusBounces;
        public readonly bool RigidbodySimulated;
        public readonly bool ProjectileEnabled;
        public readonly bool ColliderEnabled;

        public ProjectileNetworkSnapshot(
            string stage,
            ulong networkObjectId,
            ulong ownerClientId,
            bool isServer,
            Vector3 position,
            Vector3 rotationEuler,
            Vector2 direction,
            float damageMultiplier,
            int bonusBounces,
            bool rigidbodySimulated,
            bool projectileEnabled,
            bool colliderEnabled)
        {
            Stage = stage;
            NetworkObjectId = networkObjectId;
            OwnerClientId = ownerClientId;
            IsServer = isServer;
            Position = position;
            RotationEuler = rotationEuler;
            Direction = direction;
            DamageMultiplier = damageMultiplier;
            BonusBounces = bonusBounces;
            RigidbodySimulated = rigidbodySimulated;
            ProjectileEnabled = projectileEnabled;
            ColliderEnabled = colliderEnabled;
        }
    }

    public static event Action<ProjectileNetworkSnapshot> OnProjectileNetworkSampled;

    private Projectile _projectile;
    private Rigidbody2D _rb;
    private Collider2D[] _colliders;

    // Dados de inicialização (preenchidos pelo NetworkProjectileSpawner via ServerRpc)
    private Vector2 _direction;
    private float _damageMultiplier = 1f;
    private int _bonusBounces = 0;
    private ulong _ownerClientId;

    private void Awake()
    {
        _projectile = GetComponent<Projectile>();
        _rb = GetComponent<Rigidbody2D>();
        _colliders = GetComponents<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            _rb.simulated = false;
            _projectile.enabled = false;
            SetAllCollidersEnabled(false);
        }

        EmitProjectileNetworkSample("OnNetworkSpawn");
    }

    /// <summary>
    /// Inicializa o projétil no servidor com direção, multiplicadores e identificação do dono.
    /// Chamado apenas no host/servidor pelo NetworkProjectileSpawner após NetworkObject.Spawn (não usar ServerRpc).
    /// </summary>
    public void ServerApplySpawnData(Vector2 direction, float damageMultiplier, int bonusBounces, ulong ownerClientId)
    {
        ServerApplySpawnData(direction, damageMultiplier, bonusBounces, ownerClientId, splash: null);
    }

    public void ServerApplySpawnData(
        Vector2 direction,
        float damageMultiplier,
        int bonusBounces,
        ulong ownerClientId,
        SplashSpawnConfig? splash)
    {
        if (!IsServer) return;

        _direction = direction;
        _damageMultiplier = damageMultiplier;
        _bonusBounces = bonusBounces;
        _ownerClientId = ownerClientId;

        _projectile.InitializeDirection(direction);
        _projectile.SetDamageMultiplier(damageMultiplier);
        if (bonusBounces > 0) _projectile.AddBonusBounces(bonusBounces);

        if (splash.HasValue && splash.Value.Enabled)
        {
            _projectile.ConfigureSplashOnHit(
                splash.Value.Prefab,
                splash.Value.Count,
                splash.Value.Range,
                splash.Value.DamagePercentage,
                splash.Value.PrioritizeDifferentEnemies,
                splash.Value.EnemyLayers);
        }

        IgnoreOwnerPlayerColliders(ownerClientId);

        _rb.simulated = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _projectile.enabled = true;
        _projectile.ConfigureCombatColliders();
        SetAllCollidersEnabled(true);

        EmitProjectileNetworkSample("ServerApplySpawnData");
    }

    /// <summary>
    /// Inicializa um respingo no servidor (teleguiado se houver alvo; senão voo reto).
    /// </summary>
    public void ServerApplySplashSeekerData(
        ulong targetNetworkObjectId,
        float damageMultiplier,
        ulong ownerClientId,
        Vector2 fallbackDirection)
    {
        if (!IsServer) return;

        _damageMultiplier = damageMultiplier;
        _ownerClientId = ownerClientId;

        Transform target = null;
        if (targetNetworkObjectId != 0 &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetNetObj) &&
            targetNetObj != null)
        {
            target = targetNetObj.transform;
        }

        Vector2 dir = fallbackDirection.sqrMagnitude > 0.0001f ? fallbackDirection.normalized : Vector2.up;
        if (target != null)
        {
            Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position);
            if (toTarget.sqrMagnitude > 0.0001f)
                dir = toTarget.normalized;
        }

        _direction = dir;
        _projectile.InitializeDirection(dir);
        _projectile.ConfigureAsSplashSeeker(target, damageMultiplier, dir);
        IgnoreOwnerPlayerColliders(ownerClientId);

        _rb.simulated = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _projectile.enabled = true;
        _projectile.ConfigureCombatColliders();
        SetAllCollidersEnabled(true);

        EmitProjectileNetworkSample("ServerApplySplashSeekerData");
    }

    public void ServerSpawnSplashProjectiles(
        Vector3 origin,
        Transform primaryHitRoot,
        int splashCount,
        float splashRange,
        float splashDamagePercentage,
        bool prioritizeDifferentEnemies,
        LayerMask enemyLayers,
        GameObject splashPrefab,
        Vector2 fallbackDirection)
    {
        if (!IsServer || splashPrefab == null || splashCount <= 0)
            return;

        var targets = new System.Collections.Generic.List<Transform>(splashCount);
        ProjectileSplashUtility.CollectSplashTargets(
            origin,
            splashRange,
            splashCount,
            prioritizeDifferentEnemies,
            enemyLayers,
            primaryHitRoot,
            targets);

        Vector2 safeFallback = fallbackDirection.sqrMagnitude > 0.0001f
            ? fallbackDirection.normalized
            : (_projectile != null && _projectile.TravelDirection.sqrMagnitude > 0.0001f
                ? _projectile.TravelDirection.normalized
                : Vector2.up);

        float splashDamageMul = _damageMultiplier * Mathf.Max(0f, splashDamagePercentage);
        for (int i = 0; i < splashCount; i++)
        {
            Transform target = i < targets.Count ? targets[i] : null;
            ulong targetNetworkObjectId = 0;
            Vector2 dir = safeFallback;

            if (target != null)
            {
                var targetNetObj = target.GetComponent<NetworkObject>() ?? target.GetComponentInParent<NetworkObject>();
                if (targetNetObj != null && targetNetObj.IsSpawned)
                {
                    targetNetworkObjectId = targetNetObj.NetworkObjectId;
                    Vector2 toTarget = ((Vector2)target.position - (Vector2)origin);
                    if (toTarget.sqrMagnitude > 0.0001f)
                        dir = toTarget.normalized;
                }
                else
                {
                    target = null;
                }
            }

            Quaternion rotation = ProjectileAimUtility.RotationFromDirection(dir);
            GameObject splashObj = Instantiate(splashPrefab, origin, rotation);
            var netObj = splashObj.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("[NetworkProjectileController] Splash prefab sem NetworkObject.");
                Destroy(splashObj);
                continue;
            }

            netObj.Spawn(true);

            var splashController = splashObj.GetComponent<NetworkProjectileController>();
            if (splashController != null)
            {
                splashController.ServerApplySplashSeekerData(
                    targetNetworkObjectId,
                    splashDamageMul,
                    _ownerClientId,
                    safeFallback);
            }
        }
    }

    public readonly struct SplashSpawnConfig
    {
        public readonly bool Enabled;
        public readonly int Count;
        public readonly float Range;
        public readonly float DamagePercentage;
        public readonly bool PrioritizeDifferentEnemies;
        public readonly LayerMask EnemyLayers;
        public readonly GameObject Prefab;

        public SplashSpawnConfig(
            bool enabled,
            int count,
            float range,
            float damagePercentage,
            bool prioritizeDifferentEnemies,
            LayerMask enemyLayers,
            GameObject prefab)
        {
            Enabled = enabled;
            Count = count;
            Range = range;
            DamagePercentage = damagePercentage;
            PrioritizeDifferentEnemies = prioritizeDifferentEnemies;
            EnemyLayers = enemyLayers;
            Prefab = prefab;
        }
    }

    private void IgnoreOwnerPlayerColliders(ulong ownerClientId)
    {
        if (_projectile == null || NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var client))
            return;

        if (client.PlayerObject != null)
            _projectile.IgnoreOwnerColliders(client.PlayerObject.gameObject);
    }

    private void SetAllCollidersEnabled(bool enabled)
    {
        if (_colliders == null) return;
        foreach (var col in _colliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }

    private int CountEnabledColliders()
    {
        if (_colliders == null) return 0;
        int count = 0;
        foreach (var col in _colliders)
        {
            if (col != null && col.enabled)
                count++;
        }
        return count;
    }

    private void EmitProjectileNetworkSample(string stage)
    {
        var snapshot = new ProjectileNetworkSnapshot(
            stage,
            NetworkObject != null ? NetworkObject.NetworkObjectId : 0,
            _ownerClientId,
            IsServer,
            transform.position,
            transform.eulerAngles,
            _direction,
            _damageMultiplier,
            _bonusBounces,
            _rb != null && _rb.simulated,
            _projectile != null && _projectile.enabled,
            CountEnabledColliders() > 0
        );

        OnProjectileNetworkSampled?.Invoke(snapshot);

        GameplayDiagnosticHub.Emit(new ProjectileNetworkDiagnostic(
            stage,
            snapshot.NetworkObjectId,
            snapshot.OwnerClientId,
            snapshot.IsServer,
            snapshot.RigidbodySimulated,
            snapshot.ProjectileEnabled,
            CountEnabledColliders(),
            _colliders != null ? _colliders.Length : 0));
    }

    /// <summary>
    /// Chamado pelo Projectile (server) quando colide com um inimigo.
    /// Aplica dano via NetworkEnemyController.TakeDamageRpc.
    /// Este método deve ser conectado ao callback de dano do Projectile na cena de rede.
    /// </summary>
    /// <returns>True se o dano foi aplicado (inimigo vivo no servidor).</returns>
    public bool ServerApplyEnemyHit(NetworkEnemyController enemy, float baseDamage)
    {
        if (!IsServer || enemy == null || enemy.IsDeadOnNetwork) return false;
        return enemy.ServerApplyDamage(baseDamage * _damageMultiplier, _ownerClientId, DamageType.Ranged);
    }

    /// <summary>
    /// Solicita ao servidor a coleta deste projétil como munição.
    /// Chamado pelo jogador ao entrar em contato com o projétil coletável.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void CollectAmmoRpc(ulong collectorClientId)
    {
        if (!IsServer) return;
        GrantAmmoClientRpc(collectorClientId);
        DespawnProjectile();
    }

    [ClientRpc]
    private void GrantAmmoClientRpc(ulong targetClientId)
    {
        // Apenas o cliente coletor recebe a munição
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            GameEvents.InvokeAmmoCollected();
        }
    }

    /// <summary>
    /// Despawna o projétil da rede (servidor). Chamado após colisão ou coleta.
    /// </summary>
    public void DespawnProjectile()
    {
        if (!IsServer) return;
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    public void DespawnAfterHit(float delay)
    {
        if (!IsServer) return;
        Invoke(nameof(DespawnProjectile), Mathf.Max(0f, delay));
    }

    /// <summary>
    /// Server: replicate splash/vanish to all clients, then despawn after the anim window.
    /// </summary>
    public void NotifyHitAndDespawn(float delay, Vector2 impactDirection)
    {
        if (!IsServer) return;
        PlayHitPresentationClientRpc(impactDirection);
        DespawnAfterHit(delay);
    }

    public void ScheduleVanishPresentation(float hitClipLength)
    {
        CancelInvoke(nameof(PlayVanishPresentationInvoked));
        Invoke(nameof(PlayVanishPresentationInvoked), Mathf.Max(0.05f, hitClipLength));
    }

    private void PlayVanishPresentationInvoked()
    {
        if (_projectile != null)
            _projectile.PlayVanishPresentation();
        else if (TryGetComponent(out Projectile projectile))
            projectile.PlayVanishPresentation();
        else if (TryGetComponent(out Animator animator))
            animator.Play("Vanish", 0, 0f);
    }

    [ClientRpc]
    private void PlayHitPresentationClientRpc(Vector2 impactDirection)
    {
        // Host already ran PlayHitPresentation in TriggerHitAndDestroy; clients need this.
        if (IsServer)
            return;

        if (_projectile != null)
            _projectile.PlayHitPresentation(impactDirection);
        else if (TryGetComponent(out Projectile projectile))
            projectile.PlayHitPresentation(impactDirection);
    }
}
