using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Prepara a sessão de gameplay (host local no solo) antes de carregar Fase-1.
/// Sem host, PlayerSpawnManager e a câmera não recebem o jogador — tela fica azul.
/// </summary>
public static class GameplaySessionStarter
{
    public static IEnumerator EnsureReadyForGameplay()
    {
        ScreenFlowController.Instance?.ClearTransitionOverlay();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            yield break;

        if (!GameSessionContext.IsSinglePlayer)
            yield break;

        ConnectionManager connection = ConnectionManager.Instance;
        if (connection == null)
        {
            Debug.LogError("[GameplaySessionStarter] ConnectionManager ausente. Inicie pelo BootstrapScene.");
            yield break;
        }

        if (!connection.TryStartLocalSoloHost())
        {
            Debug.LogError("[GameplaySessionStarter] Falha ao iniciar host local para solo.");
            yield break;
        }

        float timeout = 5f;
        while (timeout > 0f)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                yield break;

            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogError("[GameplaySessionStarter] Timeout aguardando host local para solo.");
    }
}
