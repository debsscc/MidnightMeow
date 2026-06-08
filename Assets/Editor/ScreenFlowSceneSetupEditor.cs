#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de editor para instalar hierarquia ---- ScreenFlow ---- e ligar refs nos controllers.
/// </summary>
public static class ScreenFlowSceneSetupEditor
{
    private const string RootName = "---- ScreenFlow ----";

    [MenuItem("MidnightMeow/Screen Flow/Setup Active Scene")]
    public static void SetupActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Screen Flow", "Abra uma cena de fluxo antes de executar.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Screen Flow Setup");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject root = EnsureRoot();
        EnsureBootstrap(root);
        EnsureEventSystemInScene();
        SuppressLegacyMenuButtonInScene(scene.name);
        SuppressLegacyEndGameTemplate(scene.name);

        switch (scene.name)
        {
            case "Menu2":
                EnsureController<MainMenuController>(root);
                break;
            case "Lobby":
                WireLobbyScene();
                break;
            case "Loading1":
            case "Loading2":
                EnsureController<LoadingScreenController>(root);
                break;
            case "Preparation":
                EnsureController<PreparationScreenController>(root);
                break;
            case "Characters":
                EnsureController<CharactersScreenController>(root);
                break;
            case "VictoryScene":
            case "GameOver":
                WireEndGameScene(scene.name);
                break;
            default:
                EditorUtility.DisplayDialog("Screen Flow",
                    $"Cena '{scene.name}' não faz parte do fluxo padrão. Nada foi alterado além do bootstrap.",
                    "OK");
                break;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[ScreenFlow] Setup concluído em '{scene.name}'.");
    }

    [MenuItem("MidnightMeow/Screen Flow/Setup All Flow Scenes")]
    public static void SetupAllFlowScenes()
    {
        string[] scenes =
        {
            "Assets/Scenes/UI/Menu2.unity",
            "Assets/Scenes/UI/Lobby.unity",
            "Assets/Scenes/UI/Loading1.unity",
            "Assets/Scenes/UI/Loading2.unity",
            "Assets/Scenes/UI/Preparation.unity",
            "Assets/Scenes/UI/Characters.unity",
            "Assets/Scenes/UI/VictoryScene.unity",
            "Assets/Scenes/UI/GameOver.unity"
        };

        string original = SceneManager.GetActiveScene().path;
        foreach (string path in scenes)
        {
            if (!System.IO.File.Exists(path))
                continue;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            SetupActiveScene();
            EditorSceneManager.SaveOpenScenes();
        }

        if (!string.IsNullOrEmpty(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
    }

    private static GameObject EnsureRoot()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create ScreenFlow Root");
        return root;
    }

    private static void EnsureBootstrap(GameObject root)
    {
        if (root.GetComponent<ScreenFlowSceneBootstrap>() == null)
            Undo.AddComponent<ScreenFlowSceneBootstrap>(root);
    }

    private static T EnsureController<T>(GameObject root) where T : Component
    {
        T existing = UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(typeof(T).Name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {typeof(T).Name}");
        Undo.SetTransformParent(go.transform, root.transform, "Parent controller");
        return Undo.AddComponent<T>(go);
    }

    private static void EnsureEventSystemInScene()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static void SuppressLegacyMenuButtonInScene(string sceneName)
    {
        if (sceneName is not ("Loading1" or "Loading2" or "Preparation" or "Characters"))
            return;

        Button legacy = FindButtonByName("Button_Menu");
        if (legacy == null)
            return;

        Undo.RecordObject(legacy.gameObject, "Hide legacy menu button");
        legacy.gameObject.SetActive(false);
    }

    private static void SuppressLegacyEndGameTemplate(string sceneName)
    {
        if (sceneName is not ("Loading1" or "Loading2" or "Preparation" or "Characters" or "VictoryScene" or "Lobby" or "Menu2"))
            return;

        DeactivateRootObject("Defeat");

        if (sceneName != "GameOver")
            DeactivateRootObject("Sound Track");
    }

    private static void DeactivateRootObject(string objectName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null || roots[i].name != objectName)
                continue;

            Undo.RecordObject(roots[i], $"Deactivate {objectName}");
            roots[i].SetActive(false);
        }
    }

    private static void WireLobbyScene()
    {
        LobbySceneUIController controller = UnityEngine.Object.FindFirstObjectByType<LobbySceneUIController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            GameObject root = EnsureRoot();
            controller = EnsureController<LobbySceneUIController>(root);
        }

        SerializedObject so = new SerializedObject(controller);
        AssignButton(so, "hostButton", "Host");
        AssignButton(so, "joinButton", "Join");
        AssignButton(so, "startGameButton", "StartGame");
        AssignButton(so, "disconnectButton", "Disconnect");
        AssignButton(so, "copyCodeButton", "CopyCode");
        AssignButton(so, "charactersButton", "Back");
        AssignText(so, "joinCodeText", "JoinCode");
        AssignText(so, "statusText", "ERROCODE", "Status");
        AssignText(so, "playersText", "Texts");
        AssignInput(so, "joinCodeInput");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireEndGameScene(string sceneName)
    {
        EndGameScreenController controller = EnsureController<EndGameScreenController>(EnsureRoot());
        SerializedObject so = new SerializedObject(controller);

        so.FindProperty("isVictory").boolValue = sceneName != "GameOver";
        AssignButton(so, "continueButton", "Button_Continue", "Continue");
        AssignButton(so, "exitButton", "Button_Menu", "Sair");

        so.ApplyModifiedPropertiesWithoutUndo();

        Button legacyMenu = FindButtonByName("Button_Menu");
        if (legacyMenu != null)
        {
            Undo.RecordObject(legacyMenu, "Clear legacy menu listeners");
            legacyMenu.onClick = new Button.ButtonClickedEvent();
        }
    }

    private static void AssignButton(SerializedObject so, string propertyName, params string[] objectNames)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
            return;

        Button button = FindButtonByName(objectNames);
        if (button != null)
            prop.objectReferenceValue = button;
    }

    private static void AssignText(SerializedObject so, string propertyName, params string[] objectNames)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
            return;

        TMP_Text text = FindTextByName(objectNames);
        if (text != null)
            prop.objectReferenceValue = text;
    }

    private static void AssignInput(SerializedObject so, string propertyName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
            return;

        TMP_InputField field = UnityEngine.Object.FindFirstObjectByType<TMP_InputField>(FindObjectsInactive.Include);
        if (field != null)
            prop.objectReferenceValue = field;
    }

    private static Button FindButtonByName(params string[] names)
    {
        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (string name in names)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == name)
                    return buttons[i];
            }
        }

        return null;
    }

    private static TMP_Text FindTextByName(params string[] names)
    {
        TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (string name in names)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == name)
                    return texts[i];
            }
        }

        return null;
    }
}
#endif
