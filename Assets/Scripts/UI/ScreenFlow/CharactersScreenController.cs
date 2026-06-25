using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de personagens: consulta (menu/lobby) ou seleção + upgrades (preparação).
/// </summary>
[DisallowMultipleComponent]
public class CharactersScreenController : MonoBehaviour
{
    [SerializeField] private CharacterAbilitySet nixAbilitySet;
    [SerializeField] private CharacterAbilitySet coraAbilitySet;
    [SerializeField] private int upgradeCostPerTier = 2;

    [SerializeField] private TMP_Text magiculasText;
    [SerializeField] private Button nixSelectButton;
    [SerializeField] private Button coraSelectButton;
    [SerializeField] private Button nixSkill1Button;
    [SerializeField] private Button nixSkill2Button;
    [SerializeField] private Button nixSkill3Button;
    [SerializeField] private Button coraSkill1Button;
    [SerializeField] private Button coraSkill2Button;
    [SerializeField] private Button coraSkill3Button;
    [SerializeField] private GameObject skillPopup;
    [SerializeField] private TMP_Text skillPopupTitle;
    [SerializeField] private TMP_Text skillPopupDescription;
    [SerializeField] private TMP_Text skillPopupLevel;
    [SerializeField] private Button skillPopupUpgradeButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private TMP_Text feedbackText;

    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private LobbyCharacterType _popupCharacter = LobbyCharacterType.CharacterA;
    private AbilitySlot _popupSlot = AbilitySlot.Ability1;
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
        WireButtons();
        HidePopup();
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

    private void WireButtons()
    {
        if (nixSelectButton != null) nixSelectButton.onClick.AddListener(() => OnCharacterSelect(LobbyCharacterType.CharacterA));
        if (coraSelectButton != null) coraSelectButton.onClick.AddListener(() => OnCharacterSelect(LobbyCharacterType.CharacterB));
        if (nixSkill1Button != null) nixSkill1Button.onClick.AddListener(() => OpenSkillPopup(LobbyCharacterType.CharacterA, AbilitySlot.Ability1));
        if (nixSkill2Button != null) nixSkill2Button.onClick.AddListener(() => OpenSkillPopup(LobbyCharacterType.CharacterA, AbilitySlot.Ability2));
        if (nixSkill3Button != null) nixSkill3Button.onClick.AddListener(() => OpenSkillPopup(LobbyCharacterType.CharacterA, AbilitySlot.PrimaryAttack));
        if (coraSkill1Button != null) coraSkill1Button.onClick.AddListener(() => OpenSkillPopup(LobbyCharacterType.CharacterB, AbilitySlot.Ability1));
        if (coraSkill2Button != null) coraSkill2Button.onClick.AddListener(() => OpenSkillPopup(LobbyCharacterType.CharacterB, AbilitySlot.Ability2));
        if (coraSkill3Button != null) coraSkill3Button.onClick.AddListener(() => OpenSkillPopup(LobbyCharacterType.CharacterB, AbilitySlot.PrimaryAttack));
        if (skillPopupUpgradeButton != null) skillPopupUpgradeButton.onClick.AddListener(TryUpgradeFromPopup);
        if (backButton != null) backButton.onClick.AddListener(GoBack);
        if (readyButton != null) readyButton.onClick.AddListener(ToggleReady);
    }

    private void ToggleReady()
    {
        if (!AllowSelection)
            return;

        if (GameSessionContext.IsSinglePlayer)
        {
            LobbyCharacterType selected = ResolveLocalSelection();
            if (selected == LobbyCharacterType.Default)
            {
                ShowFeedback("Escolha um personagem antes de confirmar.");
                return;
            }

            LobbySelectionStore.CaptureSinglePlayer(selected);
            ContractSceneResolver.ApplyToSession(ContractSceneResolver.ResolveActiveContractIndex());
            ScreenFlowStateMachine.BeginGameplayLoading();
            return;
        }

        PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
        if (session == null)
        {
            ShowFeedback("Aguardando sessão de rede...");
            return;
        }

        bool targetReady = !session.GetLocalReadyState();
        session.RequestSetReadyRpc(targetReady);
        RefreshView();
    }

