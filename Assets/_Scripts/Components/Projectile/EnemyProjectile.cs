///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que controla o projétil inimigo.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private EnemyProjectileStats stats;

    private Rigidbody2D _rb;
    private float _lifetimeTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _rb.linearVelocity = transform.up * stats.moveSpeed;
        _lifetimeTimer = stats.lifetime;
    }

    private void Update()
    {
        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall") || 
            other.gameObject.layer == LayerMask.NameToLayer("Structure"))
        {
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (other.TryGetComponent<IDamageable>(out IDamageable target))
            {
                target.TakeDamage(stats.damage, this.gameObject);
            }
            Destroy(gameObject);
        }
    }
}
