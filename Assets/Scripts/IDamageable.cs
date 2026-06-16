using UnityEngine;

public interface IDamageable
{
    // Todo objeto que pode levar dano precisa implementar este método:
    void TakeDamage(float amount, GameObject instigator);

    /// <summary>Dano com tipagem; implementações devem tratar <see cref="DamageType.Generic"/> como fallback.</summary>
    void TakeDamage(float amount, GameObject instigator, DamageType damageType);
}