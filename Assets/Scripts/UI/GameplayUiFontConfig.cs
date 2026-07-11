using TMPro;
using UnityEngine;

/// <summary>
/// Fonte padrão da UI de gameplay (Fira Sans). Inknut fica para menu/títulos.
/// </summary>
[CreateAssetMenu(fileName = "GameplayUiFontConfig", menuName = "MidnightMeow/UI/Gameplay UI Font Config")]
public class GameplayUiFontConfig : ScriptableObject
{
    [SerializeField] private TMP_FontAsset tmpFont;
    [SerializeField] private Font legacyFont;

    public TMP_FontAsset TmpFont => tmpFont;
    public Font LegacyFont => legacyFont;
}
