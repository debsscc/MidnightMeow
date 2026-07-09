using UnityEngine;

/// <summary>
/// Trilhas por grupo de cena (menu, lobby, fases de gameplay).
/// </summary>
[CreateAssetMenu(fileName = "MusicConfig", menuName = "MidnightMeow/Audio/Music Config")]
public class MusicConfigSO : ScriptableObject
{
    [Header("Menu")]
    public AudioClip menu;

    [Header("Lobby")]
    public AudioClip lobby;

    [Header("Gameplay")]
    public AudioClip phase1;
    public AudioClip phase2;
    public AudioClip phase3;
}
