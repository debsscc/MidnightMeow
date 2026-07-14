using UnityEngine;

/// <summary>
/// Sorting 2D por “pés” (Y do collider), igual player/inimigo.
/// Use em props estáticos (flor, pedra, árvore) pra o personagem passar na frente/atrás.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class StaticSpriteYSort : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Collider2D sortingCollider;
    [SerializeField] private int sortingOrderOffset = 5000;
    [SerializeField] private int sortingPrecision = 100;
    [SerializeField] private float sortingReferenceYOffset;
    [Tooltip("Props parados: basta atualizar no Start. Liga se o objeto se mover.")]
    [SerializeField] private bool updateEveryFrame;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (sortingCollider == null)
            sortingCollider = ResolveSolidCollider();
    }

    private void Start() => RefreshSortingOrder();

    private void LateUpdate()
    {
        if (updateEveryFrame)
            RefreshSortingOrder();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
        if (sortingCollider == null)
            sortingCollider = ResolveSolidCollider();

        if (!Application.isPlaying)
            RefreshSortingOrder();
    }
#endif

    public void RefreshSortingOrder()
    {
        if (targetRenderer == null)
            return;

        float referenceY = ResolveReferenceY();
        targetRenderer.sortingOrder =
            sortingOrderOffset - Mathf.RoundToInt((referenceY + sortingReferenceYOffset) * sortingPrecision);
    }

    private float ResolveReferenceY()
    {
        if (sortingCollider != null && sortingCollider.enabled)
            return sortingCollider.bounds.min.y;

        if (targetRenderer != null)
            return targetRenderer.bounds.min.y;

        return transform.position.y;
    }

    private Collider2D ResolveSolidCollider()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
                return colliders[i];
        }

        return GetComponent<Collider2D>();
    }
}
