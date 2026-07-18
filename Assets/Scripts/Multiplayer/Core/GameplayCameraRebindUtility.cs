using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebind atrasado da câmera local após carga de Fase-* (cliente NGO spawna jogador depois do SynchronizeComplete).
/// </summary>
public static class GameplayCameraRebindUtility
{
    private static GameplayCameraRebindRunner _runner;

    public static void ScheduleAfterGameplaySceneReady()
    {
        EnsureRunner();
        _runner.RestartDelayedRebind();
    }

    private static void EnsureRunner()
    {
        if (_runner != null)
            return;

        var go = new GameObject(nameof(GameplayCameraRebindRunner));
        Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<GameplayCameraRebindRunner>();
    }

    private sealed class GameplayCameraRebindRunner : MonoBehaviour
    {
        private Coroutine _routine;
        private static readonly float[] RebindDelaysSeconds = { 0f, 0.2f, 0.5f, 1f, 1.5f, 2.5f };

        public void RestartDelayedRebind()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(DelayedRebindRoutine());
        }

        private IEnumerator DelayedRebindRoutine()
        {
            for (int i = 0; i < RebindDelaysSeconds.Length; i++)
            {
                if (RebindDelaysSeconds[i] > 0f)
                    yield return new WaitForSeconds(RebindDelaysSeconds[i]);

                if (!GameplaySceneBootstrap.IsGameplayScene(SceneManager.GetActiveScene().name))
                    break;

                GameplaySceneBootstrap.EnsureCameraRigPresent();
                if (i >= 2)
                    GameplaySceneBootstrap.SpawnCameraRigIfMissing();

                GameplaySceneBootstrap.RebindLocalPlayerCamera();
                AspectLetterboxController.EnsureExists()?.Reapply();

                MultiplayerCameraController controller = MultiplayerCameraController.Resolve();
                if (controller != null && controller.IsFollowingTarget)
                    break;
            }

            GameplaySceneBootstrap.EnsureActiveGameplayCamera();
            AspectLetterboxController.EnsureExists()?.Reapply();
            _routine = null;
        }
    }
}
