using UnityEngine;

public interface IDamageable
{
    // Todo objeto que pode levar dano precisa implementar este método:
    void TakeDamage(float amount, GameObject instigator);
}