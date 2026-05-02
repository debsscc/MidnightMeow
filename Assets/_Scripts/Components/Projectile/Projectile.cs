///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla o comportamento de um projétil que pode quicar em paredes e ser coletado como munição.
// ---------------------------------------------------------------- */

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    // Estatísticas do projétil
    [SerializeField] private ProjectileStats stats;
    // Multiplicador de dano aplicado a essa instância (permite upgrades)
    private float _damageMultiplier = 1f;

    private Rigidbody2D _rb;
    private int _currentBounces = 0;
    private int _maxBounces;

    // Estado para controlar se pode ser pego
    private bool _canBeCollected = false;

    [Header("Animation")]
    [Tooltip("Animator do GameObject do projétil (opcional). Deve ter os estados Spawn, Flying e Hit.")]
    [SerializeField] private Animator _projectileAnimator;
    [Tooltip("Duração da animação de Hit em segundos. O objeto é destruído após esse tempo.")]
    [SerializeField] private float _hitAnimDuration = 0.3f;
    [Tooltip("Se verdadeiro, reproduz a animação de Hit quando o projétil expirar por distância (sem acertar nada).")]
    [SerializeField] private bool _playHitOnExpire = false;

    private bool _hasHit = false;
    private static readonly int _hashOnHit = Animator.StringToHash("OnHit");

    private enum ProjectileState {  Fired, Seeking}
    private ProjectileState _currentState = ProjectileState.Fired;

    private Transform _seekTarget;
    private float _seekSpeed;
    private Vector2 _travelDirection;
    private bool _hasTravelDirection;
    private Vector2 _spawnPosition;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _maxBounces = stats.maxBounces;
    }

    private void Start()
    {
        _spawnPosition = transform.position;
        Vector2 initialDirection = _hasTravelDirection ? _travelDirection : (Vector2)transform.up;
        SetTravelDirection(initialDirection, stats.moveSpeed);
    }

    private void Update()
    {
        if (stats.maxDistance > 0 && _currentState != ProjectileState.Seeking &&
            Vector2.Distance(_spawnPosition, transform.position) >= stats.maxDistance)
        {
            if (_playHitOnExpire)
                TriggerHitAndDestroy();
            else
                Destroy(gameObject);
            return;
        }

        if (_currentState == ProjectileState.Seeking && _seekTarget != null)
        {
            Vector2 direction = (_seekTarget.position - transform.position).normalized;
            _rb.linearVelocity = direction * _seekSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // Usado para ricochete
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_currentState == ProjectileState.Seeking)  return;
        // Verifica se colidiu com uma Parede
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Structure"))
        {
            _currentBounces++;
            if (!stats.infinityBounces && _currentBounces >= _maxBounces){
                TriggerHitAndDestroy();
            }
            // Após o primeiro quique, o projétil vira munição, talvez devemos considerar outra lógica usando delay
            if (!_canBeCollected && stats.collectable)
            {
                _canBeCollected = true;
            }
        }
    }
    // Usado para coletar a munição
    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (_canBeCollected && stats.collectable)
            {
                GameEvents.InvokeAmmoCollected();
                Destroy(gameObject);
            }
            return;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Structure"))
        {
            return;
        }

        Debug.Log("Projectile hit: " + other.gameObject.name);
        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            _currentBounces++;
            target.TakeDamage(stats.damage * _damageMultiplier, this.gameObject);
            if (!stats.infinityBounces && _currentBounces >= _maxBounces){
                TriggerHitAndDestroy();
            }
        }
    }

    // Para o projétil, toca a animação de Hit e agenda a destruição
    private void TriggerHitAndDestroy()
    {
        if (_hasHit) return;
        _hasHit = true;

        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;
        // Espelha horizontalmente se o projétil estava indo para a direita
        float yFlip = _travelDirection.x >= 0f ? 180f : 0f;
        transform.rotation = Quaternion.Euler(0f, yFlip, 0f);

        // Desabilita os colliders para evitar múltiplas detecções
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (_projectileAnimator != null)
            _projectileAnimator.SetTrigger(_hashOnHit);

        Destroy(gameObject, _hitAnimDuration);
    }

    public void ActivatePull(Transform target, float speed)
    {
        _currentState = ProjectileState.Seeking;
        _seekTarget = target;
        _seekSpeed = speed;
        _canBeCollected = true;
    }

    public void ActivateReflect(Vector2 newDirection, float speedMultiplier)
    {
        float newSpeed = stats.moveSpeed * speedMultiplier;
        SetTravelDirection(newDirection, newSpeed);
    }

    public void InitializeDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = Vector2.up;
        }

        _travelDirection = direction.normalized;
        _hasTravelDirection = true;

        if (_rb != null)
        {
            SetTravelDirection(_travelDirection, stats.moveSpeed);
        }
    }

    public void AddBonusBounces(int bonusBounces)
    {
        _maxBounces += bonusBounces;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = Mathf.Max(0f, multiplier);
    }

    private void SetTravelDirection(Vector2 direction, float speed)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude <= Mathf.Epsilon ? Vector2.up : direction.normalized;

        _travelDirection = normalizedDirection;
        _hasTravelDirection = true;
        _rb.linearVelocity = normalizedDirection * speed;

        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}