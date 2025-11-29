///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Habilidade que puxa todos os projéteis na cena em direção ao usuário.
// ---------------------------------------------------------------- */

using UnityEngine;

[CreateAssetMenu(fileName = "Ability_Pull", menuName = "Abilities/Projectile Pull")]
public class Ability_ProjectilePull : Ability
{
    public float pullSpeed = 25f;

    public override void Activate(GameObject user)
    {
        // Encontra todos os projéteis na cena.
        Projectile[] allProjectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);

        if (allProjectiles.Length == 0) return;

        Debug.Log($"Puxando {allProjectiles.Length} projéteis.");
        foreach (Projectile p in allProjectiles)
        {
            // Diz a cada projétil para buscar o jogador (usuário)
            p.ActivatePull(user.transform, pullSpeed);
        }
    }
}