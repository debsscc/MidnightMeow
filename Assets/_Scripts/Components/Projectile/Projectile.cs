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
                Destroy(gameObject);
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
                Destroy(gameObject);
            }
        }
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