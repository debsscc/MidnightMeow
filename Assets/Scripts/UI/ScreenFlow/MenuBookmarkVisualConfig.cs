using UnityEngine;

/// <summary>
/// Sprites dos bookmarks do menu por contexto (hub vs. tela Continuar).
/// </summary>
[CreateAssetMenu(fileName = "MenuBookmarkVisualConfig", menuName = "MidnightMeow/UI/Menu Bookmark Visual Config")]
public class MenuBookmarkVisualConfig : ScriptableObject
{
    [Header("Hub → tucked (tela Continuar)")]
    [SerializeField] private Sprite newGameTuckedSprite;
    [SerializeField] private Sprite continueTuckedSprite;
    [SerializeField] private Sprite settingsTuckedSprite;
    [SerializeField] private Sprite creditsTuckedSprite;

    [Header("Sair vira Voltar na tela Continuar")]
    [SerializeField] private Sprite continueBackBookmarkSprite;

    public Sprite ContinueBackBookmarkSprite => continueBackBookmarkSprite;

    public Sprite GetTuckedSprite(string bookmarkId)
    {
        switch (bookmarkId)
        {
            case "NewGame": return newGameTuckedSprite;
            case "Continuar": return continueTuckedSprite;
            case "Settings": return settingsTuckedSprite;
            case "Credits": return creditsTuckedSprite;
            default: return null;
        }
    }
}
