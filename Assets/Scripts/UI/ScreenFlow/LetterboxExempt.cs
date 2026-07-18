using UnityEngine;

///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Marca Canvas que devem ocupar a tela inteira (ex.: fade de transição),
// ignorando o remapeamento Overlay → Screen Space Camera do letterbox 16:9.
// ---------------------------------------------------------------- */

/// <summary>
/// Coloque no mesmo GameObject do <see cref="Canvas"/> (ou em um ancestral) para
/// manter <see cref="RenderMode.ScreenSpaceOverlay"/> em tela cheia.
/// </summary>
[DisallowMultipleComponent]
public sealed class LetterboxExempt : MonoBehaviour
{
}
