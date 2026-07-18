#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Extrai Light + ParticleSystem da Menu2 para um prefab em Resources (créditos e reuso).
/// </summary>
public static class MenuUiAmbiencePrefabBuilder
{
    private const string Menu2ScenePath = "Assets/Scenes/UI/Menu2.unity";
    private const string PrefabPath = "Assets/Resources/UI/MenuUiAmbience.prefab";

    [MenuItem("MidnightMeow/UI/Build MenuUiAmbience Prefab from Menu2")]
    public static void BuildFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!BuildInternal(additive: false, out string error))
        {
            EditorUtility.DisplayDialog("MenuUiAmbience", error, "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "MenuUiAmbience",
            $"Prefab gerado em:\n{PrefabPath}",
            "OK");
    }

    /// <summary>Uso: Unity -batchmode -executeMethod MenuUiAmbiencePrefabBuilder.BuildFromBatch</summary>
    public static void BuildFromBatch()
    {
        if (!BuildInternal(additive: false, out string error))
        {
            Debug.LogError($"[MenuUiAmbiencePrefabBuilder] {error}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[MenuUiAmbiencePrefabBuilder] Prefab salvo: {PrefabPath}");
        EditorApplication.Exit(0);
    }

    private static bool BuildInternal(bool additive, out string error)
    {
        error = null;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");

        Scene menuScene;
        Scene previousActive = default;
        bool openedAdditive = false;

        if (additive)
        {
            previousActive = EditorSceneManager.GetActiveScene();
            menuScene = EditorSceneManager.OpenScene(Menu2ScenePath, OpenSceneMode.Additive);
            openedAdditive = true;
            EditorSceneManager.SetActiveScene(menuScene);
        }
        else
        {
            menuScene = EditorSceneManager.OpenScene(Menu2ScenePath, OpenSceneMode.Single);
        }

        GameObject light = FindSceneRootByName("Light", menuScene);
        GameObject particles = FindSceneRootByName("ParticleSystem", menuScene);
        if (light == null || particles == null)
        {
            error = "Não encontrei 'Light' e/ou 'ParticleSystem' na raiz da Menu2.";
            if (openedAdditive)
                EditorSceneManager.CloseScene(menuScene, true);
            return false;
        }

        GameObject root = new GameObject("MenuUiAmbience");
        try
        {
            GameObject lightCopy = Object.Instantiate(light, root.transform, false);
            lightCopy.name = "Light";
            GameObject particlesCopy = Object.Instantiate(particles, root.transform, false);
            particlesCopy.name = "ParticleSystem";

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (openedAdditive)
            {
                EditorSceneManager.CloseScene(menuScene, true);
                if (previousActive.IsValid())
                    EditorSceneManager.SetActiveScene(previousActive);
            }
        }

        return true;
    }

    private static GameObject FindSceneRootByName(string objectName, Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = roots[i];
            if (go != null && go.name.Equals(objectName, System.StringComparison.Ordinal))
                return go;
        }

        return null;
    }
}
#endif
