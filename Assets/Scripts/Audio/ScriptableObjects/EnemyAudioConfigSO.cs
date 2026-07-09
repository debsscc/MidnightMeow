using UnityEngine;

/// <summary>
/// Banco de SFX de inimigos (ratos, Rei Rato, etc.).
/// </summary>
[CreateAssetMenu(fileName = "EnemyAudioConfig", menuName = "MidnightMeow/Audio/Enemy Audio Config")]
public class EnemyAudioConfigSO : ScriptableObject
{
    [Header("Combate")]
    public AudioEventSO attack;
    public AudioEventSO damage;
    public AudioEventSO death;

    [Header("Rei Rato (futuro)")]
    public AudioEventSO meleeAttack;
    public AudioEventSO rangedAttack;
}
