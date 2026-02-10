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

    private Rigidbody2D _rb;
    private int _currentBounces = 0;
    private int _maxBounces;

    // Estado para controlar se pode ser pego
    private bool _canBeCollected = false;

    private enum ProjectileState {  Fired, Seeking}
    private ProjectileState _currentState = ProjectileState.Fired;

    private Transform _seekTarget;
    private float _seekSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _maxBounces = stats.maxBounces;
    }

    private void Start()
    {
        // Aplica velocidade inicial
        _rb.linearVelocity = transform.up * stats.moveSpeed;
    }

    private void Update()
    {
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
            target.TakeDamage(stats.damage, this.gameObject);
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
        // Altera a direção do projétil
        float newSpeed = stats.moveSpeed * speedMultiplier;
        _rb.linearVelocity = newDirection.normalized * newSpeed;
        //_canBeCollected = false;
        // Ajusta a rotação para alinhar com a nova direção
        float angle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void AddBonusBounces(int bonusBounces)
    {
        _maxBounces += bonusBounces;
    }
}