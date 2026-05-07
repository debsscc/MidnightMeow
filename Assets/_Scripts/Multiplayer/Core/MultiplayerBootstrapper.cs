/// <summary>
/// MultiplayerBootstrapper.cs
/// Valida em Start() se todos os componentes obrigatórios da cena Sandbox estão presentes.
/// Usa Start() em vez de Awake() para garantir que o NetworkManager (que chama DontDestroyOnLoad
/// no seu próprio Awake) já esteja registrado como Singleton antes da verificação.
/// Usa FindFirstObjectByType como fallback para encontrar objetos em DontDestroyOnLoad.
/// SRP: apenas validação de cena, sem lógica de jogo.
/// </summary>

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MultiplayerBootstrapper : MonoBehaviour
{
    [Header("Referências Obrigatórias (auto-detectadas se nulas)")]
    [SerializeField] private RelayManager relayManager;
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private MultiplayerGameManager gameManager;
    [SerializeField] private NetworkWaveManager waveManager;

    private bool _sceneIsValid = true;

    private void Start()
    {
        Debug.Log("[MultiplayerBootstrapper] Validando configuração da cena Sandbox...");
        ValidateScene();

        if (_sceneIsValid)
            Debug.Log("[MultiplayerBootstrapper] Cena validada com sucesso. Todos os componentes encontrados.");
        else
            Debug.LogError("[MultiplayerBootstrapper] CONFIGURAÇÃO INCOMPLETA — revise os erros acima antes de testar.");
    }

    private void ValidateScene()
    {
        CheckNetworkManager();
        CheckUnityTransport();
        CheckSingleton("RelayManager", relayManager, FindFirstObjectByType<RelayManager>());
        CheckSingleton("ConnectionManager", connectionManager, FindFirstObjectByType<ConnectionManager>());
        CheckNetworkBehaviour("MultiplayerGameManager", gameManager, FindFirstObjectByType<MultiplayerGameManager>());
        CheckNetworkBehaviour("NetworkWaveManager", waveManager, FindFirstObjectByType<NetworkWaveManager>());
        CheckPlayerSpawnManager();
        CheckCameraRig();
        CheckProjectSettings();
    }

    private void CheckNetworkManager()
    {
        // NetworkManager.Singleton é a forma preferida, mas o NGO move o objeto para
        // DontDestroyOnLoad no seu próprio Awake. Como usamos Start(), Singleton já está setado.
        // FindFirstObjectByType(FindObjectsInactive.Include) inclui objetos inativos e funciona com DontDestroyOnLoad.
        bool found = NetworkManager.Singleton != null ||
                     FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include) != null;

        if (!found)
        {
            Debug.LogError(
                "[MultiplayerBootstrapper] ERRO: NetworkManager não encontrado na cena!\n" +
                "→ Crie um GameObject chamado 'NetworkManager' e adicione o componente NetworkManager.\n" +
                "→ Adicione o componente UnityTransport ao mesmo GameObject.\n" +
                "→ No Inspector do NetworkManager, arraste o UnityTransport para o campo 'Network Transport'."
            );
            _sceneIsValid = false;
        }
        else
        {
            string location = NetworkManager.Singleton != null ? "Singleton" : "DontDestroyOnLoad (FindFirstObjectByType)";
            Debug.Log($"[MultiplayerBootstrapper] OK: NetworkManager encontrado via {location}.");
        }
    }

    private NetworkManager GetNetworkManager() =>
        NetworkManager.Singleton != null
            ? NetworkManager.Singleton
            : FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);

    private void CheckUnityTransport()
    {
        var nm = GetNetworkManager();
        if (nm == null) return;

        var transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError(
                "[MultiplayerBootstrapper] ERRO: UnityTransport não encontrado no NetworkManager!\n" +
                "→ Selecione o GameObject 'NetworkManager' e clique em 'Add Component → Unity Transport'."
            );
            _sceneIsValid = false;
        }
        else
        {
            Debug.Log($"[MultiplayerBootstrapper] OK: UnityTransport encontrado (Protocol: {transport.Protocol}).");
        }
    }

    private void CheckSingleton<T>(string name, T serializedRef, T foundInScene) where T : MonoBehaviour
    {
        T target = serializedRef != null ? serializedRef : foundInScene;
        if (target == null)
        {
            Debug.LogError(
                $"[MultiplayerBootstrapper] ERRO: {name} não encontrado na cena!\n" +
                $"→ Crie um GameObject chamado '{name}' e adicione o componente {name}.\n" +
                $"→ Ou arraste-o para o campo correspondente no Inspector do MultiplayerBootstrapper."
            );
            _sceneIsValid = false;
        }
        else
        {
            Debug.Log($"[MultiplayerBootstrapper] OK: {name} encontrado em '{target.gameObject.name}'.");
        }
    }

    private void CheckNetworkBehaviour<T>(string name, T serializedRef, T foundInScene) where T : NetworkBehaviour
    {
        T target = serializedRef != null ? serializedRef : foundInScene;
        if (target == null)
        {
            Debug.LogError(
                $"[MultiplayerBootstrapper] ERRO: {name} não encontrado na cena!\n" +
                $"→ Crie um GameObject, adicione o componente {name} E o componente NetworkObject.\n" +
                $"→ ATENÇÃO: NetworkBehaviours SEMPRE precisam de um NetworkObject no mesmo GameObject."
            );
            _sceneIsValid = false;
        }
        else
        {
            var networkObject = target.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError(
                    $"[MultiplayerBootstrapper] ERRO CRÍTICO: {name} existe mas NÃO tem NetworkObject!\n" +
                    $"→ Selecione o GameObject '{target.gameObject.name}' e adicione o componente NetworkObject.\n" +
                    $"→ Sem NetworkObject, OnNetworkSpawn nunca será chamado e o script não funcionará."
                );
                _sceneIsValid = false;
            }
            else
            {
                Debug.Log($"[MultiplayerBootstrapper] OK: {name} encontrado com NetworkObject em '{target.gameObject.name}'.");
            }
        }
    }

    private void CheckPlayerSpawnManager()
    {
        var psm = FindFirstObjectByType<PlayerSpawnManager>(FindObjectsInactive.Include);
        if (psm == null)
        {
            Debug.LogError(
                "[MultiplayerBootstrapper] ERRO CRÍTICO: PlayerSpawnManager NÃO encontrado na cena!\n" +
                "→ Crie um GameObject (ex: 'PlayerSpawnManager') e adicione os componentes:\n" +
                "   1. NetworkObject\n" +
                "   2. PlayerSpawnManager\n" +
                "→ No Inspector do PlayerSpawnManager:\n" +
                "   - Player Network Prefab: arraste o prefab do jogador\n" +
                "   - Spawn Points: arraste os pontos de spawn\n" +
                "→ SEM este componente nenhum jogador será spawnado e a câmera ficará azul!"
            );
            _sceneIsValid = false;
            return;
        }

        var netObj = psm.GetComponent<Unity.Netcode.NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError(
                $"[MultiplayerBootstrapper] ERRO CRÍTICO: PlayerSpawnManager existe em '{psm.gameObject.name}' mas SEM NetworkObject!\n" +
                "→ Adicione o componente NetworkObject ao mesmo GameObject do PlayerSpawnManager.\n" +
                "→ Sem NetworkObject, OnNetworkSpawn() nunca é chamado e nenhum jogador spawna."
            );
            _sceneIsValid = false;
            return;
        }

        Debug.Log($"[MultiplayerBootstrapper] OK: PlayerSpawnManager encontrado com NetworkObject em '{psm.gameObject.name}'.");
    }

    private void CheckCameraRig()
    {
        var cam = FindFirstObjectByType<MultiplayerCameraController>(FindObjectsInactive.Include);
        if (cam == null)
        {
            Debug.LogError(
                "[MultiplayerBootstrapper] ERRO: MultiplayerCameraController NÃO encontrado na cena!\n" +
                "→ Crie o MultiplayerCameraRig conforme a hierarquia documentada no script.\n" +
                "→ Sem o rig de câmera a tela ficará azul mesmo com o jogador spawnado."
            );
            _sceneIsValid = false;
        }
        else
        {
            Debug.Log($"[MultiplayerBootstrapper] OK: MultiplayerCameraController encontrado em '{cam.gameObject.name}'.");
        }
    }

    private void CheckProjectSettings()
    {
        // Verifica se Unity Services está configurado (Project ID)
        try
        {
            string projectId = Unity.Services.Core.UnityServices.Instance?.ToString();
        }
        catch
        {
            Debug.LogWarning(
                "[MultiplayerBootstrapper] AVISO: Não foi possível verificar Unity Services.\n" +
                "→ Certifique-se de que o Project ID está configurado em Edit → Project Settings → Services.\n" +
                "→ O projeto precisa estar vinculado ao Unity Dashboard para o Relay funcionar."
            );
        }

#if !UNITY_EDITOR
        Debug.Log("[MultiplayerBootstrapper] Build de produção detectada. Validação concluída.");
#endif
    }
}
