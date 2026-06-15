/// <summary>
/// MultiplayerHUD.cs
/// Gerencia o HUD in-game que exibe o status de TODOS os jogadores conectados.
/// Cria e remove dinamicamente PlayerHUDCard conforme jogadores entram/saem,
/// ouvindo eventos de NetworkPlayerHealth para atualizar barras de saúde e adrenalina.
/// Também exibe indicadores de onda via GameEvents.OnWaveStatusChanged.
/// SRP: exclusivamente gerencia a exibição do HUD multiplayer em tempo de jogo.
/// </summary>

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerHUD : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform playerCardsContainer;
    [SerializeField] private PlayerHUDCard playerCardPrefab;
    [SerializeField] private MultiplayerConfig config;

    // Mapa de ClientId → Card de UI
    private Dictionary<ulong, PlayerHUDCard> _playerCards = new Dictionary<ulong, PlayerHUDCard>();

    private void OnEnable()
    {
        NetworkPlayerHealth.OnNetworkHealthChanged    += HandleNetworkHealthChanged;
        NetworkPlayerHealth.OnNetworkPlayerDied       += HandleNetworkPlayerDied;
        NetworkPlayerHealth.OnNetworkPlayerRespawned  += HandleNetworkPlayerRespawned;
        NetworkPlayerAdrenaline.OnNetworkAdrenalineChanged += HandleNetworkAdrenalineChanged;

        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.OnClientJoined += HandleClientJoined;
            ConnectionManager.Instance.OnClientLeft   += HandleClientLeft;
        }
    }

    private void OnDisable()
    {
        NetworkPlayerHealth.OnNetworkHealthChanged    -= HandleNetworkHealthChanged;
        NetworkPlayerHealth.OnNetworkPlayerDied       -= HandleNetworkPlayerDied;
        NetworkPlayerHealth.OnNetworkPlayerRespawned  -= HandleNetworkPlayerRespawned;
        NetworkPlayerAdrenaline.OnNetworkAdrenalineChanged -= HandleNetworkAdrenalineChanged;

        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.OnClientJoined -= HandleClientJoined;
            ConnectionManager.Instance.OnClientLeft   -= HandleClientLeft;
        }
    }

    private void Start()
    {
        // Cria cards para jogadores já conectados ao ativar o HUD
        RefreshAllPlayerCards();
    }

    // --- Gerenciamento de Cards ---

    private void RefreshAllPlayerCards()
    {
        if (NetworkManager.Singleton == null) return;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            CreateCardForPlayer(clientId);
    }

    private void CreateCardForPlayer(ulong clientId)
    {
        if (_playerCards.ContainsKey(clientId)) return;
        if (playerCardPrefab == null || playerCardsContainer == null) return;

        PlayerHUDCard card = Instantiate(playerCardPrefab, playerCardsContainer);
        bool isLocal = clientId == (NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue);

        // Encontra a cor do jogador via NetworkPlayerController
        Color playerColor = Color.white;
        var allPlayers = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p.OwnerClientId == clientId)
            {
                playerColor = p.GetPlayerColor();
                break;
            }
        }

        Color cardColor = isLocal
            ? (config != null ? config.localPlayerCardColor : Color.green)
            : (config != null ? config.remotePlayerCardColor : Color.white);

        card.Initialize(clientId, $"Jogador {clientId + 1}", cardColor, playerColor);
        _playerCards[clientId] = card;
        SyncCardHealthFromWorld(clientId, card);
    }

    private void SyncCardHealthFromWorld(ulong clientId, PlayerHUDCard card)
    {
        NetworkPlayerHealth[] players = FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health == null || !health.IsSpawned || health.OwnerClientId != clientId)
                continue;

            card.UpdateHealth(health.CurrentHealth, health.MaxHealth);
            return;
        }
    }

    private void RemoveCardForPlayer(ulong clientId)
    {
        if (!_playerCards.ContainsKey(clientId)) return;
        if (_playerCards[clientId] != null)
            Destroy(_playerCards[clientId].gameObject);
        _playerCards.Remove(clientId);
    }

    // --- Handlers de Eventos ---

    private void HandleClientJoined(ulong clientId)
    {
        CreateCardForPlayer(clientId);
    }

    private void HandleClientLeft(ulong clientId)
    {
        RemoveCardForPlayer(clientId);
    }

    private void HandleNetworkHealthChanged(ulong clientId, float current, float max)
    {
        if (_playerCards.TryGetValue(clientId, out var card))
            card.UpdateHealth(current, max);
    }

    private void HandleNetworkPlayerDied(ulong clientId)
    {
        if (_playerCards.TryGetValue(clientId, out var card))
        {
            Color deadColor = config != null ? config.deadPlayerCardColor : new Color(0.4f, 0.1f, 0.1f, 0.6f);
            card.SetDeadState(deadColor);
        }
    }

    private void HandleNetworkPlayerRespawned(ulong clientId)
    {
        if (_playerCards.TryGetValue(clientId, out var card))
        {
            bool isLocal = clientId == (NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue);
            Color cardColor = isLocal
                ? (config != null ? config.localPlayerCardColor : Color.green)
                : (config != null ? config.remotePlayerCardColor : Color.white);
            card.SetAliveState(cardColor);
        }
    }

    private void HandleNetworkAdrenalineChanged(ulong clientId, float current, float max, bool isFrenzy)
    {
        if (_playerCards.TryGetValue(clientId, out var card))
            card.UpdateAdrenaline(current, max, isFrenzy);
    }
}
