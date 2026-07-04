using UnityEngine;

/// <summary>
/// Sprites de preview de contrato na tela Continuar do menu.
/// </summary>
[CreateAssetMenu(fileName = "MenuContractVisualConfig", menuName = "MidnightMeow/UI/Menu Contract Visual Config")]
public class MenuContractVisualConfig : ScriptableObject
{
    [Tooltip("Preview quando nenhum slot está selecionado (ex.: Contrato 1 em cinza).")]
    [SerializeField] private Sprite emptyPreviewSprite;

    [Tooltip("Contratos 1, 2 e 3 (índices 0–2).")]
    [SerializeField] private Sprite[] contractSprites = new Sprite[3];

    [SerializeField] private Color emptyPreviewTint = new Color(0.55f, 0.55f, 0.55f, 1f);

    public Color EmptyPreviewTint => emptyPreviewTint;

    public Sprite GetEmptyPreviewSprite()
    {
        if (emptyPreviewSprite != null)
            return emptyPreviewSprite;

        return contractSprites != null && contractSprites.Length > 0 ? contractSprites[0] : null;
    }

    public Sprite GetContractSprite(int contractIndex)
    {
        if (contractSprites == null || contractSprites.Length == 0)
            return null;

        if (contractIndex < 0)
            contractIndex = 0;

        if (contractIndex >= contractSprites.Length)
            return contractSprites[contractSprites.Length - 1];

        return contractSprites[contractIndex];
    }

    /// <summary>Índice 0–2 do contrato cujo preview deve aparecer para este save.</summary>
    public static int ResolvePreviewContractIndex(GameSaveData data)
    {
        if (data == null)
            return -1;

        if (data.selectedContractIndex >= 0)
            return Mathf.Clamp(data.selectedContractIndex, 0, 2);

        for (int i = 2; i >= 0; i--)
        {
            if (data.IsContractCompleted(i))
                return i;
        }

        return 0;
    }
}
