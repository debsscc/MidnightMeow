/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-23
DESCRIÇÃO: Feedback de câmera do jogador local (shake, zoom punch, feel).
Tiro normal não usa shake.
---------------------------------------------------------------- */

using UnityEngine;

public static class PlayerCameraFeedback
{
    private const float MeleeHitShakeSuppressionSeconds = 0.15f;
    private const float MeleeHitShakeIntensity = 0.08f;
    private const float MeleeHitShakeDuration = 0.08f;

    private const float DashAbilityShakeIntensity = 0.08f;
    private const float DashAbilityShakeDuration = 0.12f;

    private const float KillShakeIntensity = 0.06f;
    private const float KillShakeDuration = 0.09f;
    private const float KillShakeCooldownSeconds = 0.14f;

    private const float DeathShakeIntensity = 0.28f;
    private const float DeathShakeDuration = 0.32f;

    private static float _lastLocalDamageShakeUnscaledTime = float.NegativeInfinity;
    private static float _lastKillShakeUnscaledTime = float.NegativeInfinity;

    public static void RegisterLocalDamageShake()
    {
        _lastLocalDamageShakeUnscaledTime = Time.unscaledTime;
    }

    public static void ShakeOnLocalPlayerDamage(CameraShakePreset preset = CameraShakePreset.Medium)
    {
        RegisterLocalDamageShake();
        GameplayVignetteController.TriggerDamagePulse();
        DispatchShake(preset);
    }

    public static void ShakeOnMeleeHit(int hitCount)
    {
        if (hitCount <= 0)
            return;

        if (Time.unscaledTime - _lastLocalDamageShakeUnscaledTime < MeleeHitShakeSuppressionSeconds)
            return;

        DispatchShakeCustom(MeleeHitShakeIntensity, MeleeHitShakeDuration);
    }

    public static void ShakeOnDash()
    {
        DispatchShakeCustom(DashAbilityShakeIntensity, DashAbilityShakeDuration);
        DispatchZoomPunch();
    }

    public static void ShakeOnAbility()
    {
        DispatchShakeCustom(DashAbilityShakeIntensity, DashAbilityShakeDuration);
        DispatchZoomPunch();
    }

    public static void ShakeOnEnemyKill()
    {
        if (Time.unscaledTime - _lastKillShakeUnscaledTime < KillShakeCooldownSeconds)
            return;

        _lastKillShakeUnscaledTime = Time.unscaledTime;
        DispatchShakeCustom(KillShakeIntensity, KillShakeDuration);
    }

    public static void ShakeOnLocalDeath()
        => DispatchShakeCustom(DeathShakeIntensity, DeathShakeDuration);

    public static void SetLocomotionFeel(Vector2 moveInput, float speedMagnitude)
    {
        MultiplayerCameraController.Resolve()?.SetLocomotionFeel(moveInput, speedMagnitude);
    }

    private static void DispatchZoomPunch()
    {
        MultiplayerCameraController.Resolve()?.PunchZoom();
    }

    private static void DispatchShake(CameraShakePreset preset)
    {
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

    private static void DispatchShakeCustom(float intensity, float duration)
    {
        MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
        if (controller != null)
        {
            controller.ShakeCustom(intensity, duration);
            return;
        }

        if (CameraShakeController.Instance != null)
        {
            CameraShakeController.Instance.ShakeCustom(intensity, duration);
            return;
        }

        FollowCamera.Instance?.Shake(intensity, duration);
    }
}
