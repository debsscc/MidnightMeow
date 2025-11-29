///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Define a classe base para habilidades que podem ser ativadas por personagens no jogo.
// ---------------------------------------------------------------- */

using UnityEngine;


public abstract class Ability : ScriptableObject
{
    public string abilityName;
    public float cooldown = 1f;

    public abstract void Activate(GameObject user);
}