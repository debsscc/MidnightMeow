///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Ordem das dicas do tutorial (lista de TutorialTipSO).
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>
/// Sequência ordenada de dicas. O <see cref="TutorialManager"/> consome este asset.
/// </summary>
[CreateAssetMenu(fileName = "TutorialSequence", menuName = "MidnightMeow/Tutorial/Sequence")]
public class TutorialSequenceSO : ScriptableObject
{
    [Tooltip("Dicas na ordem de exibição. Entradas nulas são ignoradas em runtime.")]
    [SerializeField] private TutorialTipSO[] tips;

    public int TipCount => tips != null ? tips.Length : 0;

    /// <summary>
    /// Obtém a dica no índice, ou null se inválido / slot vazio.
    /// </summary>
    public TutorialTipSO GetTip(int index)
    {
        if (tips == null || index < 0 || index >= tips.Length)
            return null;

        return tips[index];
    }
}
