using UnityEngine;

/// <summary>
/// Banco de SFX do jogador (Nixie, Cora, etc.).
/// </summary>
[CreateAssetMenu(fileName = "PlayerAudioConfig", menuName = "MidnightMeow/Audio/Player Audio Config")]
public class PlayerAudioConfigSO : ScriptableObject
{
    [Header("Combate")]
    public AudioEventSO attack;
    public AudioEventSO abilityQ;
    public AudioEventSO abilityR;
    public AudioEventSO dash;
    public AudioEventSO damage;

    [Header("Estado")]
    public AudioEventSO heartbeat;
}
