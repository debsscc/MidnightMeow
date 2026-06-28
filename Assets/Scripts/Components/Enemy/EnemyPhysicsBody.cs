using UnityEngine;

/// <summary>
/// Inimigos usam NavMesh para locomoção; colisão física com o player fica desligada via <see cref="CombatLayerCollision"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(50)]
public class EnemyPhysicsBody : MonoBehaviour
{
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.None;
    }

    private void Start()
    {
        ExcludePlayerLayerFromColliders();
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
        if (_rb == null || !_rb.simulated)
            return;

        Vector2 target = transform.position;
        if ((_rb.position - target).sqrMagnitude > 0.000001f)
            _rb.MovePosition(target);
    }
}
