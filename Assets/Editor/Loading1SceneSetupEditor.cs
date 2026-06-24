using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Monta a barra de loading em Loading1 e liga refs no <see cref="LoadingScreenController"/>.
/// Menu: Midnight Meow → Setup Loading1 UI
/// </summary>
public static class Loading1SceneSetupEditor
{
    private const string Loading1ScenePath = "Assets/Scenes/UI/Loading1.unity";

    [MenuItem("Midnight Meow/Setup Loading1 UI")]
    public static void SetupLoading1FromMenu()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path != Loading1ScenePath)
        {
            if (!EditorUtility.DisplayDialog(
                    "Setup Loading1 UI",
                    "Abrir a cena Loading1 e aplicar o setup da barra de loading?",
                    "Abrir e aplicar",
                    "Cancelar"))
                return;

            if (!System.IO.File.Exists(Loading1ScenePath))
            {
                Debug.LogError($"[Loading1Setup] Cena não encontrada: {Loading1ScenePath}");
                return;
            }

            EditorSceneManager.OpenScene(Loading1ScenePath);
        }

        ApplySetup();
    }

    public static void ApplySetup()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[Loading1Setup] Canvas não encontrado na cena.");
            return;
        }

        Transform canvasRoot = canvas.transform;

        RectTransform track = EnsureProgressTrack(canvasRoot);
        Image fill = LoadingProgressUtility.ResolveOrCreateFill(track);
        LoadingProgressUtility.ResetProgress(fill);

        RectTransform follower = EnsureFollower(canvasRoot, track);
        TMP_Text status = FindChildByName<TMP_Text>(canvasRoot, "Text_Loading");

        LoadingScreenController controller = Object.FindFirstObjectByType<LoadingScreenController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogError("[Loading1Setup] LoadingScreenController não encontrado.");
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("minimumDisplaySeconds").floatValue = 7f;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("progressTrack").objectReferenceValue = track;
        so.FindProperty("progressFill").objectReferenceValue = fill;
        so.FindProperty("progressFollower").objectReferenceValue = follower;
        so.FindProperty("followerYOffset").floatValue = 72f;
        so.FindProperty("buildPlaceholderIfMissing").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Loading1Setup] Barra, follower e refs aplicados em Loading1.");
    }

    private static RectTransform EnsureProgressTrack(Transform canvasRoot)
    {
        Transform existing = canvasRoot.Find("ProgressTrack");
        if (existing != null)
            return existing as RectTransform;

        GameObject trackGo = new GameObject("ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(trackGo, "Create ProgressTrack");
        trackGo.transform.SetParent(canvasRoot, false);
        trackGo.layer = canvasRoot.gameObject.layer;

        RectTransform trackRt = trackGo.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0.5f, 0f);
        trackRt.anchorMax = new Vector2(0.5f, 0f);
        trackRt.pivot = new Vector2(0.5f, 0.5f);
        trackRt.anchoredPosition = new Vector2(54f, 130f);
        trackRt.sizeDelta = new Vector2(900f, 28f);

        Image trackImage = trackGo.GetComponent<Image>();
        trackImage.color = LoadingProgressUtility.DefaultTrackColor;
        LoadingProgressUtility.ApplySolidSprite(trackImage);

        return trackRt;
    }

    private static RectTransform EnsureFollower(Transform canvasRoot, RectTransform track)
    {
        Transform existing = canvasRoot.Find("Character_loading");
        if (existing == null)
            existing = track.Find("Character_loading");

        GameObject followerGo;
        if (existing != null)
        {
            followerGo = existing.gameObject;
            Undo.SetTransformParent(followerGo.transform, track, "Reparent Character_loading");
        }
        else
        {
            followerGo = new GameObject("Character_loading", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(followerGo, "Create Character_loading");
            followerGo.transform.SetParent(track, false);
            followerGo.layer = canvasRoot.gameObject.layer;

            Image image = followerGo.GetComponent<Image>();
            image.color = Color.white;
            image.preserveAspect = true;
        }

        RectTransform followerRt = followerGo.GetComponent<RectTransform>();
        followerRt.anchorMin = new Vector2(0.5f, 0.5f);
        followerRt.anchorMax = new Vector2(0.5f, 0.5f);
        followerRt.pivot = new Vector2(0.5f, 0f);
        followerRt.sizeDelta = new Vector2(150f, 150f);
        LoadingProgressUtility.SetFollowerAlongTrack(track, followerRt, 0f, 72f);

        return followerRt;
    }

    private static T FindChildByName<T>(Transform root, string objectName) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].gameObject.name == objectName)
                return components[i];
        }

        return null;
    }
}
