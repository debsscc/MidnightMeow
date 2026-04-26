/// <summary>
/// MultiplayerCombatIntegrityLogger.cs
/// Logger de integridade para combate multiplayer (direção de disparo e munição).
/// Assina eventos de PlayerShooting e NetworkProjectileSpawner para rastrear o
/// fluxo ponta-a-ponta: cliente owner -> validação no servidor -> sync de munição.
/// SRP: exclusivamente diagnóstico em tempo de execução, sem alterar gameplay.
/// </summary>
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerShooting), typeof(PlayerAmmo))]
public class MultiplayerCombatIntegrityLogger : MonoBehaviour
{
    [Header("Ativação")]
    [SerializeField] private bool ativo = true;
    [SerializeField] private string prefixo = "[MP-INTEGRITY]";

    [Header("Categorias")]
    [SerializeField] private bool logDirecaoTiro = true;
    [SerializeField] private bool logMiraDetalhada = true;
    [SerializeField] private bool logSpawnServidor = true;
    [SerializeField] private bool logSincronizacaoMunicao = true;
    [SerializeField] private bool logAvisosIntegridade = true;

    private PlayerShooting _shooting;
    private PlayerAmmo _ammo;
    private PlayerAim _aim;
    private NetworkProjectileSpawner _networkSpawner;

    private void Awake()
    {
        _shooting = GetComponent<PlayerShooting>();
        _ammo = GetComponent<PlayerAmmo>();
        _aim = GetComponent<PlayerAim>();
        _networkSpawner = GetComponent<NetworkProjectileSpawner>();
    }

    private void OnEnable()
    {
        if (_shooting != null)
        {
            _shooting.OnFireDirectionComputed += OnFireDirectionComputed;
            _shooting.OnProjectileInstantiated += OnProjectileInstantiated;
            _shooting.OnOutOfAmmo += OnOutOfAmmo;
        }

        if (_networkSpawner != null)
        {
            _networkSpawner.OnOwnerShotPrepared += OnOwnerShotPrepared;
            _networkSpawner.OnServerShotValidated += OnServerShotValidated;
            _networkSpawner.OnAmmoSyncSentToOwner += OnAmmoSyncSentToOwner;
        }
    }

    private void OnDisable()
    {
        if (_shooting != null)
        {
            _shooting.OnFireDirectionComputed -= OnFireDirectionComputed;
            _shooting.OnProjectileInstantiated -= OnProjectileInstantiated;
            _shooting.OnOutOfAmmo -= OnOutOfAmmo;
        }

        if (_networkSpawner != null)
        {
            _networkSpawner.OnOwnerShotPrepared -= OnOwnerShotPrepared;
            _networkSpawner.OnServerShotValidated -= OnServerShotValidated;
            _networkSpawner.OnAmmoSyncSentToOwner -= OnAmmoSyncSentToOwner;
        }
    }

    private void OnFireDirectionComputed(Vector2 direction, bool usedFirePointDirection, int ammoAtShotStart)
    {
        if (!ativo || !logDirecaoTiro) return;
        Log($"{ContextoRede()} FireDirection={direction} | source={(usedFirePointDirection ? "firePoint" : "mouse")} | ammoStart={ammoAtShotStart}");

        if (!logAvisosIntegridade) return;
        float dotUp = Vector2.Dot(direction.normalized, Vector2.up);
        if (dotUp > 0.995f)
        {
            Warn($"{ContextoRede()} Direção praticamente Vector2.up. Verificar se mira local (PlayerAim) está atualizando o firePoint corretamente.");
            if (logMiraDetalhada && _aim != null && _aim.TryGetDebugSnapshot(out PlayerAim.AimDebugSnapshot aimSnapshot))
            {
                Log($"{ContextoRede()} AimSnapshot mouseScreen={aimSnapshot.MouseScreenPosition} mouseWorld={aimSnapshot.MouseWorldPosition} lookDir={aimSnapshot.LookDirection} camOrtho={aimSnapshot.CameraIsOrthographic} usedRayPlane={aimSnapshot.UsedRayPlane} rayHitPlane={aimSnapshot.RayHitPlane}");
            }
        }
    }

    private void OnProjectileInstantiated(GameObject projectile, Vector2 direction)
    {
        if (!ativo || !logDirecaoTiro) return;
        string projectileName = projectile != null ? projectile.name : "null";
        Log($"{ContextoRede()} LocalProjectileInstantiated name={projectileName} dir={direction}");
    }

    private void OnOutOfAmmo()
    {
        if (!ativo || !logSincronizacaoMunicao) return;
        Log($"{ContextoRede()} OutOfAmmo localCurrentAmmo={SafeCurrentAmmo()}");
    }

    private void OnOwnerShotPrepared(NetworkProjectileSpawner.OwnerShotSnapshot snapshot)
    {
        if (!ativo || !logDirecaoTiro) return;
        Log($"{ContextoRede()} OwnerShotPrepared pos={snapshot.Position} dir={snapshot.Direction} dmgMul={snapshot.DamageMultiplier:0.###} bonusBounces={snapshot.BonusBounces}");
    }

    private void OnServerShotValidated(NetworkProjectileSpawner.ServerShotSnapshot snapshot)
    {
        if (!ativo || !logSpawnServidor) return;

        string status = snapshot.Accepted ? "ACCEPTED" : "REJECTED";
        Log($"{ContextoRede()} ServerShot {status} owner={snapshot.OwnerClientId} ammo={snapshot.AmmoBefore}->{snapshot.AmmoAfter} dir={snapshot.Direction} reason=\"{snapshot.Reason}\"");

        if (logAvisosIntegridade && snapshot.Accepted && snapshot.AmmoAfter > snapshot.AmmoBefore)
        {
            Warn($"{ContextoRede()} AmmoAfter > AmmoBefore em shot aceito. Possível inconsistência de configuração de munição.");
        }
    }

    private void OnAmmoSyncSentToOwner(ulong ownerClientId, int syncedAmmo)
    {
        if (!ativo || !logSincronizacaoMunicao) return;
        Log($"{ContextoRede()} AmmoSyncSent targetOwner={ownerClientId} syncedAmmo={syncedAmmo} localAmmoNow={SafeCurrentAmmo()}");
    }

    private string ContextoRede()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
            return "[offline]";

        string mode = nm.IsHost ? "host" : (nm.IsServer ? "server" : (nm.IsClient ? "client" : "unknown"));
        ulong localId = nm.LocalClientId;
        return $"[{mode} local={localId}]";
    }

    private int SafeCurrentAmmo()
    {
        return _ammo != null ? _ammo.CurrentAmmo : -1;
    }

    private void Log(string message)
    {
        Debug.Log($"{prefixo} {message}");
    }

    private void Warn(string message)
    {
        Debug.LogWarning($"{prefixo} {message}");
    }
}
