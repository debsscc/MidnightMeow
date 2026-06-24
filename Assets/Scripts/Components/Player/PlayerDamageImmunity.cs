using System.Collections;
using UnityEngine;

/// <summary>
/// Invulnerabilidade breve após dano (sem alterar colisão física com inimigos).
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class PlayerDamageImmunity : MonoBehaviour
{
    [SerializeField] private float immunityDuration = 0.85f;

    public float ImmunityDuration => immunityDuration;

    private HealthComponent _health;
    private bool _isImmune;
    private Coroutine _immunityRoutine;

    public bool IsImmune => _isImmune;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        CombatLayerCollision.Apply();
    }

    private void OnEnable()
    {
        _health.OnTakeDamage.AddListener(HandleDamaged);
    }

    private void OnDisable()
    {
        _health.OnTakeDamage.RemoveListener(HandleDamaged);
        EndImmunityImmediate();
    }

    private void HandleDamaged()
    {
        _health.SetInvulnerableFor(immunityDuration);

        if (_immunityRoutine != null)
            StopCoroutine(_immunityRoutine);

        _immunityRoutine = StartCoroutine(ImmunityRoutine());
    }

    private IEnumerator ImmunityRoutine()
    {
        _isImmune = true;
        yield return new WaitForSeconds(immunityDuration);
        _isImmune = false;
        _immunityRoutine = null;
    }

    private void EndImmunityImmediate()
    {
        if (_immunityRoutine != null)
        {
            StopCoroutine(_immunityRoutine);
            _immunityRoutine = null;
        }

        _isImmune = false;
    }
}
