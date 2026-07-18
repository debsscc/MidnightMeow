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
    [Tooltip("Texto em português (locale padrão do projeto). Sem o contador — ele é anexado em runtime se RequiredCount > 1.")]
    [TextArea(2, 4)]
    [SerializeField] private string tipTextPt = "Rápido! Movimente-se usando WASD";

    [Tooltip("Texto em inglês (quando o locale ativo não for pt).")]
    [TextArea(2, 4)]
    [SerializeField] private string tipTextEn = "Quick! Move using WASD";

    [Tooltip("Ação que completa esta dica e avança a sequência.")]
    [SerializeField] private TutorialTipTrigger trigger = TutorialTipTrigger.Move;

    [Tooltip("Quantas vezes o gatilho precisa ocorrer (ex. 3 kills). UseAbility ignora e exige Q+R.")]
    [SerializeField] private int requiredCount = 1;

    /// <summary>Texto localizado base (sem contador).</summary>
    public string ResolvedTipText =>
        LocaleText.IsPortuguese()
            ? tipTextPt
            : (string.IsNullOrWhiteSpace(tipTextEn) ? tipTextPt : tipTextEn);

    public TutorialTipTrigger Trigger => trigger;

    /// <summary>Meta de progresso (≥ 1). UseAbility trata Q+R à parte no Manager.</summary>
    public int RequiredCount => Mathf.Max(1, requiredCount);

    /// <summary>Texto pronto para a HUD, com contador se aplicável.</summary>
    public string FormatDisplayText(int currentProgress) =>
        TutorialTipDisplayFormatter.Format(ResolvedTipText, currentProgress, RequiredCount);
}
