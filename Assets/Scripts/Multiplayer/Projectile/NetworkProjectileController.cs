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
        if (!IsServer) return;

        _direction = direction;
        _damageMultiplier = damageMultiplier;
        _bonusBounces = bonusBounces;
        _ownerClientId = ownerClientId;

        // Inicializa o Projectile existente no servidor
        _projectile.InitializeDirection(direction);
        _projectile.SetDamageMultiplier(damageMultiplier);
        if (bonusBounces > 0) _projectile.AddBonusBounces(bonusBounces);
        IgnoreOwnerPlayerColliders(ownerClientId);

        // Ativa física no servidor
        _rb.simulated = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _projectile.enabled = true;
        _projectile.ConfigureCombatColliders();
        SetAllCollidersEnabled(true);

        EmitProjectileNetworkSample("ServerApplySpawnData");
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
