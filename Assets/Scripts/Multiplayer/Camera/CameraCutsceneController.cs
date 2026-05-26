/// <summary>
/// CameraCutsceneController.cs
/// Gerencia movimentos temporários da câmera para pontos de interesse e retorno ao jogador.
/// Funciona sobrescrevendo temporariamente o Follow do CinemachineCamera para um Transform
/// auxiliar que é animado via coroutine.
/// Suporta callback ao completar e cancelamento antes do fim.
///
/// USO:
///   CameraCutsceneController.Instance.PanToPoint(worldPos, onComplete: () => Debug.Log("Voltou!"));
///   CameraCutsceneController.Instance.CancelCutscene();
///
/// ARQUITETURA (preparado para expansão futura):
///   - PanToPoint: move para posição + aguarda + retorna
///   - PanToTransform: segue um Transform dinamicamente
///   - PanSequence: sequência de pontos (roteiro de cutscene)
///
/// SRP: exclusivamente responsável pelo movimento de câmera em cutscenes.
/// </summary>

using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraCutsceneController : MonoBehaviour
{
    public static CameraCutsceneController Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private CameraConfig config;

    [Header("Transform Auxiliar de Cutscene")]
    [Tooltip("GameObject vazio usado como alvo temporário durante cutscenes. Criado automaticamente se nulo.")]
    [SerializeField] private Transform cutsceneTarget;

    public bool IsCutsceneActive { get; private set; } = false;

    private Transform _originalFollowTarget;
    private Coroutine _activeCutscene;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (cutsceneTarget == null)
        {
            GameObject go = new GameObject("[CutsceneTarget]");
            go.transform.SetParent(transform);
            cutsceneTarget = go.transform;
        }
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>
    /// Move a câmera suavemente para uma posição no mundo, aguarda e retorna ao jogador.
    /// Se uma cutscene já estiver ativa, ela é cancelada e a nova começa.
    /// </summary>
    /// <param name="worldPosition">Posição de destino da câmera.</param>
    /// <param name="onArrived">Callback disparado quando a câmera chegar ao destino.</param>
    /// <param name="onComplete">Callback disparado quando a câmera retornar ao jogador.</param>
    public void PanToPoint(Vector3 worldPosition, Action onArrived = null, Action onComplete = null)
    {
        CancelCutscene();
        _activeCutscene = StartCoroutine(PanToPointRoutine(worldPosition, onArrived, onComplete));
    }

    /// <summary>
    /// Move a câmera para seguir um Transform dinâmico por uma duração específica,
    /// depois retorna ao jogador. Útil para destacar um boss ou evento.
    /// </summary>
    /// <param name="target">Transform a seguir durante a cutscene.</param>
    /// <param name="duration">Duração em segundos antes de retornar.</param>
    /// <param name="onComplete">Callback ao retornar ao jogador.</param>
    public void PanToTransform(Transform target, float duration, Action onComplete = null)
    {
        CancelCutscene();
        _activeCutscene = StartCoroutine(PanToTransformRoutine(target, duration, onComplete));
    }

    /// <summary>
    /// Cancela qualquer cutscene ativa e restaura o follow do jogador imediatamente.
    /// </summary>
    public void CancelCutscene()
    {
        if (_activeCutscene != null)
        {
            StopCoroutine(_activeCutscene);
            _activeCutscene = null;
        }

        RestoreFollowTarget();
        IsCutsceneActive = false;
    }

    // ── Coroutines Internas ────────────────────────────────────────────────────

    private IEnumerator PanToPointRoutine(Vector3 destination, Action onArrived, Action onComplete)
    {
        IsCutsceneActive = true;
        SaveAndOverrideFollowTarget();

        float panDuration  = config != null ? 1f / config.cutscenePanSpeed       : 0.5f;
        float holdDuration = config != null ? config.cutsceneHoldDuration         : 1.5f;
        float returnDuration = config != null ? 1f / config.cutsceneReturnSpeed   : 0.3f;

        // 1. Pan para o destino
        yield return MoveTargetTo(destination, panDuration);
        onArrived?.Invoke();

        // 2. Aguarda no destino
        yield return new WaitForSeconds(holdDuration);

        // 3. Retorna ao jogador
        if (_originalFollowTarget != null)
            yield return MoveTargetTo(_originalFollowTarget.position, returnDuration);

        RestoreFollowTarget();
        IsCutsceneActive = false;
        _activeCutscene = null;
        onComplete?.Invoke();
    }

    private IEnumerator PanToTransformRoutine(Transform target, float duration, Action onComplete)
    {
        IsCutsceneActive = true;
        SaveAndOverrideFollowTarget();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target != null)
                cutsceneTarget.position = target.position;
            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreFollowTarget();
        IsCutsceneActive = false;
        _activeCutscene = null;
        onComplete?.Invoke();
    }

    private IEnumerator MoveTargetTo(Vector3 destination, float duration)
    {
        Vector3 start = cutsceneTarget.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Aplica curva de animação se configurada
            if (config != null && config.cutscenePanCurve != null && config.cutscenePanCurve.keys.Length > 0)
                t = config.cutscenePanCurve.Evaluate(t);
            else
                t = Mathf.SmoothStep(0f, 1f, t);

            cutsceneTarget.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }

        cutsceneTarget.position = destination;
    }

    private void SaveAndOverrideFollowTarget()
    {
        if (virtualCamera == null) return;
        _originalFollowTarget = virtualCamera.Follow;
        cutsceneTarget.position = _originalFollowTarget != null
            ? _originalFollowTarget.position
            : transform.position;
        virtualCamera.Follow = cutsceneTarget;
    }

    private void RestoreFollowTarget()
    {
        if (virtualCamera == null || _originalFollowTarget == null) return;
        virtualCamera.Follow = _originalFollowTarget;
    }

    // ── Referência Externa ────────────────────────────────────────────────────

    /// <summary>
    /// Permite que o MultiplayerCameraController injete a referência da CinemachineCamera.
    /// Chamado durante a inicialização do rig de câmera.
    /// </summary>
    public void Initialize(CinemachineCamera cam, CameraConfig cfg)
    {
        virtualCamera = cam;
        config = cfg;
    }
}
