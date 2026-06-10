///* ----------------------------------------------------------------
// CRIADO EM: 10-02-2026
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Componente que controla o projétil inimigo.
// ---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private EnemyProjectileStats stats;

    private Rigidbody2D _rb;
    private float _lifetimeTimer;
    private Vector2 _spawnPosition;

    [Header("Animation")]
    [Tooltip("Animator do GameObject do projétil (opcional). Deve ter os estados Spawn, Flying e Hit.")]
    [SerializeField] private Animator _projectileAnimator;
    [Tooltip("Duração da animação de Hit em segundos. O objeto é destruído após esse tempo.")]
    [SerializeField] private float _hitAnimDuration = 0.3f;
    [Tooltip("Se verdadeiro, reproduz a animação de Hit quando o projétil expirar por tempo ou distância.")]
    [SerializeField] private bool _playHitOnExpire = false;

    private bool _hasHit = false;
    private static readonly int _hashOnHit = Animator.StringToHash("OnHit");

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
            if (_playHitOnExpire)
                TriggerHitAndDestroy();
            else
                Destroy(gameObject);
            return;
        }

        if (stats.maxDistance > 0 && Vector2.Distance(_spawnPosition, transform.position) >= stats.maxDistance)
        {
            if (_playHitOnExpire)
                TriggerHitAndDestroy();
            else
                Destroy(gameObject);
        }
    }

    //Bloqueia fisicamente pela parede e destroi o projétil se cair na layer wall e structure
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Structure"))
        {
            TriggerHitAndDestroy();
        }
    }

    //Pra diferenciar do collision, o trigger detecta o player, aplica dano e destrói depois do projétil
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerCombatUtility.TryApplyDamage(other, stats.damage, gameObject);
            TriggerHitAndDestroy();
        }
    }

    // Para o projétil, toca a animação de Hit e agenda a destruição
    public void TriggerHitAndDestroy()
    {
        if (_hasHit) return;
        _hasHit = true;

        // Captura a direção antes de zerar a velocidade
        float yFlip = _rb.linearVelocity.x >= 0f ? 180f : 0f;
        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;
        // Espelha horizontalmente se o projétil estava indo para a direita
        transform.rotation = Quaternion.Euler(0f, yFlip, 0f);

        // Desabilita os colliders para evitar múltiplas detecções
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (_projectileAnimator != null)
            _projectileAnimator.SetTrigger(_hashOnHit);

        if (TryGetComponent<NetworkEnemyProjectileController>(out var networkProjectile)
            && networkProjectile.IsSpawned
            && NetworkManager.Singleton != null
            && NetworkManager.Singleton.IsServer)
        {
            networkProjectile.DespawnAfterHit(_hitAnimDuration);
            return;
        }

        Destroy(gameObject, _hitAnimDuration);
    }
}
