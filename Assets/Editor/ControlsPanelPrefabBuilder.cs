#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Reconstrói o prefab Controls com abas Teclado/Mouse + Gamepad, labels localizados e botão Voltar.
/// Menu: MidnightMeow/UI/Rebuild Controls Panel Prefab
/// </summary>
public static class ControlsPanelPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/Controls.prefab";
    private const string FontGuid = "7faf118ce2a110c439c07239d9436bcd";
    private const string UiTableGuid = "da2895712265ed0499f3cae10d3b7d2e";
    private const string BackSpriteGuid = "7415ed8b1f369c84bad57d02e1d9a92f";
    private const string KeyboardTabSpriteGuid = "190d720de85876740bea5e3e391e5f7d";
    private const string GamepadTabSpriteGuid = "b35caff58e9b61e4e9c2e2cfcd5bf8ef";
    private const string KeyboardBgGuid = "9a73acaa5459b0544b5a5c9a5dbe7e52";
    private const string GamepadBgGuid = "bb513c0680b3470439c3e38f9b247e58";

    [MenuItem("MidnightMeow/UI/Rebuild Controls Panel Prefab")]
    public static void RebuildPrefab()
    {
        GameObject root = new GameObject("Controls", typeof(RectTransform), typeof(CanvasRenderer), typeof(ControlsPanelController));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        Stretch(rootRt);

        ControlsPanelController controller = root.GetComponent<ControlsPanelController>();

        Transform tabs = CreateChild(root.transform, "Tabs");
        RectTransform tabsRt = tabs.GetComponent<RectTransform>();
        tabsRt.anchorMin = new Vector2(0.5f, 1f);
        tabsRt.anchorMax = new Vector2(0.5f, 1f);
        tabsRt.pivot = new Vector2(0.5f, 1f);
        tabsRt.sizeDelta = new Vector2(900f, 90f);
        tabsRt.anchoredPosition = new Vector2(0f, -20f);

        Button keyboardTab = CreateTabButton(tabs, "Tab_KeyboardMouse", "tab.controls.keyboard_mouse",
            new Vector2(-230f, 0f), new Vector2(420f, 70f), KeyboardTabSpriteGuid);
        Button gamepadTab = CreateTabButton(tabs, "Tab_Gamepad", "tab.controls.gamepad",
            new Vector2(230f, 0f), new Vector2(420f, 70f), GamepadTabSpriteGuid);

        Transform content = CreateChild(root.transform, "Content");
        Stretch(content.GetComponent<RectTransform>());

        GameObject keyboardPanel = CreateInputPanel(content, "Panel_KeyboardMouse", KeyboardBgGuid, true,
            new (string key, Vector2 pos)[]
            {
                ("controls.action.move", new Vector2(-180f, 120f)),
                ("controls.action.dash", new Vector2(-180f, 60f)),
                ("controls.action.ability2", new Vector2(-180f, 0f)),
                ("controls.action.frenzy", new Vector2(-180f, -60f)),
                ("controls.action.interact", new Vector2(-180f, -120f)),
                ("controls.action.pause", new Vector2(-180f, -180f)),
                ("controls.action.fire", new Vector2(220f, 80f)),
                ("controls.action.ability1", new Vector2(220f, -40f)),
            });

        GameObject gamepadPanel = CreateInputPanel(content, "Panel_Gamepad", GamepadBgGuid, false,
            new (string key, Vector2 pos)[]
            {
                ("controls.action.move", new Vector2(-210f, 130f)),
                ("controls.action.aim", new Vector2(210f, 130f)),
                ("controls.action.pause", new Vector2(210f, -150f)),
                ("controls.action.fire", new Vector2(210f, 80f)),
                ("controls.action.dash", new Vector2(210f, 30f)),
                ("controls.action.frenzy", new Vector2(-120f, -120f)),
                ("controls.action.ability2", new Vector2(0f, -150f)),
                ("controls.action.ability1", new Vector2(120f, -150f)),
                ("controls.action.interact", new Vector2(-120f, -180f)),
            });

        Button backButton = CreateBackButton(root.transform);

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("keyboardMouseTabButton").objectReferenceValue = keyboardTab;
        so.FindProperty("gamepadTabButton").objectReferenceValue = gamepadTab;
        so.FindProperty("keyboardMousePanel").objectReferenceValue = keyboardPanel;
        so.FindProperty("gamepadPanel").objectReferenceValue = gamepadPanel;
        so.FindProperty("backButton").objectReferenceValue = backButton;
        so.FindProperty("keyboardMouseTabGraphic").objectReferenceValue = keyboardTab.targetGraphic;
        so.FindProperty("gamepadTabGraphic").objectReferenceValue = gamepadTab.targetGraphic;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Controls Panel",
            "Prefab reconstruído em Assets/Prefabs/UI/Controls.prefab.\n\n" +
            "Próximo passo: na Menu2, remova Controls do MenuTabController e use ControlsPanelOpener no botão Opções > Controles.",
            "OK");
    }

    private static GameObject CreateInputPanel(Transform parent, string name, string bgGuid, bool active,
        (string key, Vector2 pos)[] labels)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.SetActive(active);

        Image bg = CreateImage(panel.transform, "Background", bgGuid);
        Stretch(bg.rectTransform);

        Transform labelsRoot = CreateChild(panel.transform, "Labels");
        Stretch(labelsRoot.GetComponent<RectTransform>());

        for (int i = 0; i < labels.Length; i++)
            CreateLocalizedLabel(labelsRoot, $"Label_{labels[i].key.Replace('.', '_')}", labels[i].key, labels[i].pos);

        return panel;
    }

    private static Button CreateTabButton(Transform parent, string name, string locKey, Vector2 pos, Vector2 size, string spriteGuid)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite(spriteGuid);
        image.type = Image.Type.Sliced;
        image.preserveAspect = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        CreateLocalizedLabel(go.transform, "Label", locKey, Vector2.zero, 28f, TextAlignmentOptions.Center);

        return button;
    }

    private static Button CreateBackButton(Transform parent)
    {
        GameObject go = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(160f, 100f);
        rt.anchoredPosition = new Vector2(24f, -24f);

        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite(BackSpriteGuid);
        image.preserveAspect = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        CreateLocalizedLabel(go.transform, "Label", "btn.back", Vector2.zero, 32f, TextAlignmentOptions.Center);

        return button;
    }

    private static TMP_Text CreateLocalizedLabel(Transform parent, string name, string locKey, Vector2 pos,
        float fontSize = 36f, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 50f);
        rt.anchoredPosition = pos;

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = LoadFont();
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.33f, 0.29f, 0.25f, 1f);
        tmp.alignment = alignment;
        tmp.text = locKey;

        LocalizeStringEvent loc = go.AddComponent<LocalizeStringEvent>();
        loc.StringReference.SetReference(UiTableGuid, locKey);
        loc.OnUpdateString.AddListener(value => tmp.text = value);

        return tmp;
    }

    private static Image CreateImage(Transform parent, string name, string spriteGuid)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = LoadSprite(spriteGuid);
        image.preserveAspect = true;
        return image;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static Sprite LoadSprite(string guid) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));

    private static TMP_FontAsset LoadFont() =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));
}
#endif
