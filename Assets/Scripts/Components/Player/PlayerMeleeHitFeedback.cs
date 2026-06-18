using UnityEngine;

/// <summary>
/// Juice local do melee: burst, blink reforçado e shake leve no acerto.
/// </summary>
[DisallowMultipleComponent]
public class PlayerMeleeHitFeedback : MonoBehaviour
{
    [SerializeField] private PlayerMeleeCombat meleeCombat;
    [SerializeField] private float hitBlinkPulse = 1f;

    private void Awake()
    {
        if (meleeCombat == null)
            meleeCombat = GetComponent<PlayerMeleeCombat>();
    }

    private void OnEnable()
    {
        if (meleeCombat != null)
            meleeCombat.OnMeleeHitsConfirmed += HandleMeleeHitsConfirmed;
    }

    private void OnDisable()
    {
        if (meleeCombat != null)
            meleeCombat.OnMeleeHitsConfirmed -= HandleMeleeHitsConfirmed;
    }

    private void HandleMeleeHitsConfirmed(MeleeHitResult result)
    {
        if (result.HitCount <= 0)
            return;

        float pulse = meleeCombat != null && meleeCombat.CombatStats != null
            ? meleeCombat.CombatStats.hitBlinkPulse
            : hitBlinkPulse;

        PlayerCameraFeedback.ShakeOnMeleeHit(result.HitCount);

        for (int i = 0; i < result.HitPoints.Length; i++)
            MeleeHitBurstVfx.Play(result.HitPoints[i]);

        for (int i = 0; i < result.Targets.Length; i++)
        {
            GameObject target = result.Targets[i];
            if (target == null)
                continue;

            if (target.TryGetComponent<SpriteBlink>(out var blink))
                blink.Pulse(pulse);
        }
    }
}
