using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica Fira Sans em textos de gameplay criados em runtime ou prefabs.
/// </summary>
public static class GameplayUiFonts
{
    private const string ConfigResource = "GameplayUiFontConfig";
    private static GameplayUiFontConfig _config;

    public static void Apply(TMP_Text text)
    {
        if (text == null)
            return;

        TMP_FontAsset font = ResolveTmp();
        if (font == null)
            return;

        text.font = font;
    }

    public static void Apply(Text text)
    {
        if (text == null)
            return;

        Font font = ResolveLegacy();
        if (font == null)
            return;

        text.font = font;
    }

    private static TMP_FontAsset ResolveTmp()
    {
        GameplayUiFontConfig config = ResolveConfig();
        return config != null ? config.TmpFont : null;
    }

    private static Font ResolveLegacy()
    {
        GameplayUiFontConfig config = ResolveConfig();
        return config != null ? config.LegacyFont : null;
    }

    private static GameplayUiFontConfig ResolveConfig()
    {
        if (_config != null)
            return _config;

        _config = Resources.Load<GameplayUiFontConfig>(ConfigResource);
        return _config;
    }
}
