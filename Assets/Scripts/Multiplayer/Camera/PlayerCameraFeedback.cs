using UnityEngine;

/// <summary>
/// Feedback de câmera para o jogador local (shake ao tomar dano, acerto melee, etc.).
/// </summary>
public static class PlayerCameraFeedback
{
    private const float MeleeHitShakeSuppressionSeconds = 0.15f;
    private const float MeleeHitShakeIntensity = 0.08f;
    private const float MeleeHitShakeDuration = 0.08f;

    private static float _lastLocalDamageShakeUnscaledTime = float.NegativeInfinity;

    public static void RegisterLocalDamageShake()
    {
        _lastLocalDamageShakeUnscaledTime = Time.unscaledTime;
    }

    public static void ShakeOnLocalPlayerDamage(CameraShakePreset preset = CameraShakePreset.Medium)
    {
        RegisterLocalDamageShake();

        MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
        if (controller != null)
        {
            controller.Shake(preset);
            return;
        }

        if (CameraShakeController.Instance != null)
        {
            CameraShakeController.Instance.Shake(preset);
            return;
        }

        FollowCamera.Instance?.Shake();
    }

    public static void ShakeOnMeleeHit(int hitCount)
    {
        if (hitCount <= 0)
            return;

        if (Time.unscaledTime - _lastLocalDamageShakeUnscaledTime < MeleeHitShakeSuppressionSeconds)
            return;

        MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
        if (controller != null)
        {
            controller.ShakeCustom(MeleeHitShakeIntensity, MeleeHitShakeDuration);
            return;
        }

        if (CameraShakeController.Instance != null)
        {
            CameraShakeController.Instance.ShakeCustom(MeleeHitShakeIntensity, MeleeHitShakeDuration);
            return;
        }

        FollowCamera.Instance?.Shake(MeleeHitShakeIntensity, MeleeHitShakeDuration);
    }
}
