using UnityEngine;

/// <summary>
/// SFX de interface e interações globais.
/// </summary>
[CreateAssetMenu(fileName = "UIAudioConfig", menuName = "MidnightMeow/Audio/UI Audio Config")]
public class UIAudioConfigSO : ScriptableObject
{
    [Header("Lobby — digitação do código")]
    public AudioEventSO lobbyKey1;
    public AudioEventSO lobbyKey2;
    public AudioEventSO lobbyKey3;

    [Header("Botões (hover / click)")]
    public AudioEventSO buttonHover;
    public AudioEventSO buttonClick;

    [Header("Interações")]
    public AudioEventSO interactE;
    public AudioEventSO reviveComplete;

    public AudioEventSO GetRandomLobbyKeyEvent()
    {
        int pick = Random.Range(0, 3);
        return pick switch
        {
            0 => lobbyKey1,
            1 => lobbyKey2,
            _ => lobbyKey3
        };
    }
}
