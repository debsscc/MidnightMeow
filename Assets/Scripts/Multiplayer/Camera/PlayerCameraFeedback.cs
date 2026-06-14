using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Feedback de câmera para o jogador local (shake ao tomar dano, etc.).
/// Usa <see cref="MultiplayerCameraController"/> na Fase-1/MP e fallback em <see cref="FollowCamera"/> legado.
/// </summary>
public static class PlayerCameraFeedback
{
    public static void ShakeOnLocalPlayerDamage(CameraShakePreset preset = CameraShakePreset.Medium)
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
}
