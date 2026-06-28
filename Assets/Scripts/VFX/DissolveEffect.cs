using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dissolve sincronizado em todos os SpriteRenderers (incluindo filhos) + brilho/partículas.
/// Sequência: animação Dying até o fim → dissolve (visível → invisível) → esconde renderers.
/// </summary>
public class DissolveEffect : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AnimatorProfileBinder animationBinder;

    [Header("Alvos")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool includeChildRenderers = true;

    [Header("Configurações")]
    [Tooltip("Material com DissolveSprite ou VOiD1 2D Dissolve Shader Graph (.mat).")]
    [SerializeField] private Material dissolveMaterial;

    [SerializeField] private float duration = 1.2f;
    [Tooltip("Esconde quando o dissolve chega neste progresso do shader (0–1).")]
    [SerializeField] [Range(0.5f, 1f)] private float hideAtDissolveProgress = 0.82f;
    [Tooltip("VOiD1: esconde nesta fração linear do duration (o brilho acaba antes do fim do duration).")]
    [SerializeField] [Range(0.35f, 0.95f)] private float void1HideAtLinearTime = 0.92f;
    [SerializeField] private bool waitForDeathAnimation = true;
    [SerializeField] private string deathStateName = "Dying";
    [SerializeField] [Range(0.5f, 0.95f)] private float dissolveStartNormalizedTime = 0.98f;
    [SerializeField] private float deathAnimLeadTimeFallback = 0.52f;
    [SerializeField] private Color edgeColor = new Color(0.85f, 0.95f, 1f, 1f);
    [SerializeField] private float edgeIntensity = 3.5f;
    [SerializeField] private float sparkleIntensity = 2f;
    [SerializeField] private bool playSparkleParticles = true;

    private readonly List<SpriteRenderer> _targets = new List<SpriteRenderer>(4);
    private readonly List<Material> _instanceMaterials = new List<Material>(4);
    private readonly List<Material> _originalSharedMaterials = new List<Material>(4);

    private DissolveMaterialBinding _binding;
    private NetworkEnemyController _networkEnemyController;
    private bool _isPlaying;
    private bool _visualsHidden;
    private Animator _animator;
    private int _deathStateHash;
    private Coroutine _presentationRoutine;

    public bool IsPlaying => _isPlaying;

    /// <summary>Material de dissolve usado no death; reaproveitado pelo spawn (materialização).</summary>
    public Material DissolveTemplate => dissolveMaterial;

    public float Duration => EstimatedTotalDuration;

    /// <summary>Timeout de segurança para despawn no servidor (pior caso de espera + dissolve).</summary>
    public float EstimatedTotalDuration =>
        ResolveDeathClipLength() + duration + 0.5f;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animationBinder == null)
            animationBinder = GetComponent<AnimatorProfileBinder>();

        _animator = GetComponent<Animator>();
        _networkEnemyController = GetComponent<NetworkEnemyController>();
        _deathStateHash = Animator.StringToHash(deathStateName);
    }

    public void HideVisuals()
    {
        if (_visualsHidden)
            return;

        if (_isPlaying)
            StopPresentationRoutine();

        _visualsHidden = true;
        ReleaseMaterials();
        DeathVisualHider.Hide(transform, visualRoot);
    }

    public void HandleDeath()
    {
        if (_isPlaying)
            return;

        if (TryGetComponent<EnemySpawnPresentation>(out var spawnPresentation))
            spawnPresentation.CancelForDeath();

        if (dissolveMaterial == null)
        {
            Debug.LogWarning("[DissolveEffect] dissolveMaterial não atribuído.");
            FinishPresentation();
            return;
        }

        _binding = DissolveMaterialBinding.FromMaterial(dissolveMaterial);
        if (_binding.Driver == DissolveMaterialBinding.Kind.Unsupported)
        {
            Debug.LogWarning(
                $"[DissolveEffect] Material '{dissolveMaterial.name}' não tem propriedade de dissolve reconhecida.");
            FinishPresentation();
            return;
        }

        CollectTargets();
        if (_targets.Count == 0)
        {
            FinishPresentation();
            return;
        }

        _visualsHidden = false;
        _isPlaying = true;

        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.speed = 1f;
        }

        StopPresentationRoutine();
        _presentationRoutine = StartCoroutine(DeathPresentationRoutine());
    }

    private void StopPresentationRoutine()
    {
        if (_presentationRoutine == null)
            return;

        StopCoroutine(_presentationRoutine);
        _presentationRoutine = null;
        _isPlaying = false;
    }

    private IEnumerator DeathPresentationRoutine()
    {
        try
        {
            yield return null;

            if (waitForDeathAnimation)
                yield return WaitUntilDeathAnimationComplete();

            if (_animator != null)
                _animator.speed = 0f;

            RestoreOriginalMaterialsBeforeFade();
            ApplyDissolveMaterials();

            if (playSparkleParticles && _binding.Driver == DissolveMaterialBinding.Kind.DissolveSprite)
                DissolveSparkleVfx.Attach(transform, GetCombinedBounds(), duration, edgeColor);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float linearNormalized = Mathf.Clamp01(elapsed / duration);
                float shaderNormalized = EvaluateDissolveProgress(linearNormalized);

                for (int i = 0; i < _instanceMaterials.Count; i++)
                    _binding.SetAmount(_instanceMaterials[i], shaderNormalized);

                if (ShouldHideNow(linearNormalized, shaderNormalized))
                {
                    FinishPresentation();
                    yield break;
                }

                yield return null;
            }

            FinishPresentation();
        }
        finally
        {
            _isPlaying = false;
            _presentationRoutine = null;
        }
    }

    private void RestoreOriginalMaterialsBeforeFade()
    {
        ReleaseMaterials();
        CollectTargets();

        for (int i = 0; i < _targets.Count; i++)
        {
            SpriteRenderer renderer = _targets[i];
            if (renderer == null)
                continue;

            renderer.enabled = true;
            renderer.forceRenderingOff = false;

            if (i < _originalSharedMaterials.Count && _originalSharedMaterials[i] != null)
                renderer.sharedMaterial = _originalSharedMaterials[i];
        }
    }

    private static float EvaluateDissolveProgress(float elapsedNormalized)
    {
        return Mathf.Clamp01(elapsedNormalized);
    }

    private bool ShouldHideNow(float linearNormalized, float shaderNormalized)
    {
        if (_binding.Driver == DissolveMaterialBinding.Kind.Void1Sprite2D)
            return linearNormalized >= void1HideAtLinearTime;

        if (_binding.Driver == DissolveMaterialBinding.Kind.EnemyDeathFade)
            return shaderNormalized >= 0.995f;

        return shaderNormalized >= hideAtDissolveProgress;
    }

    private void FinishPresentation()
    {
        HideVisuals();
        _networkEnemyController?.NotifyDeathPresentationFinished();
    }

    private IEnumerator WaitUntilDeathAnimationComplete()
    {
        if (_animator == null)
        {
            yield return new WaitForSeconds(ResolveDeathClipLength());
            yield break;
        }

        float timeout = ResolveDeathClipLength() + 1.5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == _deathStateHash)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < timeout)
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != _deathStateHash)
            {
                elapsed += Time.deltaTime;
                yield return null;
                continue;
            }

            float clipLength = state.length > 0.05f
                ? state.length
                : ResolveDeathClipLength();

            float completionThreshold = Mathf.Clamp(dissolveStartNormalizedTime, 0.9f, 1f);
            if (state.normalizedTime >= completionThreshold)
                break;

            elapsed += Time.deltaTime;
            if (elapsed >= clipLength + 0.35f)
                break;

            yield return null;
        }
    }

    private float ResolveDeathClipLength()
    {
        if (animationBinder != null && animationBinder.Profile != null)
        {
            return AnimatorDeathTimingUtility.ResolveConfiguredClipLength(
                animationBinder.Profile,
                deathAnimLeadTimeFallback);
        }

        return deathAnimLeadTimeFallback;
    }

    private void CollectTargets()
    {
        _targets.Clear();
        _originalSharedMaterials.Clear();

        if (includeChildRenderers)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer != null && renderer.sprite != null)
                {
                    _targets.Add(renderer);
                    _originalSharedMaterials.Add(renderer.sharedMaterial);
                }
            }

            return;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            _targets.Add(spriteRenderer);
            _originalSharedMaterials.Add(spriteRenderer.sharedMaterial);
        }
    }

    private void DestroyInstanceMaterials()
    {
        for (int i = 0; i < _instanceMaterials.Count; i++)
        {
            if (_instanceMaterials[i] != null)
                Destroy(_instanceMaterials[i]);
        }

        _instanceMaterials.Clear();
    }

    private void ApplyDissolveMaterials()
    {
        DestroyInstanceMaterials();

        for (int i = 0; i < _targets.Count; i++)
        {
            SpriteRenderer target = _targets[i];
            Material instance = new Material(dissolveMaterial);

            if (target.sprite != null)
                instance.mainTexture = target.sprite.texture;

            _binding.ApplyInitial(instance, edgeColor, edgeIntensity, sparkleIntensity);
            target.material = instance;
            _instanceMaterials.Add(instance);
        }
    }

    private Bounds GetCombinedBounds()
    {
        Bounds bounds = _targets[0].bounds;
        for (int i = 1; i < _targets.Count; i++)
            bounds.Encapsulate(_targets[i].bounds);

        return bounds;
    }

    private void ReleaseMaterials()
    {
        DestroyInstanceMaterials();

        for (int i = 0; i < _targets.Count; i++)
        {
            SpriteRenderer target = _targets[i];
            if (target == null)
                continue;

            if (i < _originalSharedMaterials.Count && _originalSharedMaterials[i] != null)
                target.sharedMaterial = _originalSharedMaterials[i];
        }

        _originalSharedMaterials.Clear();
    }

    private void OnDestroy()
    {
        ReleaseMaterials();
    }
}
