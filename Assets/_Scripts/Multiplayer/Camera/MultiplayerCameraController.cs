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
using Unity.Cinemachine;
using UnityEngine;

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

    private Transform _currentTarget;
    private float _targetOrthographicSize;
    private bool _isZooming = false;
    private Coroutine _findPlayerCoroutine;
    private Camera _mainCam;
    private bool _useFallbackFollow = false;

    public Transform CurrentTarget => _currentTarget;
    public bool HasTarget => _currentTarget != null;
    public Camera MainCamera
    {
        get
        {
            if (_mainCam == null || !_mainCam.isActiveAndEnabled)
                _mainCam = ResolveMainCamera();
            return _mainCam;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _mainCam = ResolveMainCamera();
        ApplyConfigToCamera();
        InitializeSubControllers();
        _findPlayerCoroutine = StartCoroutine(FindLocalPlayerRoutine());
    }

    private Camera ResolveMainCamera()
    {
        Camera taggedCamera = Camera.main;
        if (taggedCamera != null && taggedCamera.isActiveAndEnabled)
            return taggedCamera;

        Camera childCamera = GetComponentInChildren<Camera>(true);
        if (childCamera != null && childCamera.isActiveAndEnabled)
            return childCamera;

        Camera anyCamera = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
        if (anyCamera != null)
            Debug.LogWarning($"[MultiplayerCameraController] Camera.main não encontrada; usando fallback '{anyCamera.name}'. Configure a tag MainCamera no objeto correto.");

        return anyCamera;
    }

    private void Update()
    {
        if (_isZooming && virtualCamera != null)
            AnimateZoom();
    }

    private void LateUpdate()
    {
        // Fallback: se o CinemachineCamera não tiver Body configurado, move a câmera diretamente
        if (_useFallbackFollow && _currentTarget != null && _mainCam != null)
        {
            Vector3 offset = config != null ? config.followOffset : new Vector3(0, 0, -10f);
            Vector3 targetPos = new Vector3(_currentTarget.position.x, _currentTarget.position.y, _mainCam.transform.position.z);
            _mainCam.transform.position = Vector3.Lerp(_mainCam.transform.position, targetPos, Time.deltaTime * 10f);
        }
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

    private void InitializeSubControllers()
    {
        if (cutsceneController != null && virtualCamera != null)
            cutsceneController.Initialize(virtualCamera, config);
    }

    // ── Detecção do Jogador Local ──────────────────────────────────────────────

    private IEnumerator FindLocalPlayerRoutine()
    {
        Debug.Log("[MultiplayerCameraController] Aguardando spawn do jogador local...");
        while (_currentTarget == null)
        {
            TryFindLocalPlayer();
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
        var allPlayers = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.IsOwner)
            {
                SetTarget(player.transform);
                return;
            }
        }
    }

    // ── API Pública — Alvo ─────────────────────────────────────────────────────

    /// <summary>
    /// Define o Transform que a câmera seguirá. Teleporta imediatamente para evitar
    /// interpolação inicial indesejada, depois deixa o Cinemachine suavizar.
    /// </summary>
    public void SetTarget(Transform target)
    {
        _currentTarget = target;

        if (target == null)
        {
            Debug.LogWarning("[MultiplayerCameraController] SetTarget chamado com target=null. Câmera ficará parada.");
            if (virtualCamera != null) virtualCamera.Follow = null;
            return;
        }

        Debug.Log($"[MultiplayerCameraController] SetTarget → '{target.name}' em {target.position}. virtualCamera={virtualCamera?.name ?? "NULL"}");

        if (virtualCamera == null)
        {
            Debug.LogError("[MultiplayerCameraController] SetTarget: virtualCamera é NULL! " +
                           "Verifique se o campo 'Virtual Camera' está preenchido no Inspector. " +
                           "A câmera NÃO seguirá o jogador.");
            return;
        }

        virtualCamera.Follow = target;

        // Teleporta a câmera principal imediatamente para evitar lerp inicial longo
        // Nota: virtualCamera é CinemachineCamera, não tem componente Camera.
        // Usamos Camera.main (o CinemachineBrain) para o teleporte direto.
        if (_mainCam == null || !_mainCam.isActiveAndEnabled)
            _mainCam = ResolveMainCamera();
        PlayerAim aim = target.GetComponent<PlayerAim>();
        if (aim != null)
            aim.SetAimCamera(_mainCam);

        if (_mainCam != null)
        {
            float zOffset = _mainCam.transform.position.z;
            _mainCam.transform.position = new Vector3(target.position.x, target.position.y, zOffset);
            Debug.Log($"[MultiplayerCameraController] Câmera teleportada para {_mainCam.transform.position}");
        }
        else
        {
            Debug.LogWarning("[MultiplayerCameraController] Camera.main não encontrada para teleporte. " +
                             "Certifique-se que o MainCamera tem tag 'MainCamera'.");
        }

        Debug.Log($"[MultiplayerCameraController] virtualCamera.Follow definido para '{target.name}'.");
    }

    /// <summary>Remove o alvo atual. A câmera para de se mover.</summary>
    public void ClearTarget()
    {
        _currentTarget = null;
        if (virtualCamera != null) virtualCamera.Follow = null;
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
