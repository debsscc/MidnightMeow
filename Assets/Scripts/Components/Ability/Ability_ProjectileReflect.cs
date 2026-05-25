///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Habilidade que reflete projéteis próximos ao usuário em uma direção específica.
// ---------------------------------------------------------------- */

using UnityEngine;

[CreateAssetMenu(fileName = "Ability_Reflect", menuName = "Abilities/Projectile Reflect")]
public class Ability_ProjectileReflect : Ability
{
    [Header("Detecção")]
    public float reflectRadius = 4f;
    public LayerMask projectileLayer; 

    [Header("Efeitos")]
    public float speedMultiplier = 1.8f;

    public override void Activate(GameObject user)
    {
        PlayerAbilityHandler handler = user.GetComponent<PlayerAbilityHandler>();
        if (handler == null)
        {
            return;
        }

        
        Transform firePoint = handler.FirePoint;
        Vector2 reflectDirection = firePoint.up; 

        Collider2D[] hits = Physics2D.OverlapCircleAll(user.transform.position, reflectRadius, projectileLayer);
        if (hits.Length == 0) return;

        foreach (Collider2D hit in hits)
        {
            Projectile p = hit.GetComponent<Projectile>();
            if (p != null)
            {
                p.ActivateReflect(reflectDirection, speedMultiplier);
            }
        }
    }
}