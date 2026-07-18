///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Dados de uma dica individual do tutorial dinâmico na HUD.
// ---------------------------------------------------------------- */

using UnityEngine;

/// <summary>
/// Uma dica de tutorial: texto exibido + gatilho que a completa.
/// </summary>
[CreateAssetMenu(fileName = "TutorialTip", menuName = "MidnightMeow/Tutorial/Tip")]
public class TutorialTipSO : ScriptableObject
{
    [Tooltip("Texto em português (locale padrão do projeto).")]
    [TextArea(2, 4)]
    [SerializeField] private string tipTextPt = "Se movimente usando WASD";

    [Tooltip("Texto em inglês (quando o locale ativo não for pt).")]
    [TextArea(2, 4)]
    [SerializeField] private string tipTextEn = "Move using WASD";

    [Tooltip("Ação que completa esta dica e avança a sequência.")]
    [SerializeField] private TutorialTipTrigger trigger = TutorialTipTrigger.Move;

    /// <summary>Texto localizado para a HUD.</summary>
    public string ResolvedTipText =>
        LocaleText.IsPortuguese()
            ? tipTextPt
            : (string.IsNullOrWhiteSpace(tipTextEn) ? tipTextPt : tipTextEn);

    public TutorialTipTrigger Trigger => trigger;
}
