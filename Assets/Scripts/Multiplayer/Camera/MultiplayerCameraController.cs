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

    [Header("Acessibilidade — Camera Bounce")]
    [Tooltip("Lean + breathing (head bob). Se desligado, a câmera segue o jogador sem bounce. Também respeita CameraConfig.enableCameraBounce.")]
    [SerializeField] private bool enableCameraBounce = true;

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
    private bool _deathFocusActive;
    private float _savedOrthographicSize;
    private bool _introZoomActive;
    private float _introZoomTimer;

    // Juice: lean / breathing / zoom punch
    private Vector2 _leanOffset;
    private Vector2 _targetLean;
    private float _breathWeight;
    private float _breathPhase;
    private float _locomotionInputMagnitude;
    private float _zoomPunchOffset;
    private float _zoomPunchRecoverOverride = -1f;
    private float _zoomBaseSize;

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
        BeginIntroZoomIfConfigured();
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
        if (_introZoomActive)
            UpdateIntroZoom();
        else if (_isZooming && virtualCamera != null)
            AnimateZoom();

        UpdateZoomPunch();

        if (_currentTarget == null && _findPlayerCoroutine == null)
            _findPlayerCoroutine = StartCoroutine(FindLocalPlayerRoutine());
    }

    private void LateUpdate()
    {
        TickLocomotionFeel();

        if (_currentTarget == null)
            return;

        if (_mainCam == null || !_mainCam.isActiveAndEnabled)
            _mainCam = ResolveMainCamera();

        if (_mainCam == null)
            return;

        if (!useDirectCameraFollow && !_useFallbackFollow)
            return;

        Vector3 desired = ComputeEdgeFollowPosition();
        if (IsCameraBounceEnabled())
            desired += (Vector3)(_leanOffset + ComputeBreathingOffset());
        desired = ClampCameraPosition(desired);
        float smoothing = config != null ? config.edgePanSmoothing : 15f;
        Vector3 next = Vector3.Lerp(
            _mainCam.transform.position,
            desired,
            Time.deltaTime * smoothing);
        _mainCam.transform.position = ClampCameraPosition(next);
    }

    private Vector3 ComputeEdgeFollowPosition()
    {
        Vector3 playerPos = _currentTarget.position;
        float z = GetGameplayCameraZ();

        if (config == null || _mainCam == null)
            return new Vector3(playerPos.x, playerPos.y, z);

        Vector3 camPos = _mainCam.transform.position;
        float halfHeight = GetActiveOrthographicSize();
        float halfWidth = halfHeight * GetActiveAspect();
        float marginX = halfWidth * (1f - config.edgeDeadZoneX * 2f);
        float marginY = halfHeight * (1f - config.edgeDeadZoneY * 2f);

        float targetX = camPos.x;
        float targetY = camPos.y;
        float deltaX = playerPos.x - camPos.x;
        float deltaY = playerPos.y - camPos.y;

        if (Mathf.Abs(deltaX) > marginX)
            targetX = playerPos.x - Mathf.Sign(deltaX) * marginX;
        if (Mathf.Abs(deltaY) > marginY)
            targetY = playerPos.y - Mathf.Sign(deltaY) * marginY;

        return new Vector3(targetX, targetY, z);
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
        _zoomBaseSize = config.defaultOrthographicSize;

        if (_mainCam != null && _mainCam.orthographic)
            _mainCam.orthographicSize = config.defaultOrthographicSize;

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

    private void BeginIntroZoomIfConfigured()
    {
        if (config == null || virtualCamera == null || !config.playIntroZoom || config.introZoomInAmount <= 0f)
            return;

        float startSize = config.defaultOrthographicSize + config.introZoomInAmount;
        ApplyOrthographicSize(startSize);
        _introZoomTimer = 0f;
        _introZoomActive = true;
    }

    private void UpdateIntroZoom()
    {
        if (config == null)
        {
            _introZoomActive = false;
            return;
        }

        _introZoomTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_introZoomTimer / config.introZoomDuration);
        float smoothT = SmoothIntroZoomT(t);
        float startSize = config.defaultOrthographicSize + config.introZoomInAmount;
        float size = Mathf.Lerp(startSize, config.defaultOrthographicSize, smoothT);
        _zoomBaseSize = size;
        ApplyOrthographicSize(size);

        if (t >= 1f)
        {
            ApplyOrthographicSize(config.defaultOrthographicSize);
            _targetOrthographicSize = config.defaultOrthographicSize;
            _zoomBaseSize = config.defaultOrthographicSize;
            _introZoomActive = false;
        }
    }

    private void ApplyOrthographicSize(float size)
    {
        if (virtualCamera != null)
        {
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = size;
            virtualCamera.Lens = lens;
        }

        if (_mainCam != null && _mainCam.orthographic)
            _mainCam.orthographicSize = size;
    }

    private static float SmoothIntroZoomT(float t) =>
        t * t * t * (t * (t * 6f - 15f) + 10f);

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

    public float GetActiveOrthographicSize()
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
        _zoomBaseSize = clamped;
        _isZooming = false;
        ApplyDisplayedZoom();
    }

    private void AnimateZoom()
    {
        if (virtualCamera == null && _mainCam == null)
            return;

        float speed = config != null ? config.zoomLerpSpeed : 5f;
        float delta = speed * Time.deltaTime;
        float currentBase = _zoomBaseSize > 0f ? _zoomBaseSize : GetActiveOrthographicSize();
        float nextBase = Mathf.Lerp(currentBase, _targetOrthographicSize, delta);
        _zoomBaseSize = nextBase;
        ApplyDisplayedZoom();

        if (Mathf.Abs(_zoomBaseSize - _targetOrthographicSize) < 0.01f)
        {
            _zoomBaseSize = _targetOrthographicSize;
            ApplyDisplayedZoom();

            if (!_deathFocusActive)
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

    /// <summary>
    /// Atualiza lean/breathing a partir do input/velocidade do jogador local.
    /// Sem efeito se enableCameraBounce estiver desligado (acessibilidade).
    /// </summary>
    public void SetLocomotionFeel(Vector2 moveInput, float speedMagnitude)
    {
        if (!IsCameraBounceEnabled() || _deathFocusActive)
        {
            _locomotionInputMagnitude = 0f;
            _targetLean = Vector2.zero;
            return;
        }

        float inputMag = moveInput.magnitude;
        _locomotionInputMagnitude = Mathf.Max(inputMag, speedMagnitude * 0.05f);

        if (config == null)
        {
            _targetLean = Vector2.zero;
            return;
        }

        if (inputMag < config.moveLeanMinInput)
        {
            _targetLean = Vector2.zero;
            return;
        }

        Vector2 dir = moveInput / inputMag;
        _targetLean = dir * config.moveLeanDistance * Mathf.Clamp01(inputMag);
    }

    /// <summary>Zoom punch curto (aproxima e volta). Usado em dash/habilidades.</summary>
    public void PunchZoom(float amount = -1f, float recoverSpeed = -1f)
    {
        if (_deathFocusActive || _introZoomActive || config == null)
            return;

        float punch = amount > 0f ? amount : config.zoomPunchAmount;
        if (punch <= 0f)
            return;

        _zoomPunchOffset = Mathf.Max(_zoomPunchOffset, punch);
        _zoomPunchOffset = Mathf.Min(_zoomPunchOffset, punch * 1.35f);

        _zoomPunchRecoverOverride = recoverSpeed > 0f ? recoverSpeed : -1f;
        ApplyDisplayedZoom();
    }

    private void TickLocomotionFeel()
    {
        if (!IsCameraBounceEnabled())
        {
            _targetLean = Vector2.zero;
            _leanOffset = Vector2.zero;
            _breathWeight = 0f;
            _locomotionInputMagnitude = 0f;
            return;
        }

        float dt = Time.deltaTime;
        float leanSmooth = config != null ? config.moveLeanSmoothing : 6f;
        _leanOffset = Vector2.Lerp(_leanOffset, _targetLean, dt * leanSmooth);

        float idleThreshold = config != null ? config.breathingIdleInputThreshold : 0.18f;
        float breathTarget = (!_deathFocusActive && _locomotionInputMagnitude < idleThreshold) ? 1f : 0f;
        float breathBlend = config != null ? config.breathingBlendSpeed : 3.5f;
        _breathWeight = Mathf.MoveTowards(_breathWeight, breathTarget, dt * breathBlend);

        float breathSpeed = config != null ? config.breathingSpeed : 0.4f;
        _breathPhase += dt * breathSpeed;
    }

    private Vector2 ComputeBreathingOffset()
    {
        if (!IsCameraBounceEnabled() || _breathWeight <= 0.001f || config == null || config.breathingAmplitude <= 0f)
            return Vector2.zero;

        float amp = config.breathingAmplitude * _breathWeight;
        return new Vector2(
            Mathf.Sin(_breathPhase * Mathf.PI * 2f) * amp,
            Mathf.Cos(_breathPhase * Mathf.PI * 2f * 0.73f) * amp * 0.55f);
    }

    /// <summary>
    /// Bounce = lean no movimento + breathing idle. Ambos devem estar ligados
    /// no Inspector do rig e no CameraConfig (AND).
    /// </summary>
    private bool IsCameraBounceEnabled()
    {
        if (!enableCameraBounce)
            return false;
        if (config != null && !config.enableCameraBounce)
            return false;
        return true;
    }

    private void UpdateZoomPunch()
    {
        if (_deathFocusActive || _introZoomActive)
        {
            if (_zoomPunchOffset > 0f)
            {
                _zoomPunchOffset = 0f;
                ApplyDisplayedZoom();
            }

            return;
        }

        if (_zoomPunchOffset <= 0f)
            return;

        float recover = _zoomPunchRecoverOverride > 0f
            ? _zoomPunchRecoverOverride
            : (config != null ? config.zoomPunchRecoverSpeed : 7f);

        _zoomPunchOffset = Mathf.MoveTowards(_zoomPunchOffset, 0f, Time.deltaTime * recover);
        ApplyDisplayedZoom();
    }

    private void ApplyDisplayedZoom()
    {
        float baseSize = _zoomBaseSize > 0.01f
            ? _zoomBaseSize
            : (_targetOrthographicSize > 0.01f
                ? _targetOrthographicSize
                : (config != null ? config.defaultOrthographicSize : 8f));

        float minSize = config != null ? config.minOrthographicSize : 1f;
        float size = Mathf.Max(minSize, baseSize - _zoomPunchOffset);
        ApplyOrthographicSize(size);
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

    /// <summary>Foco dramático no corpo morto: zoom + follow (ignora clamp min/max do config).</summary>
    public void BeginDeathFocus(float targetOrthographicSize, Transform focusBody)
    {
        if (focusBody != null)
            SetTarget(focusBody);

        _deathFocusActive = true;
        _zoomPunchOffset = 0f;

        if (_savedOrthographicSize <= 0f)
            _savedOrthographicSize = GetActiveOrthographicSize();

        _targetOrthographicSize = targetOrthographicSize;
        _zoomBaseSize = GetActiveOrthographicSize();
        _isZooming = true;
    }

    public void UpdateDeathFocusZoom(float orthographicSize)
    {
        _targetOrthographicSize = orthographicSize;
        _isZooming = true;
    }

    public void EndDeathFocus()
    {
        _deathFocusActive = false;

        if (_savedOrthographicSize > 0f)
        {
            _targetOrthographicSize = _savedOrthographicSize;
            _isZooming = true;
        }
        else if (config != null)
        {
            ResetZoom();
        }

        _savedOrthographicSize = 0f;
    }

    // ── API Pública — Shader / Pós-processamento (stubs para expansão futura) ──

    /// <summary>
    /// [Stub] Aciona efeito de flash de dano usando URP Volume.
    /// Implementação futura: animar um parâmetro de Vignette/ChromaticAberration no Volume.
    /// </summary>
    public void TriggerDamageEffect()
    {
        GameplayVignetteController.TriggerDamagePulse();
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
