using UnityEngine;

/// <summary>
/// Aplica lentidão temporária ao movimento do inimigo.
/// </summary>
public class EnemySlowEffect : MonoBehaviour
{
    private float _slowMultiplier = 1f;
    private float _slowEndTime;

    public float SpeedMultiplier => Time.time < _slowEndTime ? _slowMultiplier : 1f;
    public bool IsSlowed => Time.time < _slowEndTime;

    public void ApplySlow(float speedMultiplier, float duration)
    {
        if (duration <= 0f) return;
        _slowMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        _slowEndTime = Mathf.Max(_slowEndTime, Time.time + duration);
    }
}
