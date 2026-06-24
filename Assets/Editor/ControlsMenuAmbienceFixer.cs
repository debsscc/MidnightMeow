#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Remove Light/Particle duplicados dentro do prefab Controls na Menu2.
/// Luz e partículas da raiz da cena permanecem sempre ativas.
/// </summary>
public static class ControlsMenuAmbienceFixer
{
    private const string Menu2ScenePath = "Assets/Scenes/UI/Menu2.unity";
    private const string ControlsPrefabPath = "Assets/Prefabs/UI/Controls.prefab";

    [MenuItem("MidnightMeow/UI/Fix Menu2 Controls ambience (scene-only)")]
    public static void FixMenu2Ambience()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(Menu2ScenePath, OpenSceneMode.Single);

        GameObject controlsRoot = FindControlsPrefabRoot();
        if (controlsRoot != null)
            RemoveEmbeddedAmbienceFromControls(controlsRoot);

        GameObject sceneLight = FindSceneRootByName("Light");
        GameObject sceneParticles = FindSceneRootByName("ParticleSystem");

        if (sceneLight == null || sceneParticles == null)
        {
            EditorUtility.DisplayDialog(
                "Controls ambience",
                "Não encontrei 'Light' e/ou 'ParticleSystem' na raiz da Menu2.\n" +
                "Restaure esses objetos na cena antes de rodar o fix.",
                "OK");
            return;
        }

        sceneLight.SetActive(true);
        sceneParticles.SetActive(true);

        if (controlsRoot != null)
            controlsRoot.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Controls ambience",
            "Menu2 corrigida:\n" +
            "- Duplicatas removidas do Controls\n" +
            "- Light + ParticleSystem na raiz mantidos ativos\n" +
            "- Prefab Controls permanece só UI (sem luz global extra)",
            "OK");
    }

    private static GameObject FindControlsPrefabRoot()
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.name.Equals("Controls", System.StringComparison.Ordinal))
                continue;

            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (root == null)
                continue;

            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) == ControlsPrefabPath)
                return root;
        }

        return null;
    }

    private static void RemoveEmbeddedAmbienceFromControls(GameObject controlsRoot)
    {
        Transform root = controlsRoot.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            string name = child.name;
            if (name == "Light" || name == "ParticleSystem")
                Object.DestroyImmediate(child.gameObject);
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(controlsRoot);
    }

    private static GameObject FindSceneRootByName(string objectName)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.name.Equals(objectName, System.StringComparison.Ordinal))
                continue;

            if (go.transform.parent == null)
                return go;
        }

        return null;
    }
}
#endif
