///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Identifica qual ação do jogador completa uma dica do tutorial.
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>
/// Gatilho que avança a dica atual do tutorial quando o evento correspondente
/// é disparado em <see cref="GameEvents"/>.
/// </summary>
public enum TutorialTipTrigger
{
    [Tooltip("Jogador moveu (WASD / stick).")]
    Move = 0,

    [Tooltip("Jogador disparou ou atacou com o botão primário.")]
    Shoot = 1,

    [Tooltip("Um buraco de spawn foi selado com sucesso.")]
    SealHole = 2,

    [Tooltip("Jogador usou as habilidades Q e R (ambas).")]
    UseAbility = 3,

    [Tooltip("Jogador usou o dash (Shift).")]
    Dash = 4,

    [Tooltip("Inimigos eliminados (requer requiredCount no Tip SO).")]
    KillEnemies = 5
}
