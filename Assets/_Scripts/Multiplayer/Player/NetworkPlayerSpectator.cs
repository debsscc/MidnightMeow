/// <summary>
/// NetworkPlayerSpectator.cs
/// Gerencia o modo espectador do jogador ao ser eliminado no multiplayer.
/// Quando ativado, assume controle da câmera para seguir outros jogadores vivos,
/// permitindo troca manual de alvo via input. Expõe método de respawn atômico
/// para quando o sistema de respawn for implementado pelo servidor.
/// SRP: apenas gerencia câmera e input de espectador, não lógica de saúde.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSpectator : NetworkBehaviour
{
    [SerializeField] private MultiplayerConfig config;
    [SerializeField] private Camera spectatorCamera;

    private bool _isSpectating = false;
    private List<Transform> _alivePlayers = new List<Transform>();
    private int _currentTargetIndex = 0;
    private float _autoSwitchTimer = 0f;
    private Coroutine _autoSwitchCoroutine;

    private PlayerInputHandler _inputHandler;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    public override void OnNetworkSpawn()
    {
        // Apenas o owner deste jogador usa o modo espectador
        if (!IsOwner) return;

        NetworkPlayerHealth.OnNetworkPlayerDied += HandleAnyPlayerDied;
        NetworkPlayerHealth.OnNetworkPlayerRespawned += HandleAnyPlayerRespawned;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        NetworkPlayerHealth.OnNetworkPlayerDied -= HandleAnyPlayerDied;
        NetworkPlayerHealth.OnNetworkPlayerRespawned -= HandleAnyPlayerRespawned;
    }

    /// <summary>
    /// Ativa o modo espectador para o jogador local após a morte.
    /// </summary>
    public void EnterSpectatorMode()
    {
        if (!IsOwner || _isSpectating) return;

        _isSpectating = true;
        RefreshAlivePlayers();

        if (spectatorCamera != null)
            spectatorCamera.gameObject.SetActive(true);

        if (_alivePlayers.Count > 0)
            FocusOnPlayer(_currentTargetIndex);

        _autoSwitchCoroutine = StartCoroutine(AutoSwitchRoutine());
        Debug.Log("[NetworkPlayerSpectator] Modo espectador ativado.");
    }

    /// <summary>
    /// Sai do modo espectador (chamado ao reviver - respawn atômico).
    /// Restaura câmera original e desativa lógica de espectador.
    /// </summary>
    public void ExitSpectatorMode()
    {
        if (!IsOwner || !_isSpectating) return;

        _isSpectating = false;

        if (_autoSwitchCoroutine != null)
        {
            StopCoroutine(_autoSwitchCoroutine);
            _autoSwitchCoroutine = null;
        }

        if (spectatorCamera != null)
            spectatorCamera.gameObject.SetActive(false);

        Debug.Log("[NetworkPlayerSpectator] Modo espectador desativado.");
    }

    /// <summary>
    /// Troca para o próximo jogador vivo na lista de espectador.
    /// Chamado por input ou pelo auto-switch timer.
    /// </summary>
    public void SpectateNextPlayer()
    {
        if (!_isSpectating || _alivePlayers.Count == 0) return;
        _currentTargetIndex = (_currentTargetIndex + 1) % _alivePlayers.Count;
        FocusOnPlayer(_currentTargetIndex);
    }

    public void SpectatePreviousPlayer()
    {
        if (!_isSpectating || _alivePlayers.Count == 0) return;
        _currentTargetIndex = (_currentTargetIndex - 1 + _alivePlayers.Count) % _alivePlayers.Count;
        FocusOnPlayer(_currentTargetIndex);
    }

    private void Update()
    {
        if (!IsOwner || !_isSpectating) return;

        // Suaviza o movimento da câmera em direção ao alvo atual
        if (_alivePlayers.Count > 0 && spectatorCamera != null)
        {
            Transform target = _alivePlayers[_currentTargetIndex];
            if (target == null)
            {
                RefreshAlivePlayers();
                return;
            }

            float speed = config != null ? config.spectatorCameraLerpSpeed : 5f;
            Vector3 targetPos = new Vector3(target.position.x, target.position.y, spectatorCamera.transform.position.z);
            spectatorCamera.transform.position = Vector3.Lerp(
                spectatorCamera.transform.position,
                targetPos,
                speed * Time.deltaTime
            );
        }
    }

    private void FocusOnPlayer(int index)
    {
        if (index < 0 || index >= _alivePlayers.Count) return;
        Transform target = _alivePlayers[index];
        if (target == null) return;

        if (spectatorCamera != null)
        {
            Vector3 pos = new Vector3(target.position.x, target.position.y, spectatorCamera.transform.position.z);
            spectatorCamera.transform.position = pos;
        }

        Debug.Log($"[NetworkPlayerSpectator] Espectando: {target.name}");
    }

    private void RefreshAlivePlayers()
    {
        _alivePlayers.Clear();
        var allPlayers = FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (!player.IsDead && player.OwnerClientId != OwnerClientId)
                _alivePlayers.Add(player.transform);
        }
        _currentTargetIndex = 0;
    }

    private void HandleAnyPlayerDied(ulong clientId)
    {
        if (_isSpectating) RefreshAlivePlayers();
    }

    private void HandleAnyPlayerRespawned(ulong clientId)
    {
        if (_isSpectating) RefreshAlivePlayers();
    }

    private IEnumerator AutoSwitchRoutine()
    {
        float interval = config != null ? config.spectatorAutoSwitchInterval : 8f;
        while (_isSpectating)
        {
            yield return new WaitForSeconds(interval);
            if (_alivePlayers.Count > 1)
                SpectateNextPlayer();
        }
    }
}
