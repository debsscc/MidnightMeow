using UnityEngine;

/// <summary>
/// Locomoção normal: NavMesh move o transform; Rigidbody2D fica Kinematic e espelha a posição.
/// Durante knockback: troca temporariamente para Dynamic + Continuous para a física bloquear paredes
/// (evita tunneling por teleporte via transform).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(50)]
public class EnemyPhysicsBody : MonoBehaviour
{
    [Header("Knockback (física externa)")]
    [Tooltip("Linear Drag enquanto o inimigo está em knockback (Dynamic). Baixo o bastante para percorrer a distância; alto o bastante para não deslizar após bater na parede.")]
    [SerializeField] private float knockbackLinearDamping = 3f;

    private Rigidbody2D _rb;
    private bool _externalPhysicsActive;
    private float _cachedLinearDamping;

    /// <summary>True enquanto o Rigidbody está Dynamic sob impulso de knockback.</summary>
    public bool IsExternalPhysicsActive => _externalPhysicsActive;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        ApplyLocomotionRigidbodyDefaults();
    }

    private void Start()
    {
        ExcludePlayerLayerFromColliders();
    }

    /// <summary>
    /// Ativa Rigidbody Dynamic + Continuous para o motor de física resolver colisões com paredes.
    /// Chamar antes de ApplyForce / alterar linearVelocity no knockback.
    /// </summary>
    public void BeginExternalPhysics()
    {
        if (_rb == null)
            return;

        _externalPhysicsActive = true;
        _cachedLinearDamping = _rb.linearDamping;

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.linearDamping = knockbackLinearDamping;
    }

    /// <summary>
    /// Encerra o modo físico: zera velocidade e volta ao Kinematic usado com NavMesh.
    /// </summary>
    public void EndExternalPhysics()
    {
        if (_rb == null)
            return;

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.linearDamping = _cachedLinearDamping;
        ApplyLocomotionRigidbodyDefaults();
        _externalPhysicsActive = false;
    }

    private void ApplyLocomotionRigidbodyDefaults()
    {
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.None;
    }

    private void ExcludePlayerLayerFromColliders()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0)
            return;

        int playerMask = 1 << playerLayer;
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.isTrigger)
                continue;

            col.excludeLayers |= playerMask;
        }
    }

    private void FixedUpdate()
    {
        if (_rb == null || !_rb.simulated || _externalPhysicsActive)
            return;

        Vector2 target = transform.position;
        if ((_rb.position - target).sqrMagnitude > 0.000001f)
            _rb.MovePosition(target);
    }
}