    private void ApplySinglePlayerContractScene()
    {
        ContractSceneResolver.ApplyToSession(ContractSceneResolver.ResolveActiveContractIndex());
    }

    private void OnCharacterSelect(LobbyCharacterType type)
    {
        if (!AllowSelection)
            return;

        if (GameSessionContext.IsSinglePlayer)
        {
            SaveProfileStore save = SaveProfileStore.Instance;
            save?.SetSelectedCharacter(type);
            LobbySelectionStore.CaptureSinglePlayer(type);
            RefreshView();
            return;
        }

        PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
        if (session == null)
        {
            ShowFeedback("Aguardando sessão de rede...");
            return;
        }

        session.RequestSetCharacterRpc((byte)type);
        RefreshView();
    }

    private void OpenSkillPopup(LobbyCharacterType character, AbilitySlot slot)
    {
        _popupCharacter = character;
        _popupSlot = slot;

        if (skillPopup != null)
            skillPopup.SetActive(true);

        RefreshPopupContent();
    }

    private void HidePopup()
    {
        if (skillPopup != null)
            skillPopup.SetActive(false);
    }

    private void TryUpgradeFromPopup()
    {
        if (IsBrowseMode)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null)
            return;

        CharacterSaveData data = save.Active.GetCharacterData(_popupCharacter);
        int currentTier = data.GetTierForSlot(_popupSlot);
        if (currentTier >= 3)
            return;

        if (!save.TrySpendMagiculas(upgradeCostPerTier))
        {
            ShowFeedback("Magículas insuficientes.");
            return;
        }

