using TMPro;
using UnityEngine;

/// <summary>
/// Visual dos créditos: tipografia de título vs corpo e fundo do botão Fechar.
/// </summary>
[CreateAssetMenu(fileName = "CreditsVisualConfig", menuName = "MidnightMeow/UI/Credits Visual Config")]
public class CreditsVisualConfig : ScriptableObject
{
    [Header("Tipografia")]
    [Tooltip("Fonte dos títulos (trechos com <size=…%> no CreditsBody.txt).")]
    [SerializeField] private TMP_FontAsset titleFont;

    [Tooltip("Fonte do restante do texto (nomes, cargos, etc.).")]
    [SerializeField] private TMP_FontAsset bodyFont;

    [Tooltip("Tamanho base do corpo. 0 = manter padrão do overlay (28).")]
    [SerializeField] private float bodyFontSize = 28f;

    [Header("Botão Fechar")]
    [SerializeField] private Sprite closeButtonSprite;

    [Tooltip("Usado quando não há sprite; com sprite, multiplica a cor da Image (branco = sprite puro).")]
    [SerializeField] private Color closeButtonColor = Color.white;

    public TMP_FontAsset TitleFont => titleFont;
    public TMP_FontAsset BodyFont => bodyFont;
    public float BodyFontSize => bodyFontSize;
    public Sprite CloseButtonSprite => closeButtonSprite;
    public Color CloseButtonColor => closeButtonColor;
}
