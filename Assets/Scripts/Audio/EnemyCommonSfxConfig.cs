using UnityEngine;

/// <summary>
/// SFX compartilhados dos ratos comuns (ataque, dano, morte).
/// </summary>
[CreateAssetMenu(fileName = "EnemyCommonSfxConfig", menuName = "MidnightMeow/Audio/Enemy Common SFX Config")]
public class EnemyCommonSfxConfig : ScriptableObject
{
    [Header("Clips (ratos comuns)")]
    public AudioClip[] attackClips;
    public AudioClip damageClip;
    public AudioClip deathClip;

    [Header("Variação")]
    [Range(0.1f, 2f)]
    public float pitchMin = 0.9f;

    [Range(0.1f, 2f)]
    public float pitchMax = 1.12f;

    [Range(0f, 1f)]
    public float attackVolume = 0.7f;

    [Range(0f, 1f)]
    public float damageVolume = 0.65f;

    [Range(0f, 1f)]
    public float deathVolume = 0.75f;

    public float SamplePitch() => Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax));

    public AudioClip PickAttackClip()
    {
        if (attackClips == null || attackClips.Length == 0)
            return null;

        return attackClips[Random.Range(0, attackClips.Length)];
    }
}
