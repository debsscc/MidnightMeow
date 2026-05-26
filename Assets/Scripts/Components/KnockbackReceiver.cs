///* ----------------------------------------------------------------
// DESCRIÇÃO: Aplica knockback quando o GameObject sofre dano ou recebe força direta.
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
    private Coroutine _knockbackCoroutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _agent = GetComponent<NavMeshAgent>();
    }

    public void ApplyKnockback(GameObject instigator)
    {
        if (_rb == null || instigator == null) return;

        Vector2 direction = ((Vector2)transform.position - (Vector2)instigator.transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;

        ApplyKnockback(direction, force, duration);
    }

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
        if (_agent != null) _agent.isStopped = true;

        _rb.linearVelocity = direction * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        if (_agent != null) _agent.isStopped = false;
        IsKnockedBack = false;
        _knockbackCoroutine = null;
    }
}
