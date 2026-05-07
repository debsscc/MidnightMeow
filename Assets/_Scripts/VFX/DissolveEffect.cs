using System.Collections;
using UnityEngine;

/// Aplica efeito de dissolve no SpriteRenderer quando o OnDied da HealthComponent é invocado.
///  HandleDeath() ao UnityEvent OnDied do HealthComponent no Inspector.
public class DissolveEffect : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Configurações")]
    [Tooltip("Material com o shader Custom/DissolveSprite")]
    [SerializeField] private Material dissolveMaterial;

    [Tooltip("Duração total do efeito de dissolve em segundos")]
    [SerializeField] private float duration = 1.5f;

    [Tooltip("Cor da borda do dissolve")]
    [SerializeField] private Color edgeColor = new Color(0f, 0.8f, 1f, 1f);

    private Material _instanceMaterial;
    private static readonly int _dissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int _edgeColorID = Shader.PropertyToID("_EdgeColor");

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Invocado pelo UnityEvent OnDied do HealthComponent
    public void HandleDeath()
    {
        if (dissolveMaterial == null)
        {
            Debug.LogWarning("[DissolveEffect] dissolveMaterial não atribuído.");
            return;
        }

        // Cria uma instância do material para não afetar outros objetos
        _instanceMaterial = new Material(dissolveMaterial);
        _instanceMaterial.SetColor(_edgeColorID, edgeColor);
        spriteRenderer.material = _instanceMaterial;

        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Curva suave: começa devagar, acelera no fim
            float dissolveValue = t * t;
            _instanceMaterial.SetFloat(_dissolveAmountID, dissolveValue);
            yield return null;
        }

        _instanceMaterial.SetFloat(_dissolveAmountID, 1f);
        // O GameObject já será destruído pelo HealthComponent/_destroyDelay
    }

    private void OnDestroy()
    {
        if (_instanceMaterial != null)
            Destroy(_instanceMaterial);
    }
}
