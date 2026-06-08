using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Remove UI e áudio legados copiados do template de vitória/derrota em cenas que não são fim de jogo.
/// </summary>
public static class ScreenFlowLegacySceneCleanup
{
    public static void ApplyForActiveScene()
    {
        string scene = SceneManager.GetActiveScene().name;

        switch (scene)
        {
            case "Loading1":
            case "Loading2":
            case "Preparation":
            case "Characters":
            case "VictoryScene":
                DeactivateRoot("Defeat");
                DeactivateRoot("Sound Track");
                break;
            case "Lobby":
            case "Menu2":
                DeactivateRoot("Defeat");
                break;
        }
    }

    private static void DeactivateRoot(string objectName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == objectName)
                roots[i].SetActive(false);
        }
    }
}
