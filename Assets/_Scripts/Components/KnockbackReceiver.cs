///* ----------------------------------------------------------------
// CRIADO EM: 17-04-2026
// DESCRIÇÃO: Aplica knockback quando o GameObject sofre dano.
//            Adicione este componente ao Player e/ou Inimigos.
//            HealthComponent.TakeDamage aciona automaticamente via TryGetComponent.
// ---------------------------------------------------------------- */

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class KnockbackReceiver : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("Força do knockback em unidades/segundo.")]
    [SerializeField] private float force = 8f;

    [Tooltip("Duração do knockback em segundos.")]
    [SerializeField] private float duration = 0.15f;

    public bool IsKnockedBack { get; private set; }

    private Rigidbody2D _rb;
    private NavMeshAgent _agent;

    private Coroutine _knockbackCoroutine;

    private void Awake()
    {
        _rb    = GetComponent<Rigidbody2D>();
        _agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>Chamado pelo HealthComponent ao sofrer dano. instigator é quem causou o dano.</summary>
    public void ApplyKnockback(GameObject instigator)
    {
        if (_rb == null || instigator == null) return;

        Vector2 direction = ((Vector2)transform.position - (Vector2)instigator.transform.position).normalized;

        // Reinicia knockback anterior se ainda estiver rodando
        if (_knockbackCoroutine != null)
            StopCoroutine(_knockbackCoroutine);

        _knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        IsKnockedBack = true;
        if (_agent != null) _agent.isStopped = true;

        _rb.linearVelocity = direction * force;

        yield return new WaitForSeconds(duration);

        if (_agent != null) _agent.isStopped = false;
        IsKnockedBack = false;
        _knockbackCoroutine = null;
    }
}