        data.SetTierForSlot(_popupSlot, currentTier + 1);
        save.SaveActive();
        RefreshPopupContent();
        RefreshView();
    }

    private void GoBack()
    {
        HidePopup();

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

        if (magiculasText != null)
            magiculasText.gameObject.SetActive(!browse);

        if (!browse)
        {
            SaveProfileStore save = SaveProfileStore.Instance;
            if (magiculasText != null)
                magiculasText.text = $"{save?.Active?.magiculas ?? 0}";
        }

        RefreshCharacterButtons();
        UpdateSkillButtonLabels();
        RefreshReadyUi();
    }

    private void RefreshReadyUi()
    {
        bool showReady = AllowSelection;

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(showReady);
            if (showReady && !GameSessionContext.IsSinglePlayer)
            {
                PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
                TMP_Text label = readyButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = session != null && session.GetLocalReadyState() ? "Desmarcar Pronto" : "Pronto";
            }
            else if (showReady)
            {
                TMP_Text label = readyButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = "Pronto";
            }
        }

        if (countdownText != null)
        {
            if (!showReady || GameSessionContext.IsSinglePlayer)
            {
                countdownText.gameObject.SetActive(false);
                return;
            }

            PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
            int countdown = session != null ? session.StartCountdown : -1;
            bool visible = countdown >= 0;
            countdownText.gameObject.SetActive(visible);
            if (visible)
                countdownText.text = countdown > 0 ? $"Iniciando em {countdown}..." : "Iniciando!";
        }

        if (readyStatusText != null && showReady && !GameSessionContext.IsSinglePlayer)
        {
            PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
            if (session == null)
            {
                readyStatusText.text = string.Empty;
                return;
            }

            int readyCount = 0;
            for (int i = 0; i < session.Players.Count; i++)
            {
                if (session.Players[i].IsReady)
                    readyCount++;
            }

            readyStatusText.text = $"Prontos: {readyCount}/{session.Players.Count}";
        }
        else if (readyStatusText != null)
        {
            readyStatusText.text = string.Empty;
        }
    }

    private void RefreshCharacterButtons()
    {
        LobbyCharacterType local = ResolveLocalSelection();
        bool allowSelect = AllowSelection;

        if (nixSelectButton != null)
        {
            nixSelectButton.interactable = allowSelect && !IsCharacterBlocked(LobbyCharacterType.CharacterA);
            SetPanelHighlight(nixSelectButton, local == LobbyCharacterType.CharacterA);
            UpdateCharacterOwnershipLabel(nixSelectButton, LobbyCharacterType.CharacterA, local);
        }

        if (coraSelectButton != null)
        {
            coraSelectButton.interactable = allowSelect && !IsCharacterBlocked(LobbyCharacterType.CharacterB);
            SetPanelHighlight(coraSelectButton, local == LobbyCharacterType.CharacterB);
            UpdateCharacterOwnershipLabel(coraSelectButton, LobbyCharacterType.CharacterB, local);
        }
    }

    private bool IsCharacterBlocked(LobbyCharacterType type)
    {
        if (!AllowSelection || GameSessionContext.IsSinglePlayer)
            return false;

        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        return HubSessionStateReader.IsCharacterTakenByOther(localId, type);
    }

    private void UpdateCharacterOwnershipLabel(Button button, LobbyCharacterType type, LobbyCharacterType localSelection)
    {
        if (button == null || !AllowSelection || GameSessionContext.IsSinglePlayer)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label == null)
            return;

        string baseName = type == LobbyCharacterType.CharacterB ? "Cora" : "Nixie";
        ulong? ownerId = HubSessionStateReader.FindCharacterOwnerId(type);
        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

        if (localSelection == type)
            label.text = $"{baseName}\n(Você)";
        else if (ownerId.HasValue && ownerId.Value != localId)
            label.text = $"{baseName}\n(Jogador {ownerId.Value + 1})";
        else
            label.text = baseName;
    }

    private LobbyCharacterType ResolveLocalSelection()
    {
        if (GameSessionContext.IsSinglePlayer)
        {
            if (LobbySelectionStore.TryGetCharacter(0, out LobbyCharacterType selected))
                return selected;

            SaveProfileStore save = SaveProfileStore.Instance;
            if (save != null)
                return save.GetSelectedCharacter();

            return LobbyCharacterType.Default;
        }

        return HubSessionStateReader.GetLocalCharacterType();
    }

    private void UpdateSkillButtonLabels()
    {
        SetSkillLabel(nixSkill1Button, LobbyCharacterType.CharacterA, AbilitySlot.Ability1);
        SetSkillLabel(nixSkill2Button, LobbyCharacterType.CharacterA, AbilitySlot.Ability2);
        SetSkillLabel(nixSkill3Button, LobbyCharacterType.CharacterA, AbilitySlot.PrimaryAttack);
        SetSkillLabel(coraSkill1Button, LobbyCharacterType.CharacterB, AbilitySlot.Ability1);
        SetSkillLabel(coraSkill2Button, LobbyCharacterType.CharacterB, AbilitySlot.Ability2);
        SetSkillLabel(coraSkill3Button, LobbyCharacterType.CharacterB, AbilitySlot.PrimaryAttack);
    }

    private void SetSkillLabel(Button button, LobbyCharacterType character, AbilitySlot slot)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label == null)
            return;

        CharacterAbilitySet set = character == LobbyCharacterType.CharacterB ? coraAbilitySet : nixAbilitySet;
        string name = ResolveSkillName(set, slot);

        if (IsBrowseMode)
        {
            label.text = name;
            return;
        }

        SaveProfileStore save = SaveProfileStore.Instance;
        int tier = save?.Active?.GetCharacterData(character).GetTierForSlot(slot) ?? 0;
        label.text = $"{name} (Nv.{tier})";
    }

    private void RefreshPopupContent()
    {
        CharacterAbilitySet set = _popupCharacter == LobbyCharacterType.CharacterB ? coraAbilitySet : nixAbilitySet;
        string skillName = ResolveSkillName(set, _popupSlot);
        string description = ResolveSkillDescription(set, _popupSlot);

        if (skillPopupTitle != null)
            skillPopupTitle.text = skillName;

        if (skillPopupDescription != null)
            skillPopupDescription.text = description;

        bool browse = IsBrowseMode;
        SaveProfileStore save = SaveProfileStore.Instance;
        int tier = browse ? 0 : save?.Active?.GetCharacterData(_popupCharacter).GetTierForSlot(_popupSlot) ?? 0;

        if (skillPopupLevel != null)
            skillPopupLevel.text = browse ? "Modo consulta" : $"Nível: {tier}/3";

        if (skillPopupUpgradeButton != null)
        {
            skillPopupUpgradeButton.gameObject.SetActive(!browse);
            skillPopupUpgradeButton.interactable = !browse && tier < 3 && (save?.Active?.magiculas ?? 0) >= upgradeCostPerTier;
            TMP_Text upgradeLabel = skillPopupUpgradeButton.GetComponentInChildren<TMP_Text>();
            if (upgradeLabel != null)
                upgradeLabel.text = $"Upgrade ({upgradeCostPerTier} magículas)";
        }
    }

    private static string ResolveSkillName(CharacterAbilitySet set, AbilitySlot slot)
    {
        if (set == null)
            return slot.ToString();

        return slot switch
        {
            AbilitySlot.Ability1 => set.ability1 != null ? set.ability1.displayName : "Skill 1",
            AbilitySlot.Ability2 => set.ability2 != null ? set.ability2.displayName : "Skill 2",
            _ => "Ataque Normal"
        };
    }

    private static string ResolveSkillDescription(CharacterAbilitySet set, AbilitySlot slot)
    {
        if (set == null)
            return "Descrição placeholder.";

        CharacterAbilityDefinition def = slot switch
        {
            AbilitySlot.Ability1 => set.ability1,
            AbilitySlot.Ability2 => set.ability2,
            _ => null
        };

        if (def == null)
            return "Melhora o ataque básico do personagem.";

        AbilityTierData tier = def.GetTierData(1);
        return $"Alcance: {tier.range:0.##} | CD: {tier.cooldown:0.##}s";
    }

    private static void SetPanelHighlight(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected ? new Color(0.75f, 0.15f, 0.15f, 0.95f) : new Color(0.18f, 0.18f, 0.22f, 0.95f);
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

        nixSkill1Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Nix S1",
            new Vector2(0.2f, 0.52f), new Vector2(0.2f, 0.52f), new Vector2(-130f, -28f), new Vector2(130f, 28f));
        nixSkill2Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Nix S2",
            new Vector2(0.2f, 0.4f), new Vector2(0.2f, 0.4f), new Vector2(-130f, -28f), new Vector2(130f, 28f));
        nixSkill3Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Nix S3",
            new Vector2(0.2f, 0.28f), new Vector2(0.2f, 0.28f), new Vector2(-130f, -28f), new Vector2(130f, 28f));

        coraSkill1Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Cora S1",
            new Vector2(0.8f, 0.52f), new Vector2(0.8f, 0.52f), new Vector2(-130f, -28f), new Vector2(130f, 28f));
        coraSkill2Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Cora S2",
            new Vector2(0.8f, 0.4f), new Vector2(0.8f, 0.4f), new Vector2(-130f, -28f), new Vector2(130f, 28f));
        coraSkill3Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Cora S3",
            new Vector2(0.8f, 0.28f), new Vector2(0.8f, 0.28f), new Vector2(-130f, -28f), new Vector2(130f, 28f));

        skillPopup = new GameObject("SkillPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        skillPopup.transform.SetParent(panel.transform, false);
        RectTransform popupRt = skillPopup.GetComponent<RectTransform>();
        popupRt.anchorMin = new Vector2(0.5f, 0.5f);
        popupRt.anchorMax = new Vector2(0.5f, 0.5f);
        popupRt.offsetMin = new Vector2(-280f, -200f);
        popupRt.offsetMax = new Vector2(280f, 200f);
        skillPopup.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

        skillPopupTitle = ScreenFlowPlaceholderFactory.CreateText(skillPopup.transform, "Skill", 32,
            TextAlignmentOptions.Top, Color.white,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -70f), new Vector2(-20f, -10f));
        skillPopupDescription = ScreenFlowPlaceholderFactory.CreateText(skillPopup.transform, "Descrição", 22,
            TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0f, 0.35f), new Vector2(1f, 0.85f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
        skillPopupLevel = ScreenFlowPlaceholderFactory.CreateText(skillPopup.transform, "Nível", 22,
            TextAlignmentOptions.BottomLeft, Color.white,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 60f), new Vector2(-20f, 100f));
        skillPopupUpgradeButton = ScreenFlowPlaceholderFactory.CreateButton(skillPopup.transform, "Upgrade",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 20f), new Vector2(140f, 70f));

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
