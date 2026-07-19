using UnityEngine;
using UnityEngine.Rendering.Universal;

///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Escala/posiciona Light2D tipo Sprite para cobrir o viewport
// ortográfico atual (qualquer aspect / resolução), sem SizeDelta 1920×1080.
// ---------------------------------------------------------------- */

/// <summary>
/// Mantém uma Sprite Light 2D cobrindo a câmera de gameplay.
/// Use em luzes de “máscara/letterbox” de atmosfera (ex.: Texture_Light da Fase-3).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light2D))]
public sealed class OrthographicSpriteLightFitter : MonoBehaviour
{
    [Tooltip("Padding além do viewport (1 = exatamente a tela; 1.1 = 10% a mais).")]
    [SerializeField] private float coveragePadding = 1.08f;

    [Tooltip("Se true, segue o centro XY da câmera a cada frame.")]
    [SerializeField] private bool followCameraCenter = true;

    [Tooltip("Câmera explícita; se vazio, usa MultiplayerCameraController / Camera.main.")]
    [SerializeField] private Camera targetCamera;

    private Light2D _light;
    private Vector2 _spriteSize = Vector2.one;
    private int _lastWidth = -1;
    private int _lastHeight = -1;
    private float _lastOrtho = -1f;
    private float _lastAspect = -1f;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        CacheSpriteSize();
    }

    private void OnEnable() => ApplyFit(force: true);

    private void LateUpdate() => ApplyFit(force: false);

    /// <summary>Configura a partir do Light2D já presente (chamado pelo hierarchy fix).</summary>
    public void ConfigureFromLight(Light2D light)
    {
        _light = light;
        CacheSpriteSize();
        ApplyFit(force: true);
    }

    private void CacheSpriteSize()
    {
        if (_light == null)
            _light = GetComponent<Light2D>();

        Sprite cookie = _light != null ? _light.lightCookieSprite : null;
        if (cookie != null)
        {
            Bounds b = cookie.bounds;
            _spriteSize = new Vector2(Mathf.Max(0.01f, b.size.x), Mathf.Max(0.01f, b.size.y));
            return;
        }

        // Fallback: LocalBounds serializado / unidade.
        _spriteSize = Vector2.one;
    }

    private void ApplyFit(bool force)
    {
        Camera cam = ResolveCamera();
        if (cam == null || !cam.orthographic)
            return;

        int w = Screen.width;
        int h = Screen.height;
        float ortho = cam.orthographicSize;
        float aspect = cam.aspect;

        if (!force
            && w == _lastWidth
            && h == _lastHeight
            && Mathf.Abs(ortho - _lastOrtho) < 0.0001f
            && Mathf.Abs(aspect - _lastAspect) < 0.0001f
            && !followCameraCenter)
            return;

        _lastWidth = w;
        _lastHeight = h;
        _lastOrtho = ortho;
        _lastAspect = aspect;

        float halfHeight = ortho * coveragePadding;
        float halfWidth = halfHeight * aspect;

        float scaleX = (halfWidth * 2f) / _spriteSize.x;
        float scaleY = (halfHeight * 2f) / _spriteSize.y;
        float scale = Mathf.Max(scaleX, scaleY);

        Vector3 pos = transform.position;
        if (followCameraCenter)
        {
            pos.x = cam.transform.position.x;
            pos.y = cam.transform.position.y;
        }

        // scale.z nunca zero (Texture_Light legado tinha z=0).
        transform.SetPositionAndRotation(pos, transform.rotation);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
            return targetCamera;

        MultiplayerCameraController mp = MultiplayerCameraController.Resolve();
        if (mp != null && mp.MainCamera != null)
            return mp.MainCamera;

        return Camera.main;
    }
}
