using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// Tela de Continuar (livro de saves): seleção de slot → preview → Carregar / Deletar.
/// Modo separado do <see cref="MenuTabController"/>; ESC ou bookmark Voltar volta para Levels.
/// </summary>
[DisallowMultipleComponent]
public class ContinueSavePanelController : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject savePanelRoot;
    [SerializeField] private GameObject levelsTab;
    [SerializeField] private MenuTabController menuTabController;

    [Header("Slots e preview")]
    [SerializeField] private MenuContractVisualConfig contractVisuals;
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text slotInfoText;
    [SerializeField] private Button[] slotButtons = new Button[GameSaveData.MaxSlots];

    [Header("Ações")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    [Header("Confirmação de exclusão")]
    [SerializeField] private GameObject deleteConfirmationRoot;
    [SerializeField] private TMP_Text deleteConfirmationText;
    [SerializeField] private Button deleteConfirmButton;
    [SerializeField] private Button deleteCancelButton;

    [Header("Bookmarks")]
    [SerializeField] private MenuBookmarkVisualConfig bookmarkVisuals;

    [Header("Visual do slot selecionado")]
    [SerializeField] private Color slotSelectedTint = new Color(0.83f, 0.98f, 0.85f, 1f);
    [SerializeField] private Color slotNormalTint = Color.white;
    [SerializeField] private Color slotDisabledTint = new Color(0.72f, 0.72f, 0.72f, 1f);

    private readonly List<Image> _slotImages = new List<Image>(GameSaveData.MaxSlots);
    private int? _selectedSlot;
    private int? _pendingDeleteSlot;
    private bool _isOpen;
    private MainMenuController _mainMenu;
    private BookmarkEntry[] _bookmarks = Array.Empty<BookmarkEntry>();
    private string _sairHubLabel;

    [Serializable]
    private class BookmarkEntry
    {
        public string id;
        public GameObject root;
        public Image graphic;
        public Button button;
        public TMP_Text label;
        public Sprite hubSprite;
        public bool tuckInContinueMode;
        public bool backInContinueMode;
    }

    private void Awake()
    {
        _mainMenu = GetComponent<MainMenuController>();
        if (_mainMenu == null)
            _mainMenu = FindFirstObjectByType<MainMenuController>();

        ResolveMissingReferences();
        EnsureClickableButtons();
        DisableStaticLocalizationOnDeleteText();
        WireButtons();
        CacheSlotImages();
        ApplyHubBookmarkMode();

        if (savePanelRoot != null)
            savePanelRoot.SetActive(false);

        HideDeleteConfirmation();
        ResetPreviewToPlaceholder();
    }

    private void OnEnable()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.OnProfileChanged += HandleProfileChanged;
    }

    private void OnDisable()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.OnProfileChanged -= HandleProfileChanged;
    }

    private void Update()
    {
        if (!_isOpen || deleteConfirmationRoot != null && deleteConfirmationRoot.activeSelf)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public bool IsOpen => _isOpen;

    public bool IsSaveScreenVisible => savePanelRoot != null && savePanelRoot.activeSelf;

    public static bool TryHandleMenuBack()
    {
        ContinueSavePanelController panel = FindFirstObjectByType<ContinueSavePanelController>();
        if (panel == null || !panel.IsSaveScreenVisible)
            return false;

        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", "continue_back");
        panel.Close();
        return true;
    }

    public void Open()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.HasAnyHostSave())
            return;

        _isOpen = true;
        _selectedSlot = null;
        HideDeleteConfirmation();
        ResetPreviewToPlaceholder();

        if (savePanelRoot != null)
            savePanelRoot.SetActive(true);

        SetOtherTabsInactive();
        ApplyContinueBookmarkMode();
        RefreshSlotButtons();
        SetActionButtonsInteractable(false);
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        _selectedSlot = null;
        HideDeleteConfirmation();

        if (savePanelRoot != null)
            savePanelRoot.SetActive(false);

        if (menuTabController != null && levelsTab != null)
            menuTabController.OpenTab(levelsTab);
        else if (levelsTab != null)
            levelsTab.SetActive(true);

        ApplyHubBookmarkMode();
        ResetPreviewToPlaceholder();
    }

    public void RefreshFromStore()
    {
        RefreshSlotButtons();

        if (!_isOpen)
            return;

        if (_selectedSlot.HasValue)
        {
            SaveProfileStore save = SaveProfileStore.Instance;
            if (save == null || !save.CanContinue(_selectedSlot.Value))
            {
                _selectedSlot = null;
                ResetPreviewToPlaceholder();
                SetActionButtonsInteractable(false);
            }
            else
                ApplySlotPreview(_selectedSlot.Value);
        }

        UpdateSlotSelectionVisuals();
    }

    private void HandleProfileChanged()
    {
        RefreshFromStore();

        if (_isOpen && SaveProfileStore.Instance != null && !SaveProfileStore.Instance.HasAnyHostSave())
            Close();
    }

    private void ResolveMissingReferences()
    {
        Transform canvas = FindCanvasRoot();
        if (canvas == null)
            return;

        if (savePanelRoot == null)
            savePanelRoot = FindChildGameObject(canvas, "Save");

        if (levelsTab == null)
            levelsTab = FindChildGameObject(canvas, "Levels");

        if (menuTabController == null)
            menuTabController = FindFirstObjectByType<MenuTabController>();

        if (previewImage == null && savePanelRoot != null)
            previewImage = FindChildComponent<Image>(savePanelRoot.transform, "Image");

        if (slotInfoText == null && savePanelRoot != null)
            slotInfoText = FindChildComponent<TMP_Text>(savePanelRoot.transform, "Stage_Name_Selected");

        if (loadButton == null && savePanelRoot != null)
        {
            GameObject go = FindChildGameObject(savePanelRoot.transform, "Btn_Load");
            if (go != null)
                loadButton = EnsureButton(go);
        }

        if (deleteButton == null && savePanelRoot != null)
        {
            GameObject go = FindChildGameObject(savePanelRoot.transform, "Btn_Delete");
            if (go != null)
                deleteButton = EnsureButton(go);
        }

        ResolveSlotButtons(savePanelRoot != null ? savePanelRoot.transform : canvas);
        ResolveDeleteConfirmation(canvas);

        if (contractVisuals == null)
            contractVisuals = Resources.Load<MenuContractVisualConfig>("MenuContractVisualConfig");

        if (bookmarkVisuals == null)
            bookmarkVisuals = Resources.Load<MenuBookmarkVisualConfig>("MenuBookmarkVisualConfig");

        CleanupStaleContinueBookmarkRoot(canvas);
        ResolveBookmarks(canvas);
    }

    private static Transform FindCanvasRoot()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        return canvasGo != null ? canvasGo.transform : null;
    }

    private void ResolveSlotButtons(Transform searchRoot)
    {
        if (searchRoot == null)
            return;

        string[] names = { "Btn_Save1", "Btn_Save2", "Btn_Save3" };
        for (int i = 0; i < slotButtons.Length && i < names.Length; i++)
        {
            if (slotButtons[i] != null)
                continue;

            Transform t = searchRoot.Find(names[i]);
            if (t == null)
                t = FindDeepChild(searchRoot, names[i]);

            if (t != null)
                slotButtons[i] = EnsureButton(t.gameObject);
        }
    }

    private void ResolveDeleteConfirmation(Transform canvas)
    {
        Transform saveRoot = savePanelRoot != null ? savePanelRoot.transform : canvas;
        if (deleteConfirmationRoot == null && saveRoot != null)
            deleteConfirmationRoot = FindChildGameObject(saveRoot, "SaveDeleteConfirmation");

        if (deleteConfirmationRoot == null)
            return;

        if (deleteConfirmationText == null)
            deleteConfirmationText = FindChildComponent<TMP_Text>(deleteConfirmationRoot.transform, "DeleteConfirmationText");

        if (deleteConfirmButton == null)
        {
            GameObject go = FindChildGameObject(deleteConfirmationRoot.transform, "Btn_ConfirmDelete");
            if (go != null)
                deleteConfirmButton = EnsureButton(go);
        }

        if (deleteCancelButton == null)
        {
            GameObject go = FindChildGameObject(deleteConfirmationRoot.transform, "Btn_CancelDelete");
            if (go != null)
                deleteCancelButton = EnsureButton(go);
        }
    }

    private void CleanupStaleContinueBookmarkRoot(Transform canvas)
    {
        Transform stale = FindDeepChild(canvas, "BookmartsContinue");
        if (stale != null)
            Destroy(stale.gameObject);
    }

    private void ResolveBookmarks(Transform canvas)
    {
        if (_bookmarks.Length > 0)
            return;

        Transform bookmarts = FindDeepChild(canvas, "Bookmarts");
        if (bookmarts == null)
            return;

        var list = new List<BookmarkEntry>(6);
        RegisterBookmark(list, bookmarts, "NewGame", tuckInContinue: true);
        RegisterBookmark(list, bookmarts, "Continuar", tuckInContinue: true);
        RegisterBookmark(list, bookmarts, "Settings", tuckInContinue: true);
        RegisterBookmark(list, bookmarts, "Credits", tuckInContinue: true);
        RegisterBookmark(list, bookmarts, "Sair", tuckInContinue: false, backInContinue: true);
        _bookmarks = list.ToArray();
    }

    private void RegisterBookmark(
        List<BookmarkEntry> list,
        Transform bookmarts,
        string bookmarkId,
        bool tuckInContinue,
        bool backInContinue = false)
    {
        Transform bookmark = bookmarts.Find(bookmarkId);
        if (bookmark == null)
            return;

        Image graphic = bookmark.GetComponent<Image>();
        list.Add(new BookmarkEntry
        {
            id = bookmarkId,
            root = bookmark.gameObject,
            graphic = graphic,
            button = bookmark.GetComponent<Button>(),
            label = bookmark.GetComponentInChildren<TMP_Text>(true),
            hubSprite = graphic != null ? graphic.sprite : null,
            tuckInContinueMode = tuckInContinue,
            backInContinueMode = backInContinue
        });
    }

    private void EnsureClickableButtons()
    {
        if (loadButton != null)
            loadButton = EnsureButton(loadButton.gameObject);
        if (deleteButton != null)
            deleteButton = EnsureButton(deleteButton.gameObject);
        if (deleteConfirmButton != null)
            deleteConfirmButton = EnsureButton(deleteConfirmButton.gameObject);
        if (deleteCancelButton != null)
            deleteCancelButton = EnsureButton(deleteCancelButton.gameObject);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
                slotButtons[i] = EnsureButton(slotButtons[i].gameObject);
        }
    }

    private static Button EnsureButton(GameObject go)
    {
        Button button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();

        Image image = go.GetComponent<Image>();
        if (image != null)
            button.targetGraphic = image;

        return button;
    }

    private void DisableStaticLocalizationOnDeleteText()
    {
        if (deleteConfirmationText == null)
            return;

        LocalizeStringEvent localize = deleteConfirmationText.GetComponent<LocalizeStringEvent>();
        if (localize != null)
            localize.enabled = false;
    }

    private void WireButtons()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null)
                continue;

            int slot = i;
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => SelectSlot(slot));
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(LoadSelectedSave);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(RequestDeleteSelectedSave);
        }

        if (deleteConfirmButton != null)
        {
            deleteConfirmButton.onClick.RemoveAllListeners();
            deleteConfirmButton.onClick.AddListener(ConfirmDelete);
        }

        if (deleteCancelButton != null)
        {
            deleteCancelButton.onClick.RemoveAllListeners();
            deleteCancelButton.onClick.AddListener(HideDeleteConfirmation);
        }
    }

    private void CacheSlotImages()
    {
        _slotImages.Clear();
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null)
            {
                _slotImages.Add(null);
                continue;
            }

            _slotImages.Add(slotButtons[i].GetComponent<Image>());
        }
    }

    private void SetOtherTabsInactive()
    {
        if (menuTabController != null && levelsTab != null)
        {
            foreach (GameObject tab in FindMenuTabs())
            {
                if (tab != null && tab != savePanelRoot)
                    tab.SetActive(false);
            }

            return;
        }

        if (levelsTab != null)
            levelsTab.SetActive(false);
    }

    private IEnumerable<GameObject> FindMenuTabs()
    {
        if (menuTabController == null)
            yield break;

        // MenuTabController keeps private array — mirror via known siblings under Canvas.
        Transform canvas = levelsTab != null ? levelsTab.transform.parent : null;
        if (canvas == null)
            yield break;

        for (int i = 0; i < canvas.childCount; i++)
        {
            Transform child = canvas.GetChild(i);
            if (child.name is "Levels" or "Setings" or "Upgrades" or "Controls")
                yield return child.gameObject;
        }
    }

    private void SelectSlot(int slot)
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.CanContinue(slot))
            return;

        _selectedSlot = slot;
        ApplySlotPreview(slot);
        SetActionButtonsInteractable(true);
        UpdateSlotSelectionVisuals();
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"save_slot_select_{slot + 1}");
    }

    private void ApplySlotPreview(int slot)
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        GameSaveData data = save != null ? save.PeekSlot(slot) : null;
        if (data == null)
        {
            ResetPreviewToPlaceholder();
            return;
        }

        DateTime played = new DateTime(data.lastPlayedUtcTicks, DateTimeKind.Utc).ToLocalTime();
        if (slotInfoText != null)
        {
            slotInfoText.text = UiLocalization.FormatSaveSlotInfo(
                slot + 1,
                played.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
                played.ToString("HH:mm", CultureInfo.CurrentCulture));
        }

        if (previewImage != null && contractVisuals != null)
        {
            previewImage.color = Color.white;
            int contractIndex = MenuContractVisualConfig.ResolvePreviewContractIndex(data);
            Sprite sprite = contractVisuals.GetContractSprite(contractIndex);
            if (sprite != null)
                previewImage.sprite = sprite;
        }
    }

    private void ResetPreviewToPlaceholder()
    {
        if (slotInfoText != null)
            slotInfoText.text = UiLocalization.Get("saveFiles.prompt.selectSlot", "Selecione um arquivo");

        if (previewImage != null && contractVisuals != null)
        {
            previewImage.color = contractVisuals.EmptyPreviewTint;
            Sprite empty = contractVisuals.GetEmptyPreviewSprite();
            if (empty != null)
                previewImage.sprite = empty;
        }
    }

    private void RefreshSlotButtons()
    {
        SaveProfileStore save = SaveProfileStore.Instance;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            Button button = slotButtons[i];
            if (button == null)
                continue;

            bool canContinue = save != null && save.CanContinue(i);
            button.interactable = canContinue;
            button.gameObject.SetActive(true);

            Image image = i < _slotImages.Count ? _slotImages[i] : button.GetComponent<Image>();
            if (image != null)
                image.color = canContinue ? slotNormalTint : slotDisabledTint;
        }

        UpdateSlotSelectionVisuals();
    }

    private void UpdateSlotSelectionVisuals()
    {
        for (int i = 0; i < _slotImages.Count; i++)
        {
            Image image = _slotImages[i];
            if (image == null)
                continue;

            Button button = i < slotButtons.Length ? slotButtons[i] : null;
            if (button != null && !button.interactable)
            {
                image.color = slotDisabledTint;
                continue;
            }

            image.color = _selectedSlot.HasValue && _selectedSlot.Value == i
                ? slotSelectedTint
                : slotNormalTint;
        }
    }

    private void SetActionButtonsInteractable(bool interactable)
    {
        if (loadButton != null)
            loadButton.interactable = interactable;
        if (deleteButton != null)
            deleteButton.interactable = interactable;
    }

    private void LoadSelectedSave()
    {
        if (!_selectedSlot.HasValue)
            return;

        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        int slot = _selectedSlot.Value;
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !save.CanContinue(slot))
            return;

        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"continue_slot_{slot + 1}");
        GameSessionContext.BeginContinue(slot);
        save.LoadOrCreate(slot);

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(SceneFlowRouteIds.MenuToLobby);
        else
            ScreenFlowController.Instance?.RequestRoute(SceneFlowRouteIds.MenuToLobby);
    }

    private void RequestDeleteSelectedSave()
    {
        if (!_selectedSlot.HasValue)
            return;

        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        int slot = _selectedSlot.Value;
        if (save == null || !save.CanContinue(slot))
            return;

        GameSaveData data = save.PeekSlot(slot);
        if (data == null)
            return;

        _pendingDeleteSlot = slot;
        ShowDeleteConfirmation(slot, data);
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"delete_save_prompt_slot_{slot + 1}");
    }

    private void ShowDeleteConfirmation(int slot, GameSaveData data)
    {
        DateTime played = new DateTime(data.lastPlayedUtcTicks, DateTimeKind.Utc).ToLocalTime();
        if (deleteConfirmationText != null)
        {
            deleteConfirmationText.text = UiLocalization.FormatSaveDeletePrompt(
                slot + 1,
                played.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
                played.ToString("HH:mm", CultureInfo.CurrentCulture));
        }

        if (deleteConfirmationRoot != null)
        {
            deleteConfirmationRoot.transform.SetAsLastSibling();
            deleteConfirmationRoot.SetActive(true);
        }
    }

    private void HideDeleteConfirmation()
    {
        _pendingDeleteSlot = null;
        if (deleteConfirmationRoot != null)
            deleteConfirmationRoot.SetActive(false);
    }

    private void ConfirmDelete()
    {
        if (GameFlowOrchestrator.Instance != null && !GameFlowOrchestrator.Instance.CanRequestTransition())
        {
            HideDeleteConfirmation();
            return;
        }

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null || !_pendingDeleteSlot.HasValue)
        {
            HideDeleteConfirmation();
            return;
        }

        int slot = _pendingDeleteSlot.Value;
        save.DeleteSlot(slot);
        MidnightMeowAnalyticsTracker.NotifyUiClick("main_menu", $"delete_save_confirmed_slot_{slot + 1}");

        HideDeleteConfirmation();
        _selectedSlot = null;
        ResetPreviewToPlaceholder();
        SetActionButtonsInteractable(false);
        RefreshSlotButtons();
    }

    private void ApplyContinueBookmarkMode()
    {
        ApplyBookmarkMode(continueMode: true);
    }

    private void ApplyHubBookmarkMode()
    {
        ApplyBookmarkMode(continueMode: false);
    }

    private void ApplyBookmarkMode(bool continueMode)
    {
        for (int i = 0; i < _bookmarks.Length; i++)
        {
            BookmarkEntry entry = _bookmarks[i];
            if (entry?.root == null)
                continue;

            if (entry.backInContinueMode)
            {
                ApplyBackBookmark(entry, continueMode);
                continue;
            }

            if (!entry.tuckInContinueMode)
                continue;

            ApplyTuckedBookmark(entry, continueMode);
        }
    }

    private void ApplyBackBookmark(BookmarkEntry entry, bool continueMode)
    {
        entry.root.SetActive(true);

        if (continueMode)
        {
            if (entry.graphic != null)
            {
                Sprite backSprite = bookmarkVisuals != null ? bookmarkVisuals.ContinueBackBookmarkSprite : null;
                if (backSprite != null)
                    entry.graphic.sprite = backSprite;
            }

            if (entry.label != null)
            {
                if (string.IsNullOrEmpty(_sairHubLabel))
                    _sairHubLabel = entry.label.text;

                entry.label.text = UiLocalization.Get("btn.back", "Voltar");
            }
        }
        else
        {
            if (entry.graphic != null && entry.hubSprite != null)
                entry.graphic.sprite = entry.hubSprite;

            if (entry.label != null && !string.IsNullOrEmpty(_sairHubLabel))
                entry.label.text = _sairHubLabel;
        }

        if (entry.button != null)
            entry.button.interactable = true;
    }

    private void ApplyTuckedBookmark(BookmarkEntry entry, bool continueMode)
    {
        entry.root.SetActive(true);

        if (continueMode)
        {
            Sprite tucked = bookmarkVisuals != null ? bookmarkVisuals.GetTuckedSprite(entry.id) : null;
            if (entry.graphic != null && tucked != null)
                entry.graphic.sprite = tucked;

            if (entry.button != null)
                entry.button.interactable = false;
        }
        else
        {
            if (entry.graphic != null && entry.hubSprite != null)
                entry.graphic.sprite = entry.hubSprite;

            if (entry.button != null)
                entry.button.interactable = true;
        }
    }

    private static GameObject FindChildGameObject(Transform root, string childName)
    {
        Transform t = FindDeepChild(root, childName);
        return t != null ? t.gameObject : null;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform t = FindDeepChild(root, childName);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
