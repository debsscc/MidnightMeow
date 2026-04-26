/// <summary>
/// NetworkCienciaController.cs
/// NetworkBehaviour server-autoritativo para os drops de ciência (moeda do jogo).
/// Substitui o comportamento de coleta do Ciencia.cs no contexto multiplayer:
/// - O objeto Ciencia é spawned como NetworkObject pelo NetworkWaveManager ao inimigo morrer.
/// - Quando qualquer jogador entra no trigger, envia CollectServerRpc ao servidor.
/// - O servidor valida, despawna o objeto e notifica todos (ou apenas o coletor) via ClientRpc.
/// - Se sharedSciencePool=true, todos os jogadores recebem a ciência; caso contrário, só o coletor.
/// SRP: apenas gerencia o ciclo de vida de rede de drops de ciência.
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Ciencia))]
public class NetworkCienciaController : NetworkBehaviour
{
    [SerializeField] private MultiplayerConfig config;

    private Ciencia _ciencia;
    private bool _collected = false;

    private void Awake()
    {
        _ciencia = GetComponent<Ciencia>();
    }

    public override void OnNetworkSpawn()
    {
        // Desabilita o comportamento de coleta local do Ciencia.cs em todos os contextos;
        // a coleta agora é gerenciada por este script via ServerRpc.
        _ciencia.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Qualquer cliente pode detectar a colisão, mas apenas solicita ao servidor
        if (_collected) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        // Identifica qual cliente local colidiu
        var networkPlayerCtrl = other.GetComponent<NetworkPlayerController>();
        if (networkPlayerCtrl == null || !networkPlayerCtrl.IsOwner) return;

        ulong collectorId = networkPlayerCtrl.OwnerClientId;
        RequestCollectRpc(collectorId);
    }

    [Rpc(SendTo.Server)]
    private void RequestCollectRpc(ulong collectorClientId)
    {
        if (!IsServer || _collected) return;
        _collected = true;

        int amount = _ciencia.GetValue();
        bool shared = config != null ? config.sharedSciencePool : true;

        if (shared)
        {
            // Notifica TODOS os clientes com a ciência
            GrantScienceToAllClientRpc(amount);
        }
        else
        {
            // Notifica apenas o coletor
            GrantScienceToCollectorClientRpc(amount, collectorClientId);
        }

        // Despawna o objeto de rede
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    [ClientRpc]
    private void GrantScienceToAllClientRpc(int amount)
    {
        GameEvents.InvokeCienciaCollected(amount);
    }

    [ClientRpc]
    private void GrantScienceToCollectorClientRpc(int amount, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
            GameEvents.InvokeCienciaCollected(amount);
    }
}
