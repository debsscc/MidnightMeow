using System.Collections;
using UnityEngine;

/// <summary>
/// Invulnerabilidade breve após dano e passagem temporária pela layer Enemy.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class PlayerDamageImmunity : MonoBehaviour
{
    [SerializeField] private float immunityDuration = 0.85f;

    public float ImmunityDuration => immunityDuration;
    [SerializeField] private LayerMask enemyLayers;

    private HealthComponent _health;
    private bool _isImmune;
    private Coroutine _immunityRoutine;
    private int _playerLayer;

    public bool IsImmune => _isImmune;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _playerLayer = gameObject.layer;

        if (enemyLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                enemyLayers = 1 << enemyLayer;
        }
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
        SetEnemyCollisionIgnored(true);
        yield return new WaitForSeconds(immunityDuration);
        SetEnemyCollisionIgnored(false);
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

        SetEnemyCollisionIgnored(false);
        _isImmune = false;
    }

    private void SetEnemyCollisionIgnored(bool ignore)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((enemyLayers.value & (1 << i)) == 0) continue;
            Physics2D.IgnoreLayerCollision(_playerLayer, i, ignore);
        }
    }
}
