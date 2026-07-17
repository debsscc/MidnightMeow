///* ----------------------------------------------------------------
// DESCRIÇÃO: Aplica knockback via Rigidbody2D (velocidade/impulso). Nunca move transform.position.
// Em inimigos: ativa EnemyPhysicsBody (Dynamic) e pausa EnemyMovement / NavMeshAgent.
// ---------------------------------------------------------------- */

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class KnockbackReceiver : MonoBehaviour
{
    [Header("Knockback padrão (dano genérico)")]
    [SerializeField] private float force = 8f;
    [SerializeField] private float duration = 0.15f;

    public bool IsKnockedBack { get; private set; }

    private Rigidbody2D _rb;
    private NavMeshAgent _agent;
    private EnemyPhysicsBody _physicsBody;
    private EnemyMovement _movement;
    private Coroutine _knockbackCoroutine;
    private bool _agentHadUpdatePosition;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _agent = GetComponent<NavMeshAgent>();
        _physicsBody = GetComponent<EnemyPhysicsBody>();
        _movement = GetComponent<EnemyMovement>();
    }

    public void ApplyKnockback(GameObject instigator)
    {
        if (_rb == null || instigator == null) return;

        Vector2 direction = ((Vector2)transform.position - (Vector2)instigator.transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;

        ApplyKnockback(direction, force, duration);
    }

    /// <summary>
    /// Aplica knockback. <paramref name="knockbackForce"/> é velocidade inicial (unidades/s),
    /// não distância — use distance/duration quando vier de dados de distância.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float knockbackForce, float knockbackDuration)
    {
        if (_rb == null || knockbackForce <= 0f || knockbackDuration <= 0f) return;

        Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

        if (_knockbackCoroutine != null)
            StopCoroutine(_knockbackCoroutine);

        _knockbackCoroutine = StartCoroutine(KnockbackRoutine(normalized, knockbackForce, knockbackDuration));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float knockbackForce, float knockbackDuration)
    {
        IsKnockedBack = true;
        LockLocomotion();

        // Inimigos: Dynamic + Continuous para colidir com paredes. Players já são Dynamic.
        if (_physicsBody != null)
            _physicsBody.BeginExternalPhysics();

        // Impulso único — a física Continuous impede atravessar colisores estáticos.
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(direction * knockbackForce * _rb.mass, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        if (_physicsBody != null)
            _physicsBody.EndExternalPhysics();

        UnlockLocomotion();
        IsKnockedBack = false;
        _knockbackCoroutine = null;
    }

    private void LockLocomotion()
    {
        if (_movement != null)
            _movement.enabled = false;

        if (_agent == null)
            return;

        _agentHadUpdatePosition = _agent.updatePosition;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        _agent.ResetPath();
        _agent.updatePosition = false;
    }

    private void UnlockLocomotion()
    {
        if (_agent != null)
        {
            if (_agent.enabled && _agent.isOnNavMesh)
                _agent.Warp(transform.position);

            _agent.updatePosition = _agentHadUpdatePosition;
            _agent.isStopped = false;
        }

        if (_movement != null)
            _movement.enabled = true;
    }

    private void OnDisable()
    {
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
        }

        if (_physicsBody != null && _physicsBody.IsExternalPhysicsActive)
            _physicsBody.EndExternalPhysics();

        if (IsKnockedBack)
        {
            UnlockLocomotion();
            IsKnockedBack = false;
        }
    }
}
