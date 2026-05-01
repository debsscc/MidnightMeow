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
    private Vector2 _spawnPosition;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _rb.linearVelocity = transform.up * stats.moveSpeed;
        _lifetimeTimer = stats.lifetime;
        _spawnPosition = transform.position;
    }

    private void Update()
    {
        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (stats.maxDistance > 0 && Vector2.Distance(_spawnPosition, transform.position) >= stats.maxDistance)
        {
            Destroy(gameObject);
        }
    }

    //Bloqueia fisicamente pela parede e destroi o projétil se cair na layer wall e structure
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Structure"))
        {
            Destroy(gameObject);
        }
    }

    //Pra diferenciar do collision, o trigger detecta o player, aplica dano e destrói depois do projétil
    private void OnTriggerEnter2D(Collider2D other)
    {
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
