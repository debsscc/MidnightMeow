using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Apresentação de spawn do inimigo: baforada de poeira no ponto de surgimento + materialização
/// (dissolve reverso) do sprite. Espelha o death de <see cref="DissolveEffect"/> e roda local em
/// cada máquina (solo e multiplayer), sem depender de sincronização de rede.
/// </summary>
[DisallowMultipleComponent]
public class EnemySpawnPresentation : MonoBehaviour
{
    [Header("Materialização (dissolve reverso)")]
    [Tooltip("Material de dissolve. Se vazio, herda o do DissolveEffect do inimigo.")]
    [SerializeField] private Material materializeMaterial;
    [SerializeField] private bool includeChildRenderers = true;
    [SerializeField] private float materializeDuration = 0.35f;
    [SerializeField] private Color edgeColor = new Color(1f, 0.85f, 0.55f, 1f);
    [SerializeField] private float edgeIntensity = 2.5f;

    [Header("Baforada de poeira")]
    [SerializeField] private bool playDustPuff = true;
    [SerializeField] private float dustRadius = 0.35f;
    [SerializeField] private float dustDuration = 0.6f;
    [SerializeField] private Color dustColor = new Color(0.62f, 0.55f, 0.45f, 1f);

    private readonly List<SpriteRenderer> _targets = new List<SpriteRenderer>(4);
    private readonly List<Material> _instanceMaterials = new List<Material>(4);
    private readonly List<Material> _originalSharedMaterials = new List<Material>(4);

    private DissolveEffect _dissolveEffect;
    private bool _played;
    private Coroutine _routine;

    private void Awake()
    {
        _dissolveEffect = GetComponent<DissolveEffect>();
        if (materializeMaterial == null && _dissolveEffect != null)
            materializeMaterial = _dissolveEffect.DissolveTemplate;
    }

    private void Start()
    {
        Play();
    }

    public void Play()
    {
        if (_played)
            return;

        _played = true;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(MaterializeRoutine());
    }

    private IEnumerator MaterializeRoutine()
    {
        // Espera o NetworkTransform aplicar a posição de spawn no cliente antes do puff.
        yield return null;

        if (playDustPuff)
        {
            ResolveSortingFromRenderer(out int sortingLayerId, out int sortingOrder);
            EnemySpawnVfx.Play(
                transform.position,
                dustRadius,
                dustColor,
                dustDuration,
                sortingLayerId,
                sortingOrder);
        }

        DissolveMaterialBinding binding = DissolveMaterialBinding.FromMaterial(materializeMaterial);
        if (materializeMaterial == null || binding.Driver == DissolveMaterialBinding.Kind.Unsupported)
        {
            _routine = null;
            yield break;
        }

        CollectTargets();
        if (_targets.Count == 0)
        {
            _routine = null;
            yield break;
        }

        ApplyMaterializeMaterials(binding);

        float elapsed = 0f;
        while (elapsed < materializeDuration)
        {
            // Aborta se o death começar (não brigar pelos materiais).
            if (_dissolveEffect != null && _dissolveEffect.IsPlaying)
                break;

            elapsed += Time.deltaTime;
            float linear = Mathf.Clamp01(elapsed / materializeDuration);
            // Vai de "sumido" (1) para "intacto" (0): materializa de fora pra dentro.
            float amount = 1f - (linear * linear);
            for (int i = 0; i < _instanceMaterials.Count; i++)
                binding.SetAmount(_instanceMaterials[i], amount);

            yield return null;
        }

        for (int i = 0; i < _instanceMaterials.Count; i++)
            binding.SetAmount(_instanceMaterials[i], 0f);

        RestoreMaterials();
        _routine = null;
    }

    private void ResolveSortingFromRenderer(out int sortingLayerId, out int sortingOrder)
    {
        SpriteRenderer reference = GetComponent<SpriteRenderer>();
        if (reference == null)
            reference = GetComponentInChildren<SpriteRenderer>(true);

        if (reference != null)
        {
            sortingLayerId = reference.sortingLayerID;
            // Poeira logo à frente do inimigo.
            sortingOrder = reference.sortingOrder + 1;
            return;
        }

        sortingLayerId = 0;
        sortingOrder = 100;
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

        SpriteRenderer single = GetComponent<SpriteRenderer>();
        if (single != null)
        {
            _targets.Add(single);
            _originalSharedMaterials.Add(single.sharedMaterial);
        }
    }

    private void ApplyMaterializeMaterials(DissolveMaterialBinding binding)
    {
        DestroyInstanceMaterials();

        for (int i = 0; i < _targets.Count; i++)
        {
            SpriteRenderer target = _targets[i];
            Material instance = new Material(materializeMaterial);

            if (target.sprite != null)
                instance.mainTexture = target.sprite.texture;

            binding.ApplyInitial(instance, edgeColor, edgeIntensity, edgeIntensity);
            binding.SetAmount(instance, 1f);
            target.material = instance;
            _instanceMaterials.Add(instance);
        }
    }

    private void RestoreMaterials()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            SpriteRenderer target = _targets[i];
            if (target == null)
                continue;

            if (i < _originalSharedMaterials.Count && _originalSharedMaterials[i] != null)
                target.sharedMaterial = _originalSharedMaterials[i];
        }

        DestroyInstanceMaterials();
        _originalSharedMaterials.Clear();
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

    private void OnDestroy()
    {
        RestoreMaterials();
    }
}
