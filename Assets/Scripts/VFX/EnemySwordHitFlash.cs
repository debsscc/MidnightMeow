using UnityEngine;

/// <summary>
/// Flash branco/dourado no sprite quando o inimigo recebe hit melee (espada da Nixie).
/// Usa shader <see cref="EnemySwordHitFlash"/> via MaterialPropertyBlock.
/// </summary>
[DisallowMultipleComponent]
public class EnemySwordHitFlash : MonoBehaviour
{
    private static readonly int HitFlashAmountId = Shader.PropertyToID("_HitFlashAmount");

    [SerializeField] private float flashDecaySpeed = 12f;
    [SerializeField] private float flashIntensity = 1f;

    private SpriteRenderer[] _renderers;
    private MaterialPropertyBlock _propertyBlock;
    private Material _hitFlashMaterial;
    private float _flashAmount;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _propertyBlock = new MaterialPropertyBlock();
        _hitFlashMaterial = Resources.Load<Material>("EnemySwordHitFlash");

        if (_hitFlashMaterial == null)
        {
            Shader shader = Shader.Find("MidnightMeow/EnemySwordHitFlash");
            if (shader != null)
                _hitFlashMaterial = new Material(shader);
        }
    }

    private void Update()
    {
        if (_flashAmount <= 0f)
            return;

        _flashAmount = Mathf.MoveTowards(_flashAmount, 0f, Time.deltaTime * flashDecaySpeed);
        ApplyFlash();
    }

    public void PlayFlash(float intensity = 1f)
    {
        _flashAmount = Mathf.Max(_flashAmount, Mathf.Clamp01(intensity) * flashIntensity);
        ApplyFlash();
    }

    private void ApplyFlash()
    {
        if (_renderers == null || _renderers.Length == 0)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            SpriteRenderer renderer = _renderers[i];
            if (renderer == null)
                continue;

            EnsureHitFlashMaterial(renderer);
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(HitFlashAmountId, _flashAmount);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private void EnsureHitFlashMaterial(SpriteRenderer renderer)
    {
        if (_hitFlashMaterial == null || renderer.sharedMaterial == _hitFlashMaterial)
            return;

        if (renderer.sharedMaterial != null &&
            renderer.sharedMaterial.shader != null &&
            renderer.sharedMaterial.shader.name == "MidnightMeow/EnemySwordHitFlash")
            return;

        if (_hitFlashMaterial != null)
            renderer.sharedMaterial = _hitFlashMaterial;
    }
}
