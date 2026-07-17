//--------------------------------
// FEITO POR: PEDRO CAURIO
// DESCRICAO: Tela de personagens: consulta (menu/lobby) ou seleção + upgrades (preparação).
// --------------------------------

using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharactersScreenController : MonoBehaviour
{
    private const string PanelHub = "hub";
    private const string PanelSkillsNyxie = "skills_nyxie";
    private const string PanelSkillsCora = "skills_cora";

    [SerializeField] private CharacterAbilitySet nixAbilitySet;
    [SerializeField] private CharacterAbilitySet coraAbilitySet;
    [SerializeField] private int upgradeCostPerTier = 2;

    [SerializeField] private TMP_Text magiculasText;
    [SerializeField] private Button nixSelectButton;
    [SerializeField] private Button coraSelectButton;
    [SerializeField] private Button nixSkillsButton;
    [SerializeField] private Button coraSkillsButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private TMP_Text feedbackText;

    [SerializeField] private GameObject[] hubRoots = System.Array.Empty<GameObject>();
    [SerializeField] private GameObject skillsNyxieRoot;
    [SerializeField] private GameObject skillsCoraRoot;

    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private CharacterSkillsPanel _nixSkillsPanel;
    private CharacterSkillsPanel _coraSkillsPanel;
    private CharacterPortraitVisual _nixPortraitVisual;
    private CharacterPortraitVisual _coraPortraitVisual;
    private TMP_Text[] _magiculasTexts = System.Array.Empty<TMP_Text>();
    private string _currentPanelId = PanelHub;
    private CharactersSessionManager _subscribedCharactersSession;
    private PreparationSessionManager _subscribedPreparationSession;

    private bool IsBrowseMode =>
        GameSessionContext.CharactersMode == GameSessionContext.CharactersScreenMode.UpgradesOnly;

    private bool AllowSelection =>
        GameSessionContext.CharactersMode == GameSessionContext.CharactersScreenMode.SelectionAllowed
        && GameSessionContext.CharactersOrigin == GameSessionContext.CharactersScreenOrigin.Preparation;

    private void Awake()
    {
        if (buildPlaceholderIfMissing && magiculasText == null)
            BuildPlaceholderUI();

        ResolveAbilitySets();
        BindSceneReferences();
        SetupSkillsPanels();
        SetupPortraitVisuals();
        WireButtons();
        ShowPanel(PanelHub);
        ApplyMenuButtonFeedback();
    }

    private void ApplyMenuButtonFeedback()
    {
        UiButtonFeedbackUtility.ApplyToScene(gameObject.scene);
    }

    private void ResolveAbilitySets()
    {
        if (nixAbilitySet == null)
            nixAbilitySet = FindAbilitySetByName("NixAbilitySet");
        if (coraAbilitySet == null)
            coraAbilitySet = FindAbilitySetByName("CoraAbilitySet");
    }

    private static CharacterAbilitySet FindAbilitySetByName(string assetName)
    {
        CharacterAbilitySet[] sets = Resources.FindObjectsOfTypeAll<CharacterAbilitySet>();
        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i] != null && sets[i].name == assetName)
                return sets[i];
        }

        return null;
    }

    private void OnEnable()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.OnProfileChanged += RefreshView;

        CharactersSessionManager.OnInstanceAvailable += TrySubscribeSessions;
        PreparationSessionManager.OnInstanceAvailable += TrySubscribeSessions;
        TrySubscribeSessions();
        RefreshView();
        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
        ApplyMenuButtonFeedback();
    }

    private void OnDisable()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.OnProfileChanged -= RefreshView;

        CharactersSessionManager.OnInstanceAvailable -= TrySubscribeSessions;
        PreparationSessionManager.OnInstanceAvailable -= TrySubscribeSessions;
        UnsubscribeSessions();
    }

    private void TrySubscribeSessions()
    {
        CharactersSessionManager session = CharactersSessionManager.Instance;
        if (session != null && session != _subscribedCharactersSession)
        {
            if (_subscribedCharactersSession != null)
            {
                _subscribedCharactersSession.OnCharactersStateChanged -= RefreshView;
                _subscribedCharactersSession.OnCharactersFeedback -= ShowFeedback;
            }

            session.OnCharactersStateChanged += RefreshView;
            session.OnCharactersFeedback += ShowFeedback;
            _subscribedCharactersSession = session;
            RefreshView();
        }

        PreparationSessionManager prep = PreparationSessionManager.Instance;
        if (prep != null && prep != _subscribedPreparationSession)
        {
            if (_subscribedPreparationSession != null)
            {
                _subscribedPreparationSession.OnPreparationStateChanged -= RefreshView;
                _subscribedPreparationSession.OnPreparationFeedback -= ShowFeedback;
            }

            prep.OnPreparationStateChanged += RefreshView;
            prep.OnPreparationFeedback += ShowFeedback;
            _subscribedPreparationSession = prep;
            RefreshView();
        }
    }

    private void UnsubscribeSessions()
    {
        if (_subscribedCharactersSession != null)
        {
            _subscribedCharactersSession.OnCharactersStateChanged -= RefreshView;
            _subscribedCharactersSession.OnCharactersFeedback -= ShowFeedback;
            _subscribedCharactersSession = null;
        }

        if (_subscribedPreparationSession != null)
        {
            _subscribedPreparationSession.OnPreparationStateChanged -= RefreshView;
            _subscribedPreparationSession.OnPreparationFeedback -= ShowFeedback;
            _subscribedPreparationSession = null;
        }
    }

    private void BindSceneReferences()
    {
        Transform canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        if (hubRoots == null || hubRoots.Length == 0)
        {
            hubRoots = new[]
            {
                canvas.Find("Titles")?.gameObject,
                canvas.Find("Buttons")?.gameObject,
                canvas.Find("Bookmarkets")?.gameObject,
                canvas.Find("Nyxie_Images")?.gameObject,
                canvas.Find("Cora_Images")?.gameObject,
            };
        }

        if (skillsNyxieRoot == null)
            skillsNyxieRoot = canvas.Find("Skils_Nyxie")?.gameObject;
        if (skillsCoraRoot == null)
            skillsCoraRoot = canvas.Find("Skils_Cora")?.gameObject;

        if (nixSkillsButton == null)
            nixSkillsButton = canvas.Find("Buttons/Nyxie's Skill")?.GetComponent<Button>();
        if (coraSkillsButton == null)
            coraSkillsButton = canvas.Find("Buttons/Cora's Skill")?.GetComponent<Button>();

        if (nixSelectButton == null)
            nixSelectButton = EnsurePortraitButton(canvas.Find("Nyxie_Images"));
        if (coraSelectButton == null)
            coraSelectButton = EnsurePortraitButton(canvas.Find("Cora_Images"));

        WirePortraitSelectButtons(canvas.Find("Nyxie_Images"), LobbyCharacterType.CharacterA);
        WirePortraitSelectButtons(canvas.Find("Cora_Images"), LobbyCharacterType.CharacterB);

        BindMagiculasTexts(canvas);

        CleanupPortraitRootBlockers(canvas.Find("Nyxie_Images"));
        CleanupPortraitRootBlockers(canvas.Find("Cora_Images"));

        if (backButton == null)
            backButton = canvas.Find("Bookmarkets/Btn_Voltar")?.GetComponent<Button>();

        if (readyButton == null)
            readyButton = canvas.Find("Bookmarkets")?.Find("Btn_Pronto")?.GetComponent<Button>()
                ?? canvas.Find("Bookmarkets")?.GetComponentInChildren<Button>(true);

        EnsureHubRaycastOrder(canvas);
    }

    private void BindMagiculasTexts(Transform canvas)
    {
        if (canvas == null)
            return;

        if (magiculasText != null)
        {
            _magiculasTexts = new[] { magiculasText };
            return;
        }

        var found = new List<TMP_Text>();
        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.gameObject.name == "magiculasText")
                found.Add(text);
        }

        _magiculasTexts = found.ToArray();
        if (_magiculasTexts.Length > 0)
            magiculasText = _magiculasTexts[0];
    }

    private void WirePortraitSelectButtons(Transform portraitRoot, LobbyCharacterType type)
    {
        if (portraitRoot == null)
            return;

        WirePortraitChildButton(portraitRoot.Find("Desselected"), type);
        WirePortraitChildButton(portraitRoot.Find("Selected"), type);
        WirePortraitChildButton(portraitRoot.Find("Animation"), type);
    }

    private void WirePortraitChildButton(Transform child, LobbyCharacterType type)
    {
        if (child == null)
            return;

        Button button = child.GetComponent<Button>();
        if (button == null)
        {
            button = child.gameObject.AddComponent<Button>();
            Image image = child.GetComponent<Image>();
            if (image != null)
                button.targetGraphic = image;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnCharacterSelect(type));
    }

    private static Transform FindSceneCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.scene.name == "Characters")
                return canvas.transform;
        }

        return null;
    }

    private static void CleanupPortraitRootBlockers(Transform portraitRoot)
    {
        if (portraitRoot == null)
            return;

        Transform hitChild = portraitRoot.Find("Desselected");
        Button rootButton = portraitRoot.GetComponent<Button>();
        if (rootButton != null && hitChild != null && rootButton.gameObject == portraitRoot.gameObject)
            Object.Destroy(rootButton);

        Image rootImage = portraitRoot.GetComponent<Image>();
        if (rootImage != null)
            rootImage.raycastTarget = false;
    }

    private static Button EnsurePortraitButton(Transform portraitRoot)
    {
        if (portraitRoot == null)
            return null;

        Transform hitTarget = portraitRoot.Find("Desselected") ?? portraitRoot;

        Button button = hitTarget.GetComponent<Button>();
        if (button != null)
            return button;

        button = hitTarget.gameObject.AddComponent<Button>();
        Image image = hitTarget.GetComponent<Image>();
        if (image != null)
            button.targetGraphic = image;

        return button;
    }

    private static void EnsureHubRaycastOrder(Transform canvas)
    {
        if (canvas == null)
            return;

        DisableRaycastOnRoot(canvas.Find("Nyxie_Images"));
        DisableRaycastOnRoot(canvas.Find("Cora_Images"));

        Transform cora = canvas.Find("Cora_Images");
        Transform nyxie = canvas.Find("Nyxie_Images");
        if (nyxie != null)
            nyxie.SetAsLastSibling();
        if (cora != null)
            cora.SetAsLastSibling();
    }

    private static void DisableRaycastOnRoot(Transform root)
    {
        if (root == null)
            return;

        Image image = root.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }

    private void SetupSkillsPanels()
    {
        _nixSkillsPanel = EnsureSkillsPanel(skillsNyxieRoot, LobbyCharacterType.CharacterA);
        _coraSkillsPanel = EnsureSkillsPanel(skillsCoraRoot, LobbyCharacterType.CharacterB);
    }

    private CharacterSkillsPanel EnsureSkillsPanel(GameObject root, LobbyCharacterType character)
    {
        if (root == null)
            return null;

        CharacterSkillsPanel panel = root.GetComponent<CharacterSkillsPanel>();
        if (panel == null)
            panel = root.AddComponent<CharacterSkillsPanel>();

        panel.ExitRequested -= ShowHub;
        panel.ExitRequested += ShowHub;
        panel.UpgradeRequested -= TryUpgradeSlot;
        panel.UpgradeRequested += TryUpgradeSlot;
        panel.Bind(this, character, upgradeCostPerTier, IsBrowseMode);
        return panel;
    }

    private void SetupPortraitVisuals()
    {
        Transform canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        _nixPortraitVisual = EnsurePortraitVisual(canvas.Find("Nyxie_Images"));
        _coraPortraitVisual = EnsurePortraitVisual(canvas.Find("Cora_Images"));
    }

    private static CharacterPortraitVisual EnsurePortraitVisual(Transform root)
    {
        if (root == null)
            return null;

        CharacterPortraitVisual visual = root.GetComponent<CharacterPortraitVisual>();
        if (visual != null)
            return visual;

        return root.gameObject.AddComponent<CharacterPortraitVisual>();
    }

    private void WireButtons()
    {
        if (nixSkillsButton != null)
        {
            nixSkillsButton.onClick.RemoveAllListeners();
            nixSkillsButton.onClick.AddListener(() => ShowPanel(PanelSkillsNyxie));
        }

        if (coraSkillsButton != null)
        {
            coraSkillsButton.onClick.RemoveAllListeners();
            coraSkillsButton.onClick.AddListener(() => ShowPanel(PanelSkillsCora));
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(GoBack);
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveAllListeners();
        }
    }

    private void ShowPanel(string panelId)
    {
        _currentPanelId = panelId;
        bool showHub = panelId == PanelHub;
        bool showNix = panelId == PanelSkillsNyxie;
        bool showCora = panelId == PanelSkillsCora;

        SetRootsActive(hubRoots, showHub);
        if (skillsNyxieRoot != null)
            skillsNyxieRoot.SetActive(showNix);
        if (skillsCoraRoot != null)
            skillsCoraRoot.SetActive(showCora);

        RefreshMagiculasLabels();

        if (showNix)
            _nixSkillsPanel?.RefreshBars();
        else if (showCora)
            _coraSkillsPanel?.RefreshBars();
    }

    private void ShowHub()
    {
        ShowPanel(PanelHub);
    }

    private static void SetRootsActive(GameObject[] roots, bool active)
    {
        if (roots == null)
            return;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
                roots[i].SetActive(active);
        }
    }

    private void OnCharacterSelect(LobbyCharacterType type)
    {
        if (!AllowSelection)
            return;

        if (IsCharacterBlocked(type))
            return;

        if (GameSessionContext.IsSinglePlayer)
        {
            SaveProfileStore save = SaveProfileStore.Instance;
            save?.SetSelectedCharacter(type);
            LobbySelectionStore.CaptureSinglePlayer(type);
            RefreshView();
            ReturnToPreparationAfterSelection();
            return;
        }

        PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
        if (session == null)
        {
            ShowFeedback(LocaleText.IsPortuguese()
                ? "Aguardando sessão de rede..."
                : "Waiting for network session...");
            return;
        }

        session.RequestSetCharacterRpc((byte)type);
        RefreshView();
        ReturnToPreparationAfterSelection();
    }

    private void ReturnToPreparationAfterSelection()
    {
        if (!AllowSelection)
            return;

        GoBack();
    }

    private void TryUpgradeSlot(AbilitySlot slot)
    {
        if (IsBrowseMode)
            return;

        LobbyCharacterType character = _currentPanelId == PanelSkillsCora
            ? LobbyCharacterType.CharacterB
            : LobbyCharacterType.CharacterA;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null)
            return;

        CharacterSaveData data = save.Active.GetCharacterData(character);
        int currentTier = data.GetTierForSlot(slot);
        if (currentTier >= 3)
            return;

        if (!save.TrySpendMagiculas(upgradeCostPerTier))
        {
            ShowFeedback(LocaleText.IsPortuguese()
                ? "Magículas insuficientes."
                : "Not enough magículas.");
            return;
        }

        data.SetTierForSlot(slot, currentTier + 1);
        save.SaveActive();
        RefreshView();
    }

    private void GoBack()
    {
        string route = string.IsNullOrEmpty(GameSessionContext.ReturnRouteId)
            ? SceneFlowRouteIds.ReturnToMenu
            : GameSessionContext.ReturnRouteId;

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(route);
        else
            ScreenFlowController.Instance?.RequestRoute(route);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }

    private void RefreshView()
    {
        bool browse = IsBrowseMode;
        _nixSkillsPanel?.SetBrowseMode(browse);
        _coraSkillsPanel?.SetBrowseMode(browse);

        RefreshMagiculasLabels();
        RefreshCharacterPortraits();
        RefreshReadyUi();
        _nixSkillsPanel?.RefreshBars();
        _coraSkillsPanel?.RefreshBars();
    }

    private void RefreshMagiculasLabels()
    {
        if (_magiculasTexts == null || _magiculasTexts.Length == 0)
        {
            if (magiculasText != null)
                _magiculasTexts = new[] { magiculasText };
            else
                BindMagiculasTexts(FindSceneCanvas());
        }

        SaveProfileStore save = SaveProfileStore.Instance;
        int magiculaCount = save?.Active?.magiculas ?? 0;
        string magiculaLabel = UiLocalization.FormatMagiculaCount(magiculaCount);

        for (int i = 0; i < _magiculasTexts.Length; i++)
        {
            TMP_Text text = _magiculasTexts[i];
            if (text == null)
                continue;

            // Sempre visível nas telas de skills (pai Skils_* controla se aparece).
            text.gameObject.SetActive(true);
            text.text = magiculaLabel;
        }
    }

    private void RefreshReadyUi()
    {
        // PRONTO fica na tela Preparation; Characters só escolhe personagem/upgrades.
        if (readyButton != null)
            readyButton.gameObject.SetActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (readyStatusText != null)
            readyStatusText.text = string.Empty;
    }

    private void RefreshCharacterPortraits()
    {
        LobbyCharacterType local = ResolveLocalSelection();
        bool allowSelect = AllowSelection;
        Transform canvas = FindSceneCanvas();

        bool nixInteractable = allowSelect && !IsCharacterBlocked(LobbyCharacterType.CharacterA);
        bool coraInteractable = allowSelect && !IsCharacterBlocked(LobbyCharacterType.CharacterB);

        if (nixSelectButton != null)
            nixSelectButton.interactable = nixInteractable;
        if (coraSelectButton != null)
            coraSelectButton.interactable = coraInteractable;

        if (canvas != null)
        {
            SetPortraitInteractable(canvas.Find("Nyxie_Images"), nixInteractable);
            SetPortraitInteractable(canvas.Find("Cora_Images"), coraInteractable);
        }

        ApplyPortraitState(_nixPortraitVisual, LobbyCharacterType.CharacterA, local, allowSelect);
        ApplyPortraitState(_coraPortraitVisual, LobbyCharacterType.CharacterB, local, allowSelect);
    }

    private static void SetPortraitInteractable(Transform portraitRoot, bool interactable)
    {
        if (portraitRoot == null)
            return;

        SetChildButtonInteractable(portraitRoot.Find("Desselected"), interactable);
        SetChildButtonInteractable(portraitRoot.Find("Selected"), interactable);
        SetChildButtonInteractable(portraitRoot.Find("Animation"), interactable);
    }

    private static void SetChildButtonInteractable(Transform child, bool interactable)
    {
        if (child == null)
            return;

        Button button = child.GetComponent<Button>();
        if (button != null)
            button.interactable = interactable;
    }

    private void ApplyPortraitState(
        CharacterPortraitVisual visual,
        LobbyCharacterType type,
        LobbyCharacterType localSelection,
        bool allowSelect)
    {
        if (visual == null)
            return;

        if (!allowSelect)
        {
            visual.SetBaseState(CharacterPortraitVisual.PortraitState.Deselected);
            return;
        }

        if (localSelection == type)
        {
            visual.SetBaseState(CharacterPortraitVisual.PortraitState.Selected);
            return;
        }

        if (IsCharacterBlocked(type))
            visual.SetBaseState(CharacterPortraitVisual.PortraitState.TakenByOther);
        else
            visual.SetBaseState(CharacterPortraitVisual.PortraitState.Deselected);
    }

    private bool IsCharacterBlocked(LobbyCharacterType type)
    {
        if (!AllowSelection || GameSessionContext.IsSinglePlayer)
            return false;

        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        return HubSessionStateReader.IsCharacterTakenByOther(localId, type);
    }

    private LobbyCharacterType ResolveLocalSelection()
    {
        if (GameSessionContext.IsSinglePlayer)
        {
            if (LobbySelectionStore.TryGetCharacter(0, out LobbyCharacterType selected))
                return selected;

            if (AllowSelection)
                return LobbyCharacterType.Default;

            SaveProfileStore save = SaveProfileStore.Instance;
            if (save != null)
                return save.GetSelectedCharacter();

            return LobbyCharacterType.Default;
        }

        return HubSessionStateReader.GetLocalCharacterType();
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "CharactersPanel", new Color(0.05f, 0.05f, 0.08f, 0.96f));

        magiculasText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "0", 32,
            TextAlignmentOptions.TopRight, Color.white,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -20f), new Vector2(-8f, -8f));

        nixSelectButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Nixie",
            new Vector2(0.2f, 0.72f), new Vector2(0.2f, 0.72f), new Vector2(-160f, -50f), new Vector2(160f, 50f));
        coraSelectButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Cora",
            new Vector2(0.8f, 0.72f), new Vector2(0.8f, 0.72f), new Vector2(-160f, -50f), new Vector2(160f, 50f));

        nixSkillsButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Nix Skills",
            new Vector2(0.2f, 0.52f), new Vector2(0.2f, 0.52f), new Vector2(-130f, -28f), new Vector2(130f, 28f));
        coraSkillsButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Cora Skills",
            new Vector2(0.8f, 0.52f), new Vector2(0.8f, 0.52f), new Vector2(-130f, -28f), new Vector2(130f, 28f));

        feedbackText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "", 22,
            TextAlignmentOptions.Bottom, new Color(0.9f, 0.75f, 0.75f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-300f, 110f), new Vector2(300f, 150f));

        backButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Voltar",
            new Vector2(0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(-100f, -35f), new Vector2(100f, 35f));

        readyButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Pronto",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-220f, 40f), new Vector2(-40f, 100f));

        countdownText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "", 36,
            TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -30f), new Vector2(180f, 30f));

        readyStatusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "", 24,
            TextAlignmentOptions.Bottom, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 110f), new Vector2(280f, 150f));
    }
}
