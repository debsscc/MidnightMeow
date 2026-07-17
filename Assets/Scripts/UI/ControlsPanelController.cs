/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-07-01
DESCRIÇÃO: Painel de controles reutilizável (Menu2, Pause, etc.):
           abas Teclado/Mouse e Gamepad.
---------------------------------------------------------------- */

using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ControlsPanelController : MonoBehaviour
{
    public enum ControlsTab
    {
        KeyboardMouse = 0,
        Gamepad = 1
    }

    [Header("Abas (botões invisíveis sobre a arte)")]
    [SerializeField] private Button keyboardMouseTabButton;
    [SerializeField] private Button gamepadTabButton;

    [Header("Backgrounds")]
    [SerializeField] private GameObject keyboardMouseBackground;
    [SerializeField] private GameObject gamepadBackground;

    [Header("Grupos de texto")]
    [SerializeField] private GameObject keyboardMouseTexts;
    [SerializeField] private GameObject gamepadTexts;

    [Header("Comportamento")]
    [SerializeField] private ControlsTab defaultTab = ControlsTab.KeyboardMouse;

    [Tooltip("Root visual do prefab Controls na cena. Se vazio, busca um GameObject \"Controls\" que não seja botão.")]
    [SerializeField] private GameObject panelRoot;

    public static ControlsPanelController Instance { get; private set; }

    public bool IsPanelVisible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        AutoWirePanelRoot();
        AutoWireIfMissing();

        if (keyboardMouseTabButton != null)
        {
            keyboardMouseTabButton.onClick.RemoveAllListeners();
            keyboardMouseTabButton.onClick.AddListener(OpenKeyboardMouseTab);
        }

        if (gamepadTabButton != null)
        {
            gamepadTabButton.onClick.RemoveAllListeners();
            gamepadTabButton.onClick.AddListener(OpenGamepadTab);
        }
    }

    private void OnEnable()
    {
        Instance = this;
        OpenTab(defaultTab);
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        AutoWirePanelRoot();
        if (panelRoot != null)
            panelRoot.SetActive(true);

        OpenTab(defaultTab);
    }

    public void HidePanel()
    {
        AutoWirePanelRoot();
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>Abre o painel escondendo o painel de retorno (ex.: Opções do menu).</summary>
    public void ShowFrom(GameObject returnPanel)
    {
        if (returnPanel != null)
            returnPanel.SetActive(false);

        Show();
    }

    public void OpenTab(ControlsTab tab)
    {
        bool keyboard = tab == ControlsTab.KeyboardMouse;

        if (keyboardMouseBackground != null)
            keyboardMouseBackground.SetActive(keyboard);

        if (gamepadBackground != null)
            gamepadBackground.SetActive(!keyboard);

        if (keyboardMouseTexts != null)
            keyboardMouseTexts.SetActive(keyboard);

        if (gamepadTexts != null)
            gamepadTexts.SetActive(!keyboard);

        if (keyboard && keyboardMouseTabButton != null)
            UiSelectionUtility.Select(keyboardMouseTabButton);
        else if (!keyboard && gamepadTabButton != null)
            UiSelectionUtility.Select(gamepadTabButton);
        else
            UiSelectionUtility.SelectFirstUnder(transform);
    }

    public void OpenKeyboardMouseTab() => OpenTab(ControlsTab.KeyboardMouse);

    public void OpenGamepadTab() => OpenTab(ControlsTab.Gamepad);

    public static ControlsPanelController FindInScene()
    {
        if (Instance != null)
            return Instance;

        return FindFirstObjectByType<ControlsPanelController>(FindObjectsInactive.Include);
    }

    private void AutoWirePanelRoot()
    {
        if (panelRoot != null)
            return;

        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject candidate = all[i];
            if (candidate == null || candidate.name != "Controls")
                continue;

            if (candidate.GetComponent<ControlsPanelController>() != null)
                continue;

            if (candidate.GetComponent<Button>() != null)
                continue;

            if (candidate.GetComponent<RectTransform>() == null)
                continue;

            panelRoot = candidate;
            return;
        }
    }

    private void AutoWireIfMissing()
    {
        Transform root = panelRoot != null ? panelRoot.transform : transform;

        if (keyboardMouseBackground == null)
            keyboardMouseBackground = FindDeepChild(root, "Image_MOUSE")?.gameObject;

        if (gamepadBackground == null)
            gamepadBackground = FindDeepChild(root, "Image_gAMEPAD")?.gameObject
                                ?? FindDeepChild(root, "Image_Gamepad")?.gameObject;

        if (keyboardMouseTexts == null)
            keyboardMouseTexts = FindDeepChild(root, "Texts_TecladoMouse")?.gameObject;

        if (gamepadTexts == null)
            gamepadTexts = FindDeepChild(root, "Texts_Controle")?.gameObject;

        if (keyboardMouseTabButton == null)
            keyboardMouseTabButton = FindDeepChild(root, "KeyboardEMouse")?.GetComponent<Button>();

        if (gamepadTabButton == null)
            gamepadTabButton = FindDeepChild(root, "Controle")?.GetComponent<Button>();
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
