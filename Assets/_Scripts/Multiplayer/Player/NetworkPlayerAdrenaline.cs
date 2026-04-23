/// <summary>
/// NetworkPlayerAdrenaline.cs
/// NetworkBehaviour que sincroniza o estado de adrenalina e frenesi do jogador pela rede.
/// Envolve o PlayerAdrenaline existente: no owner, o PlayerAdrenaline roda normalmente;
/// o NetworkPlayerAdrenaline lê os valores e os replica via NetworkVariable.
/// Nos outros clientes, os valores são usados para exibir barras de frenesi no HUD.
/// SRP: exclusivamente sincronização do estado de adrenalina/frenesi pela rede.
/// </summary>

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerAdrenaline))]
public class NetworkPlayerAdrenaline : NetworkBehaviour
{
    private PlayerAdrenaline _adrenaline;

    private NetworkVariable<float> _networkCurrentAdrenaline = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<float> _networkMaxAdrenaline = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<bool> _networkIsFrenzyActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public float NetworkCurrentAdrenaline => _networkCurrentAdrenaline.Value;
    public float NetworkMaxAdrenaline => _networkMaxAdrenaline.Value;
    public bool NetworkIsFrenzyActive => _networkIsFrenzyActive.Value;

    // Evento disparado em todos os clientes quando adrenalina de qualquer jogador muda
    public static event System.Action<ulong, float, float, bool> OnNetworkAdrenalineChanged;

    private void Awake()
    {
        _adrenaline = GetComponent<PlayerAdrenaline>();
    }

    public override void OnNetworkSpawn()
    {
        _networkCurrentAdrenaline.OnValueChanged += HandleNetworkAdrenalineChanged;
        _networkIsFrenzyActive.OnValueChanged += HandleNetworkFrenzyChanged;
    }

    public override void OnNetworkDespawn()
    {
        _networkCurrentAdrenaline.OnValueChanged -= HandleNetworkAdrenalineChanged;
        _networkIsFrenzyActive.OnValueChanged -= HandleNetworkFrenzyChanged;
    }

    private void Update()
    {
        // Apenas o owner atualiza as NetworkVariables com seus valores locais
        if (!IsOwner || _adrenaline == null) return;

        // Atualiza a rede somente quando o valor muda de forma significativa
        float current = _adrenaline.CurrentAdrenaline;
        bool frenzy = _adrenaline.IsFrenzyActive;

        if (Mathf.Abs(_networkCurrentAdrenaline.Value - current) > 0.5f)
            _networkCurrentAdrenaline.Value = current;

        if (_networkIsFrenzyActive.Value != frenzy)
            _networkIsFrenzyActive.Value = frenzy;
    }

    private void HandleNetworkAdrenalineChanged(float oldValue, float newValue)
    {
        // No owner, o GameEvents já é disparado pelo PlayerAdrenaline local
        if (IsOwner) return;

        OnNetworkAdrenalineChanged?.Invoke(
            OwnerClientId,
            newValue,
            _networkMaxAdrenaline.Value,
            _networkIsFrenzyActive.Value
        );
    }

    private void HandleNetworkFrenzyChanged(bool oldValue, bool newValue)
    {
        OnNetworkAdrenalineChanged?.Invoke(
            OwnerClientId,
            _networkCurrentAdrenaline.Value,
            _networkMaxAdrenaline.Value,
            newValue
        );
    }
}
