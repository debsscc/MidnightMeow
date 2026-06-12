/// <summary>
/// MultiplayerCameraController.cs
/// Controlador central do rig de câmera no modo multiplayer.
/// Orquestra CinemachineCamera (Unity 6 / Cinemachine 3.x) para seguir apenas o jogador
/// local (owner), delegando shake a CameraShakeController e cutscenes a CameraCutsceneController.
/// Detecta automaticamente o jogador local via pooling com retry, ou pode ser configurado
/// diretamente via SetTarget().
///
/// ARQUITETURA DO RIG DE CÂMERA (ver seção de hierarquia abaixo):
///   - Este componente fica no GameObject raiz do rig.
///   - CinemachineCamera é um filho separado ou referenciado via SerializeField.
///   - O zoom padrão é lido do CameraConfig e aplicado ao Lens.OrthographicSize.
///   - Shake e cutscenes são delegados a sub-componentes especializados.
///
/// HIERARQUIA RECOMENDADA:
///   MultiplayerCameraRig
///     ├── [Component] MultiplayerCameraController
///     ├── MainCamera
///     │     ├── [Component] Camera (tag: MainCamera)
///     │     ├── [Component] CinemachineBrain (UpdateMethod: Late Update)
///     │     ├── [Component] AudioListener
///     │     └── [Component] CameraShakeController
///     └── PlayerVirtualCamera
///           └── [Component] CinemachineCamera
///                 Body → CinemachinePositionComposer (Damping X/Y da CameraConfig)
///
/// SRP: orquestra o rig de câmera; não lida com conexão, gameplay ou UI.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerCameraController : MonoBehaviour
{
    public static MultiplayerCameraController Instance { get; private set; }

    [Header("Referências do Rig")]
    [Tooltip("CinemachineCamera (virtual camera) que seguirá o jogador local.")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Tooltip("CameraShakeController no GameObject da câmera principal.")]
    [SerializeField] private CameraShakeController shakeController;

    [Tooltip("CameraCutsceneController para movimentos temporários de câmera.")]
    [SerializeField] private CameraCutsceneController cutsceneController;

    [Header("Configuração")]
    [SerializeField] private CameraConfig config;

    [Tooltip("Intervalo em segundos entre tentativas de achar o jogador local.")]
    [SerializeField] private float findPlayerRetryInterval = 0.5f;

    [Header("Limites da câmera")]
    [Tooltip("Liga automaticamente um CameraBoundsVolume da cena ao Cinemachine Confiner.")]
    [SerializeField] private bool autoBindSceneBounds = true;

    [Header("Multiplayer")]
    [Tooltip("Follow direto na MainCamera (2D). Mais confiável que CinemachineBrain em clientes NGO.")]
    [SerializeField] private bool useDirectCameraFollow = true;

    [Header("Diagnostico")]
    [SerializeField] private GameplayDiagnosticConfig diagnosticConfig;
    [SerializeField] private bool useConfigAsset = true;
    [Tooltip("Usado só se Use Config Asset estiver desmarcado ou Config for nulo.")]
    [SerializeField] private bool enableDiagnosticsLogs = true;
    [SerializeField] private float diagnosticsIntervalSeconds = 2f;
    [SerializeField] private float findPlayerTimeoutSeconds = 45f;

    private Transform _currentTarget;
    private float _targetOrthographicSize;
    private bool _isZooming = false;
    private Coroutine _findPlayerCoroutine;
    private Camera _mainCam;
    private bool _useFallbackFollow = false;
    private Coroutine _diagnosticsCoroutine;
    private Unity.Cinemachine.CinemachineBrain _cinemachineBrain;
    private bool _brainDisabledForDirectFollow;
    private Collider2D _sceneBoundsCollider;

    public Transform CurrentTarget => _currentTarget;
    public bool HasTarget => _currentTarget != null;

    public bool IsFollowingTarget =>
        _currentTarget != null
        && virtualCamera != null
        && virtualCamera.Follow == _currentTarget;
    public Camera MainCamera
    {
        get
        {
            if (_mainCam == null || !_mainCam.isActiveAndEnabled)
                _mainCam = ResolveMainCamera();
            return _mainCam;
        }
    }

    /// <summary>Resolve o controlador ativo (singleton ou busca na cena de gameplay).</summary>
    public static MultiplayerCameraController Resolve()
    {
        Scene active = SceneManager.GetActiveScene();
        if (Instance != null && IsControllerInActiveGameplayScene(Instance, active))
            return Instance;

        if (Instance != null)
            Instance = null;

        MultiplayerCameraController found =
            Object.FindFirstObjectByType<MultiplayerCameraController>(FindObjectsInactive.Include);
        if (found != null && !found.gameObject.activeSelf)
            found.gameObject.SetActive(true);

        return Instance ?? found;
    }

    private static bool IsControllerInActiveGameplayScene(MultiplayerCameraController controller, Scene active)
    {
        return controller != null
               && controller.gameObject.scene == active
               && GameplaySceneBootstrap.IsGameplayScene(active.name);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            bool replaceInstance = ShouldReplaceInstance(Instance, this);
            if (replaceInstance)
            {
                Destroy(Instance.gameObject);
                Instance = this;
                return;
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private static bool ShouldReplaceInstance(MultiplayerCameraController current, MultiplayerCameraController candidate)
    {
        if (current == null || candidate == null)
            return candidate != null;

        bool currentIsClone = IsRuntimeClone(current);
        bool candidateIsClone = IsRuntimeClone(candidate);
        if (currentIsClone && !candidateIsClone)
            return true;

        if (!currentIsClone && candidateIsClone)
            return false;

        return BelongsToActiveScene(candidate) && !BelongsToActiveScene(current);
    }

    private static bool IsRuntimeClone(MultiplayerCameraController controller) =>
        controller != null && controller.gameObject.name.Contains("(Clone)");

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private static bool BelongsToActiveScene(MultiplayerCameraController controller)
    {
        if (controller == null)
            return false;

        Scene active = SceneManager.GetActiveScene();
        return controller.gameObject.scene == active
               && GameplaySceneBootstrap.IsGameplayScene(active.name);
    }

    private void Start()
    {
        AutoResolveVirtualCameraIfNeeded();
        TryBindCameraBounds();
        _mainCam = ResolveMainCamera();
        DisableLegacyCameraFollowIfPresent();
        ApplyConfigToCamera();
        InitializeSubControllers();
        _findPlayerCoroutine = StartCoroutine(FindLocalPlayerRoutine());
        RefreshDiagnosticsRoutine();
    }

    private bool DiagnosticsEnabled =>
        useConfigAsset && diagnosticConfig != null
            ? diagnosticConfig.masterEnabled && diagnosticConfig.cameraDiagnostics
            : enableDiagnosticsLogs;

    private void RefreshDiagnosticsRoutine()
    {
        if (_diagnosticsCoroutine != null)
        {
            StopCoroutine(_diagnosticsCoroutine);
            _diagnosticsCoroutine = null;
        }

        if (DiagnosticsEnabled)
            _diagnosticsCoroutine = StartCoroutine(DiagnosticsRoutine());
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (isActiveAndEnabled && Application.isPlaying)
            RefreshDiagnosticsRoutine();
    }
#endif

    private void OnEnable()
    {
        NetworkPlayerController.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        NetworkPlayerController.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
        EnsureInitialized();
        TryFindLocalPlayer();
    }

    /// <summary>Garante referências resolvidas antes de Start (bind no spawn NGO).</summary>
    public void EnsureInitialized()
    {
        AutoResolveVirtualCameraIfNeeded();
        if (_mainCam == null || !_mainCam.isActiveAndEnabled)
            _mainCam = ResolveMainCamera();

        if (_sceneBoundsCollider == null)
            TryBindCameraBounds();
    }

    private void OnDisable()
    {
        NetworkPlayerController.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        NetworkPlayerController.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
        if (_diagnosticsCoroutine != null)
        {
            StopCoroutine(_diagnosticsCoroutine);
            _diagnosticsCoroutine = null;
        }
    }

    private Camera ResolveMainCamera()
    {
        Camera childCamera = GetComponentInChildren<Camera>(true);
        if (childCamera != null)
            return childCamera;

        Camera taggedCamera = Camera.main;
        if (taggedCamera != null && taggedCamera.isActiveAndEnabled)
            return taggedCamera;

        Camera anyCamera = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
        if (anyCamera != null)
            Debug.LogWarning($"[MultiplayerCameraController] Câmera do rig não encontrada; usando fallback '{anyCamera.name}'.");

        return anyCamera;
    }

    private IEnumerator DiagnosticsRoutine()
    {
        while (true)
        {
            LogDiagnosticSnapshot("tick");
            yield return new WaitForSeconds(diagnosticsIntervalSeconds);
        }
    }

    private void LogDiagnosticSnapshot(string source)
    {
        if (!DiagnosticsEnabled) return;

        string camName = _mainCam != null ? _mainCam.name : "NULL";
        bool camActive = _mainCam != null && _mainCam.isActiveAndEnabled;
        string vcName = virtualCamera != null ? virtualCamera.name : "NULL";
        bool vcActive = virtualCamera != null && virtualCamera.isActiveAndEnabled;
        string followName = virtualCamera != null && virtualCamera.Follow != null ? virtualCamera.Follow.name : "NULL";
        string targetName = _currentTarget != null ? _currentTarget.name : "NULL";
        bool sameTarget = virtualCamera != null && virtualCamera.Follow == _currentTarget;
        string position = _mainCam != null ? _mainCam.transform.position.ToString("F2") : "N/A";

        string role = NetworkManager.Singleton == null
            ? "offline"
            : NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsClient
                ? "host"
                : NetworkManager.Singleton.IsClient
                    ? "client"
                    : "server";

        Debug.Log(
            $"[CAM-DIAG][{source}][{role}] mainCam={camName} active={camActive} pos={position} | " +
            $"vCam={vcName} active={vcActive} follow={followName} | target={targetName} sameRef={sameTarget} | " +
            $"fallbackFollow={_useFallbackFollow}");
    }

    private void DisableLegacyCameraFollowIfPresent()
    {
        if (_mainCam == null) return;

        FollowCamera legacyFollow = _mainCam.GetComponent<FollowCamera>();
        if (legacyFollow != null && legacyFollow.enabled)
        {
            legacyFollow.enabled = false;
            Debug.Log("[MultiplayerCameraController] FollowCamera legado desabilitado para evitar conflito no multiplayer.");
        }
    }

    private void Update()
    {
        if (_isZooming && virtualCamera != null)
            AnimateZoom();

        if (_currentTarget == null && _findPlayerCoroutine == null)
            _findPlayerCoroutine = StartCoroutine(FindLocalPlayerRoutine());
    }

    private void LateUpdate()
    {
        if (_currentTarget == null)
            return;

        if (_mainCam == null || !_mainCam.isActiveAndEnabled)
            _mainCam = ResolveMainCamera();

        if (_mainCam == null)
            return;

        if (!useDirectCameraFollow && !_useFallbackFollow)
            return;

        Vector3 desired = new Vector3(_currentTarget.position.x, _currentTarget.position.y, GetGameplayCameraZ());
        desired = ClampCameraPosition(desired);
        Vector3 next = Vector3.Lerp(
            _mainCam.transform.position,
            desired,
            Time.deltaTime * 15f);
        _mainCam.transform.position = ClampCameraPosition(next);
    }

    private float GetGameplayCameraZ()
    {
        if (_mainCam != null && !Mathf.Approximately(_mainCam.transform.position.z, 0f))
            return _mainCam.transform.position.z;

        if (config != null && !Mathf.Approximately(config.followOffset.z, 0f))
            return config.followOffset.z;

        return -10f;
    }

    // ── Inicialização ──────────────────────────────────────────────────────────

    private void ApplyConfigToCamera()
    {
        if (config == null)
        {
            Debug.LogError("[MultiplayerCameraController] CameraConfig não atribuído no Inspector! Atribua um CameraConfig ScriptableObject.");
            return;
        }
        if (virtualCamera == null)
        {
            Debug.LogError("[MultiplayerCameraController] Virtual Camera (CinemachineCamera) não atribuído no Inspector! " +
                           "Crie o filho 'PlayerVirtualCamera' com CinemachineCamera e arraste para este campo.");
            return;
        }

        var lens = virtualCamera.Lens;
        lens.OrthographicSize = config.defaultOrthographicSize;
        virtualCamera.Lens = lens;
        _targetOrthographicSize = config.defaultOrthographicSize;

        // Configura o damping do CinemachinePositionComposer se presente
        bool hasPositionComposer = virtualCamera.TryGetComponent<CinemachinePositionComposer>(out var composer);
        bool hasFollow = virtualCamera.TryGetComponent<CinemachineFollow>(out _);

        if (hasPositionComposer)
        {
            composer.Damping = new Vector3(config.followDampingX, config.followDampingY, 0f);
            Debug.Log("[MultiplayerCameraController] CinemachinePositionComposer encontrado e configurado.");
        }
        else if (hasFollow)
        {
            Debug.Log("[MultiplayerCameraController] CinemachineFollow encontrado (sem damping customizado).");
        }
        else
        {
            _useFallbackFollow = true;
            Debug.LogWarning("[MultiplayerCameraController] AVISO: CinemachineCamera não tem Body (CinemachinePositionComposer ou CinemachineFollow)!\n" +
                             "→ No PlayerVirtualCamera: Add Component → Cinemachine Position Composer\n" +
                             "→ Usando fallback: câmera movida diretamente via LateUpdate (sem suavização Cinemachine).");
        }

        Debug.Log($"[MultiplayerCameraController] Config aplicada. OrthographicSize={config.defaultOrthographicSize}");
    }

    private void TryBindCameraBounds()
    {
        if (!autoBindSceneBounds || virtualCamera == null)
            return;

        CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
            confiner = virtualCamera.gameObject.AddComponent<CinemachineConfiner2D>();

        CameraBoundsVolume volume = FindFirstObjectByType<CameraBoundsVolume>();
        if (volume == null || volume.BoundsCollider == null)
            return;

        confiner.BoundingShape2D = volume.BoundsCollider;
        _sceneBoundsCollider = volume.BoundsCollider;
        Debug.Log($"[MultiplayerCameraController] Limites da câmera ligados a '{volume.name}'.");
    }

    private Vector3 ClampCameraPosition(Vector3 position)
    {
        if (_sceneBoundsCollider == null)
            return position;

        return CameraBoundsClampUtility.ClampOrthographicPosition(
            position,
            _sceneBoundsCollider,
            GetActiveOrthographicSize(),
            GetActiveAspect());
    }

    private float GetActiveOrthographicSize()
    {
        if (_mainCam != null && _mainCam.orthographic)
            return _mainCam.orthographicSize;

        if (virtualCamera != null)
            return virtualCamera.Lens.OrthographicSize;

        return config != null ? config.defaultOrthographicSize : 8f;
    }

    private float GetActiveAspect()
    {
        if (_mainCam != null)
            return _mainCam.aspect;

        return Screen.width > 0 && Screen.height > 0
            ? (float)Screen.width / Screen.height
            : 16f / 9f;
    }

    private void AutoResolveVirtualCameraIfNeeded()
    {
        if (virtualCamera != null) return;

        virtualCamera = GetComponentInChildren<CinemachineCamera>(true);
        if (virtualCamera != null)
        {
            Debug.Log($"[MultiplayerCameraController] VirtualCamera auto-resolvida: '{virtualCamera.name}'.");
            return;
        }

        var allCams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        var candidates = new List<CinemachineCamera>(allCams.Length);
        for (int i = 0; i < allCams.Length; i++)
        {
            if (allCams[i] != null && allCams[i].name.Contains("PlayerVirtualCamera"))
                candidates.Add(allCams[i]);
        }

        if (candidates.Count > 0)
        {
            virtualCamera = candidates[0];
            Debug.LogWarning($"[MultiplayerCameraController] VirtualCamera atribuida por fallback: '{virtualCamera.name}'.");
        }
    }

    private void InitializeSubControllers()
    {
        if (cutsceneController != null && virtualCamera != null)
            cutsceneController.Initialize(virtualCamera, config);
    }

    // ── Detecção do Jogador Local ──────────────────────────────────────────────

    private IEnumerator FindLocalPlayerRoutine()
    {
        Debug.Log("[MultiplayerCameraController] Aguardando spawn do jogador local...");
        float elapsed = 0f;
        while (_currentTarget == null)
        {
            TryFindLocalPlayer();
            if (_currentTarget != null)
                break;

            elapsed += findPlayerRetryInterval;
            if (elapsed >= findPlayerTimeoutSeconds)
            {
                Debug.LogWarning(
                    $"[MultiplayerCameraController] Timeout ({findPlayerTimeoutSeconds}s) aguardando jogador local. " +
                    $"cena='{SceneManager.GetActiveScene().name}' " +
                    $"playerObject={(NetworkManager.Singleton?.LocalClient?.PlayerObject != null ? NetworkManager.Singleton.LocalClient.PlayerObject.name : "NULL")}");
                elapsed = 0f;
            }

            yield return new WaitForSeconds(findPlayerRetryInterval);
        }
        _findPlayerCoroutine = null;
        Debug.Log($"[MultiplayerCameraController] Jogador local encontrado: {_currentTarget.name}");
    }

    /// <summary>
    /// Busca o NetworkPlayerController do jogador local (IsOwner = true) e o define como alvo.
    /// Chamado automaticamente pela coroutine, mas pode ser invocado manualmente ao spawnar.
    /// </summary>
    public void TryFindLocalPlayer()
    {
        if (NetworkManager.Singleton != null
            && NetworkManager.Singleton.LocalClient != null
            && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            Transform playerObjectTransform = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
            NetworkPlayerController playerController = playerObjectTransform.GetComponent<NetworkPlayerController>();
            Transform followTarget = playerController != null
                ? playerController.GetCameraFollowTransform()
                : playerObjectTransform;
            if (DiagnosticsEnabled)
                Debug.Log($"[CAM-DIAG][TryFindLocalPlayer] usando LocalClient.PlayerObject: {followTarget.name}");
            SetTarget(followTarget);
            return;
        }

        var allPlayers = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        if (DiagnosticsEnabled)
            Debug.Log($"[CAM-DIAG][TryFindLocalPlayer] playersEncontrados={allPlayers.Length}");

        foreach (var player in allPlayers)
        {
            if (player.IsOwner)
            {
                if (DiagnosticsEnabled)
                    Debug.Log($"[CAM-DIAG][TryFindLocalPlayer] owner encontrado: {player.name} clientId={player.OwnerClientId}");
                SetTarget(player.GetCameraFollowTransform());
                return;
            }
        }

        if (DiagnosticsEnabled)
            Debug.LogWarning("[CAM-DIAG][TryFindLocalPlayer] nenhum player owner encontrado neste frame.");
    }

    private void HandleLocalPlayerSpawned(NetworkPlayerController localPlayer)
    {
        if (localPlayer == null) return;
        SetTarget(localPlayer.GetCameraFollowTransform());
    }

    private void HandleLocalPlayerDespawned(ulong _)
    {
        ClearTarget();
    }

    // ── API Pública — Alvo ─────────────────────────────────────────────────────

    /// <summary>
    /// Define o Transform que a câmera seguirá. Teleporta imediatamente para evitar
    /// interpolação inicial indesejada, depois deixa o Cinemachine suavizar.
    /// </summary>
    public void SetTarget(Transform target)
    {
        LogDiagnosticSnapshot("SetTarget-before");
        EnsureInitialized();

        if (target == null)
        {
            ClearTarget();
            Debug.LogWarning("[MultiplayerCameraController] SetTarget chamado com target=null. Câmera ficará parada.");
            return;
        }

        if (virtualCamera == null)
        {
            Debug.LogError("[MultiplayerCameraController] SetTarget: virtualCamera é NULL! " +
                           "Verifique MultiplayerCameraRig → PlayerVirtualCamera no Inspector.");
            return;
        }

        if (_mainCam == null || !_mainCam.isActiveAndEnabled)
            _mainCam = ResolveMainCamera();

        CameraTarget camTarget = virtualCamera.Target;
        camTarget.TrackingTarget = target;
        camTarget.CustomLookAtTarget = false;
        virtualCamera.Target = camTarget;
        virtualCamera.Follow = target;
        EnsureVirtualCameraLive();

        Vector3 cameraPosition = ClampCameraPosition(
            new Vector3(target.position.x, target.position.y, GetGameplayCameraZ()));
        if (_mainCam != null)
        {
            _mainCam.transform.position = cameraPosition;
            if (!_mainCam.isActiveAndEnabled)
                _mainCam.enabled = true;
        }

        virtualCamera.ForceCameraPosition(cameraPosition, Quaternion.identity);

        PlayerAim aim = target.GetComponentInParent<PlayerAim>();
        if (aim != null)
            aim.SetAimCamera(_mainCam);

        _currentTarget = target;

        ConfigureDirectFollowRendering();

        Debug.Log($"[MultiplayerCameraController] SetTarget → '{target.name}' em {target.position}. " +
                  $"mainCam={_mainCam?.name ?? "NULL"} follow={virtualCamera.Follow?.name ?? "NULL"} " +
                  $"role={(NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer ? "host" : "client")}");

        TransitionCameraKeeper.Refresh();
        LogDiagnosticSnapshot("SetTarget-after");
    }

    private void ConfigureDirectFollowRendering()
    {
        if (_mainCam == null)
            return;

        GameplayCameraSceneUtility.TakeOverGameplayRendering(_mainCam);

        if (!useDirectCameraFollow || _brainDisabledForDirectFollow)
            return;

        if (_cinemachineBrain == null)
            _cinemachineBrain = _mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();

        if (_cinemachineBrain != null && _cinemachineBrain.enabled)
        {
            _cinemachineBrain.enabled = false;
            _brainDisabledForDirectFollow = true;
        }
    }

    private void EnsureVirtualCameraLive()
    {
        if (virtualCamera == null)
            return;

        if (!virtualCamera.gameObject.activeSelf)
            virtualCamera.gameObject.SetActive(true);

        if (!virtualCamera.enabled)
            virtualCamera.enabled = true;

        PrioritySettings priority = virtualCamera.Priority;
        if (!priority.Enabled || priority.Value <= 0)
        {
            priority.Enabled = true;
            priority.Value = 10;
            virtualCamera.Priority = priority;
        }
    }

    /// <summary>Remove o alvo atual. A câmera para de se mover.</summary>
    public void ClearTarget()
    {
        _currentTarget = null;
        if (virtualCamera != null) virtualCamera.Follow = null;
        LogDiagnosticSnapshot("ClearTarget");
    }

    // ── API Pública — Zoom ─────────────────────────────────────────────────────

    /// <summary>
    /// Define o tamanho ortográfico alvo. A câmera interpola suavemente.
    /// O valor é clampado entre CameraConfig.minOrthographicSize e maxOrthographicSize.
    /// </summary>
    public void SetZoom(float orthographicSize)
    {
        if (config == null) return;
        _targetOrthographicSize = Mathf.Clamp(orthographicSize, config.minOrthographicSize, config.maxOrthographicSize);
        _isZooming = true;
    }

    /// <summary>Restaura o zoom padrão configurado em CameraConfig.</summary>
    public void ResetZoom()
    {
        if (config != null) SetZoom(config.defaultOrthographicSize);
    }

    /// <summary>Define o zoom imediatamente, sem interpolação.</summary>
    public void SetZoomImmediate(float orthographicSize)
    {
        if (virtualCamera == null || config == null) return;
        float clamped = Mathf.Clamp(orthographicSize, config.minOrthographicSize, config.maxOrthographicSize);
        _targetOrthographicSize = clamped;
        var lens = virtualCamera.Lens;
        lens.OrthographicSize = clamped;
        virtualCamera.Lens = lens;
        _isZooming = false;
    }

    private void AnimateZoom()
    {
        if (virtualCamera == null) return;

        float speed = config != null ? config.zoomLerpSpeed : 5f;
        var lens = virtualCamera.Lens;
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, _targetOrthographicSize, speed * Time.deltaTime);
        virtualCamera.Lens = lens;

        if (Mathf.Abs(lens.OrthographicSize - _targetOrthographicSize) < 0.01f)
        {
            lens.OrthographicSize = _targetOrthographicSize;
            virtualCamera.Lens = lens;
            _isZooming = false;
        }
    }

    // ── API Pública — Shake ────────────────────────────────────────────────────

    /// <summary>
    /// Aciona shake com preset (Light / Medium / Heavy).
    /// Delegado ao CameraShakeController; seguro chamar de qualquer sistema.
    /// </summary>
    public void Shake(CameraShakePreset preset = CameraShakePreset.Medium)
    {
        shakeController?.Shake(preset);
    }

    /// <summary>
    /// Aciona shake com valores customizados de intensidade e duração.
    /// Use para eventos únicos que não se encaixam nos presets padrão.
    /// </summary>
    public void ShakeCustom(float intensity, float duration)
    {
        shakeController?.ShakeCustom(intensity, duration);
    }

    // ── API Pública — Cutscene ─────────────────────────────────────────────────

    /// <summary>
    /// Move a câmera suavemente para uma posição no mundo, aguarda e retorna ao jogador.
    /// A câmera volta automaticamente após CameraConfig.cutsceneHoldDuration segundos.
    /// </summary>
    /// <param name="worldPosition">Posição de destino no mundo.</param>
    /// <param name="onArrived">Callback quando a câmera chegar ao ponto.</param>
    /// <param name="onComplete">Callback quando a câmera retornar ao jogador.</param>
    public void PanToPoint(Vector3 worldPosition, System.Action onArrived = null, System.Action onComplete = null)
    {
        cutsceneController?.PanToPoint(worldPosition, onArrived, onComplete);
    }

    /// <summary>
    /// Move a câmera para seguir um Transform por uma duração, depois retorna ao jogador.
    /// Útil para destacar spawn de boss ou evento importante.
    /// </summary>
    public void PanToTransform(Transform target, float duration, System.Action onComplete = null)
    {
        cutsceneController?.PanToTransform(target, duration, onComplete);
    }

    /// <summary>Cancela qualquer cutscene ativa e restaura o follow imediatamente.</summary>
    public void CancelCutscene()
    {
        cutsceneController?.CancelCutscene();
    }

    // ── API Pública — Shader / Pós-processamento (stubs para expansão futura) ──

    /// <summary>
    /// [Stub] Aciona efeito de flash de dano usando URP Volume.
    /// Implementação futura: animar um parâmetro de Vignette/ChromaticAberration no Volume.
    /// </summary>
    public void TriggerDamageEffect()
    {
        Debug.Log("[MultiplayerCameraController] TriggerDamageEffect chamado (aguardando implementação URP).");
        // TODO: Usar volume.profile.TryGet<Vignette>(out var vignette) e animar intensity
    }

    /// <summary>
    /// [Stub] Aplica um shader/material de pós-processamento à câmera temporariamente.
    /// Implementação futura: usar Camera.SetReplacementShader ou Full-Screen Render Feature.
    /// </summary>
    public void ApplyPostProcessingEffect(string effectName, float duration)
    {
        Debug.Log($"[MultiplayerCameraController] ApplyPostProcessingEffect('{effectName}', {duration}s) chamado (aguardando implementação).");
        // TODO: Ativar/desativar Volume components via nome do efeito
    }
}
