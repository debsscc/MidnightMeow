using TMPro;
using UnityEngine;

/// <summary>
/// Visual dos créditos: tipografia de título vs corpo, fundo do overlay e botão Fechar.
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

    [Tooltip("Cor do texto dos créditos (títulos e corpo).")]
    [SerializeField] private Color bodyTextColor = Color.black;

    [Tooltip("Largura da coluna de texto (0–1 da tela). Ex.: 0.38 = 38% centralizados.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float bodyWidthNormalized = 0.38f;

    [Header("Fundo")]
    [Tooltip("Sprite de fundo do overlay. Se vazio, usa cor sólida escura.")]
    [SerializeField] private Sprite backgroundSprite;

    [Tooltip("Material lit (ex.: Sprite Lit Default) para Light2D da ambiência Menu2 afetar o fundo.")]
    [SerializeField] private Material litBackgroundMaterial;

    [Header("Botão Fechar")]
    [SerializeField] private Sprite closeButtonSprite;

    [Tooltip("Usado quando não há sprite; com sprite, multiplica a cor da Image (branco = sprite puro).")]
    [SerializeField] private Color closeButtonColor = Color.white;

    public TMP_FontAsset TitleFont => titleFont;
    public TMP_FontAsset BodyFont => bodyFont;
    public float BodyFontSize => bodyFontSize;
    public Color BodyTextColor => bodyTextColor;
    public float BodyWidthNormalized => bodyWidthNormalized;
    public Sprite BackgroundSprite => backgroundSprite;
    public Material LitBackgroundMaterial => litBackgroundMaterial;
    public Sprite CloseButtonSprite => closeButtonSprite;
    public Color CloseButtonColor => closeButtonColor;
}
