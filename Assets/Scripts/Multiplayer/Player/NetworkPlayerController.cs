/// <summary>
/// NetworkPlayerController.cs
/// NetworkBehaviour responsável por inicializar o jogador no contexto multiplayer.
/// Ao spawnar, verifica IsOwner para habilitar/desabilitar componentes de input:
///   - Owner (jogador local): input, tiro, dash, habilidades habilitados.
///     Notifica MultiplayerCameraController para seguir este jogador.
///   - Não-owner (jogador remoto): input desabilitado; posição recebida via rede.
/// CÂMERA: O prefab do jogador NÃO deve conter uma câmera própria.
///   A câmera da cena (MultiplayerCameraRig) segue o jogador local automaticamente.
///   Manter uma câmera no prefab causaria conflito e tela azul (duas câmeras ativas).
/// Replica orientação visual do sprite via NetworkVariable escrita pelo dono (PlayerFacingController).
/// SRP: ownership, cor/nome de rede e orientação visual replicada.
/// </summary>

using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NetworkPlayerController : NetworkBehaviour
{
    public static event System.Action<NetworkPlayerController> OnLocalPlayerSpawned;
    public static event System.Action<ulong> OnLocalPlayerDespawned;

    [Header("Diagnostico")]
    [SerializeField] private bool enableDiagnosticsLogs = true;

    [Header("Camera Binding")]
    [SerializeField] private float cameraBindRetryInterval = 0.25f;
    [SerializeField] private float cameraBindTimeoutSeconds = 30f;

    private Coroutine _cameraBindCoroutine;

    [Header("Componentes locais a desabilitar em clientes remotos")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private PlayerMeleeCombat meleeCombat;
    [SerializeField] private PlayerAim aim;
    [SerializeField] private PlayerDash dash;
    [SerializeField] private PlayerAbilityHandler abilityHandler;
    [SerializeField] private PlayerAdrenaline adrenaline;
    [SerializeField] private PlayerAmmo ammo;

    [Header("Componentes visuais (sempre ativos)")]
    [SerializeField] private PlayerAnimationHandler animationHandler;
    [SerializeField] private Renderer[] playerRenderers;

    [Header("Câmera no Prefab (DEVE FICAR VAZIO)")]
    [Tooltip("ATENÇÃO: Deixe este campo VAZIO. Câmeras no prefab causam conflito com o MultiplayerCameraRig da cena (tela azul). " +
             "A câmera da cena encontra o jogador local automaticamente via MultiplayerCameraController.")]
    [SerializeField] private GameObject playerCamera;

    [Header("Configuração Visual por Jogador")]
    [SerializeField] private Color[] playerColors = new Color[]
    {
        Color.white,
        new Color(0.4f, 0.7f, 1f),
        new Color(1f, 0.5f, 0.2f),
        new Color(0.8f, 0.3f, 0.8f)
    };

    // NetworkVariable para o índice de cor deste jogador (atribuído pelo servidor)
    private NetworkVariable<int> _playerColorIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // NetworkVariable para o nome do jogador (sincronizado)
    private NetworkVariable<Unity.Collections.FixedString64Bytes> _playerName =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>(
            "Jogador",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    private readonly NetworkVariable<bool> _networkFacingFlipX = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private bool _subscribedFacing;

    public string PlayerName => _playerName.Value.ToString();
    public int ColorIndex => _playerColorIndex.Value;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public static void RebindLocalPlayerCameras()
    {
        NetworkPlayerController[] players =
            Object.FindObjectsByType<NetworkPlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsSpawned && players[i].IsOwner)
                players[i].TryBindCameraNow();
        }
    }

    public static bool TryGetFirstAliveTeammateFollowTarget(out Transform followTarget)
    {
        followTarget = null;

        NetworkPlayerHealth[] players =
            Object.FindObjectsByType<NetworkPlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health == null || !health.IsSpawned || !health.CanFight)
                continue;

            NetworkPlayerController controller = health.GetComponent<NetworkPlayerController>();
            if (controller == null)
                continue;

            followTarget = controller.GetCameraFollowTransform();
            return followTarget != null;
        }

        return false;
    }

    /// <summary>Transform estável para follow da câmera (evita jitter do Rigidbody2D).</summary>
    public Transform GetCameraFollowTransform()
    {
        Transform anchor = transform.Find("CameraFollow");
        if (anchor != null)
            return anchor;

        return transform;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsOwner || !IsSpawned)
            return;

        if (GameplaySceneBootstrap.IsGameplayScene(scene.name))
            TryBindCameraNow();
    }

    public override void OnNetworkSpawn()
    {
        if (enableDiagnosticsLogs)
        {
            Debug.Log($"[PLAYER-DIAG][OnNetworkSpawn] obj={name} ownerId={OwnerClientId} localClientId={NetworkManager.LocalClientId} " +
                      $"isOwner={IsOwner} isServer={IsServer} isClient={IsClient} isLocalPlayer={IsLocalPlayer}");
        }

        // Servidor atribui um índice de cor único baseado no ClientId
        if (IsServer)
        {
            _playerColorIndex.Value = (int)(OwnerClientId % (ulong)playerColors.Length);
        }

        _playerColorIndex.OnValueChanged += OnColorIndexChanged;
        ApplyPlayerColor(_playerColorIndex.Value);

        _networkFacingFlipX.OnValueChanged += OnNetworkFacingFlipChanged;
        ApplyFacingVisual(_networkFacingFlipX.Value);
        if (IsOwner && TryGetComponent<PlayerFacingController>(out var facingController))
        {
            facingController.OnFacingChanged += OnOwnerFacingChanged;
            _subscribedFacing = true;
        }

        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }

        // Registra este jogador nos eventos globais de multiplayer
        GameEvents.InvokePlayerJoined(OwnerClientId, IsOwner);
    }

    public override void OnNetworkDespawn()
    {
        if (enableDiagnosticsLogs)
            Debug.Log($"[PLAYER-DIAG][OnNetworkDespawn] obj={name} ownerId={OwnerClientId} isOwner={IsOwner}");

        if (IsOwner)
            OnLocalPlayerDespawned?.Invoke(OwnerClientId);

        _playerColorIndex.OnValueChanged -= OnColorIndexChanged;
        _networkFacingFlipX.OnValueChanged -= OnNetworkFacingFlipChanged;
        if (_subscribedFacing && TryGetComponent<PlayerFacingController>(out var facingController))
        {
            facingController.OnFacingChanged -= OnOwnerFacingChanged;
            _subscribedFacing = false;
        }
        if (_cameraBindCoroutine != null)
        {
            StopCoroutine(_cameraBindCoroutine);
            _cameraBindCoroutine = null;
        }
        GameEvents.InvokePlayerLeft(OwnerClientId);
    }

    private void OnOwnerFacingChanged(bool facingRight)
    {
        _networkFacingFlipX.Value = facingRight;
    }

    private void OnNetworkFacingFlipChanged(bool previous, bool current)
    {
        ApplyFacingVisual(current);
    }

    private void ApplyFacingVisual(bool facingRight)
    {
        if (animationHandler != null)
            animationHandler.ApplyNetworkFacing(facingRight);
    }

    private void SetupLocalPlayer()
    {
        if (enableDiagnosticsLogs)
            Debug.Log($"[PLAYER-DIAG][SetupLocalPlayer] obj={name} ownerId={OwnerClientId} scene={SceneManager.GetActiveScene().name}");

        SetInputComponentsActive(true);
        _playerName.Value = $"Jogador {OwnerClientId + 1}";

        // Garante que qualquer câmera no prefab esteja DESLIGADA
        // A câmera correta é a da cena (MultiplayerCameraRig), não do prefab
        if (playerCamera != null)
        {
            playerCamera.SetActive(false);
            Debug.LogWarning($"[NetworkPlayerController] Campo 'Player Camera' está preenchido no prefab. " +
                             "Câmeras no prefab causam conflito (tela azul). Deixe o campo vazio no Inspector do prefab.");
        }

        if (!TryBindCameraNow() && _cameraBindCoroutine == null)
            _cameraBindCoroutine = StartCoroutine(WaitAndBindCameraRoutine());

        Debug.Log($"[NetworkPlayerController] Jogador local configurado. ClientId={OwnerClientId}");
        if (enableDiagnosticsLogs)
            Debug.Log($"[PLAYER-DIAG][SetupLocalPlayer] disparando OnLocalPlayerSpawned para obj={name}");

        OnLocalPlayerSpawned?.Invoke(this);
    }

    private bool TryBindCameraNow()
    {
        GameplaySceneBootstrap.EnsureCameraRigPresent();

        MultiplayerCameraController cameraController = MultiplayerCameraController.Resolve();
        if (cameraController == null)
            return false;

        Transform followTarget = GetCameraFollowTransform();
        cameraController.SetTarget(followTarget);
        if (aim != null)
            aim.SetAimCamera(cameraController.MainCamera);

        bool bound = cameraController.IsFollowingTarget;
        if (enableDiagnosticsLogs)
        {
            Debug.Log(bound
                ? $"[NetworkPlayerController] Câmera vinculada ao jogador local ClientId={OwnerClientId} alvo={followTarget.name}"
                : $"[NetworkPlayerController] Falha ao vincular câmera ClientId={OwnerClientId} (controller sem follow).");
        }

        return bound;
    }

    private System.Collections.IEnumerator WaitAndBindCameraRoutine()
    {
        float elapsed = 0f;
        while (elapsed < cameraBindTimeoutSeconds && IsSpawned && IsOwner)
        {
            if (TryBindCameraNow())
            {
                _cameraBindCoroutine = null;
                yield break;
            }

            elapsed += cameraBindRetryInterval;
            yield return new WaitForSeconds(cameraBindRetryInterval);
        }

        if (GameplaySceneBootstrap.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            Debug.LogWarning("[NetworkPlayerController] MultiplayerCameraController não encontrado após timeout. " +
                             "Verifique GameplayPrefabCatalog e MultiplayerCameraRig.");
        }

        _cameraBindCoroutine = null;
    }

    private void SetupRemotePlayer()
    {
        SetInputComponentsActive(false);

        // O servidor precisa manter PlayerAmmo ativo também para jogadores remotos,
        // pois a validação autoritativa de disparo/munição acontece no host.
        if (IsServer && ammo != null)
            ammo.enabled = true;

        // Garante câmera desativada em jogadores remotos — nunca devem ter câmera ativa
        if (playerCamera != null)
            playerCamera.SetActive(false);

        Debug.Log($"[NetworkPlayerController] Jogador remoto configurado. ClientId={OwnerClientId}");
    }

    private void SetInputComponentsActive(bool active)
    {
        if (playerInput == null)
            TryGetComponent(out playerInput);

        if (inputHandler == null)
            TryGetComponent(out inputHandler);

        if (playerInput != null)
            playerInput.enabled = active;

        if (inputHandler != null) inputHandler.enabled = active;
        if (movement != null) movement.enabled = active;
        if (shooting != null) shooting.enabled = active;
        if (meleeCombat != null) meleeCombat.enabled = active;
        if (aim != null) aim.enabled = active;
        if (dash != null) dash.enabled = active;
        if (abilityHandler != null) abilityHandler.enabled = active;
        if (adrenaline != null) adrenaline.enabled = active;
        if (ammo != null) ammo.enabled = active;
    }

    private void OnColorIndexChanged(int oldIndex, int newIndex)
    {
        ApplyPlayerColor(newIndex);
    }

    private void ApplyPlayerColor(int colorIndex)
    {
        if (playerRenderers == null || playerColors == null) return;
        if (colorIndex < 0 || colorIndex >= playerColors.Length) return;

        Color color = playerColors[colorIndex];
        foreach (var renderer in playerRenderers)
        {
            if (renderer != null)
                renderer.material.color = color;
        }
    }

    /// <summary>
    /// Permite que sistemas externos obtenham a cor deste jogador para uso em UI.
    /// </summary>
    public Color GetPlayerColor()
    {
        if (_playerColorIndex.Value < playerColors.Length)
            return playerColors[_playerColorIndex.Value];
        return Color.white;
    }
}
