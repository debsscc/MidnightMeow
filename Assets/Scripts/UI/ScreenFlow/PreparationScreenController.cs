using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hub de preparação: escolha de fase/contrato, personagem (via Characters) e confirmação de pronto.
/// </summary>
[DisallowMultipleComponent]
public class PreparationScreenController : MonoBehaviour
{
    private const int ContractCount = 3;
    private const string SelectedPhaseHintName = "SelectedPhaseHint";
    private const float SelectedPhaseHintFontSize = 20f;
    private static readonly Color IconSelectedColor = Color.white;
    private static readonly Color IconDeselectedColor = new(0.55f, 0.5f, 0.48f, 1f);

    [SerializeField] private ContractDefinition[] contracts;
    [SerializeField] private Button[] contractButtons;
    [SerializeField] private GameObject[] contractCompletionBadges = System.Array.Empty<GameObject>();
    [SerializeField] private GameObject[] contractPreviewImages = System.Array.Empty<GameObject>();
    [SerializeField] private Image nixieIcon;
    [SerializeField] private Image coraIcon;
    [Tooltip("Nix_Selecionado — nenhum jogador escolheu Nix.")]
    [SerializeField] private Sprite nixieIconDefaultSprite;
    [Tooltip("Nix_Selecionado (1) — Nix escolhido (local ou outro jogador).")]
    [SerializeField] private Sprite nixieIconChosenSprite;
    [Tooltip("Cora_Selecionada — nenhum jogador escolheu Cora.")]
    [SerializeField] private Sprite coraIconDefaultSprite;
    [Tooltip("Cora_Selecionada (1) — Cora escolhida (local ou outro jogador).")]
    [SerializeField] private Sprite coraIconChosenSprite;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Button chooseCharacterButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private int _localSelectedContract = -1;
    private bool _buttonsWired;
    private bool _previewZoomWired;
    private bool _showSelectedPhaseHint;
    private PreparationSessionManager _subscribedSession;
    private UiSimpleImageZoomOverlay _imageZoomOverlay;
    private TMP_Text[] _phaseSelectedHints;

    private void Awake()
    {
        ResolveContracts();
        BindSceneReferences();
        EnsureUi();
        WireButtons();
        WireContractPreviewZoom();
        ApplyMenuButtonFeedback();
    }

    private void ApplyMenuButtonFeedback()
    {
        UiButtonFeedbackUtility.ApplyToScene(gameObject.scene);
    }

    private void Start()
    {
        EnsureUi();
        if (!_buttonsWired)
            WireButtons();

        WireContractPreviewZoom();
        ApplyMenuButtonFeedback();
        EnsureDefaultContract();
        RefreshView();
        ScreenFlowSceneReadiness.MarkReadyIfPending("Preparation");
        EnsureTransitionOverlayCleared();
    }

    /// <summary>
    /// Cliente NGO pode chegar com Fade DDOL opaco se o fade-in perdeu a race.
    /// </summary>
    private static void EnsureTransitionOverlayCleared()
    {
        ScreenFlowController flow = ScreenFlowController.Instance;
        if (flow != null && flow.IsTransitioning)
            return;

        TransitionFadeOverlay overlay = TransitionFadeOverlay.Instance;
        if (overlay == null)
            return;

        if (overlay.GetFadeAlpha() > 0.01f)
            flow?.ForceClearTransitionOverlay();
    }

    private void EnsureUi()
    {
        // Só gera placeholder se a cena não tiver os botões de fase (evita UI duplicada sobreposta).
        if (HasScenePhaseButtons())
            return;

        bool missingUi = contractButtons == null || contractButtons.Length == 0 || readyButton == null;
        if (buildPlaceholderIfMissing && missingUi)
            BuildPlaceholderUI();
    }

    private bool HasScenePhaseButtons()
    {
        if (contractButtons != null)
        {
            for (int i = 0; i < contractButtons.Length; i++)
            {
                if (contractButtons[i] != null)
                    return true;
            }
        }

        Transform canvas = FindSceneCanvas();
        Transform buttonsDir = canvas != null ? FindDeep(canvas, "Buttons_Dir") : null;
        return buttonsDir != null && buttonsDir.Find("Fase 1") != null;
    }

    private void ResolveContracts()
    {
        if (contracts == null || contracts.Length < ContractCount)
            contracts = new ContractDefinition[ContractCount];

        ContractSceneResolver.FillMissingSlots(contracts);
    }

    public void RefreshFromHubNavigation()
    {
        RestoreSinglePlayerContractState();
        TrySubscribeSession();
        RefreshView();
    }

    private void RestoreSinglePlayerContractState()
    {
        if (!GameSessionContext.IsSinglePlayer)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active == null || save.Active.selectedContractIndex < 0)
            return;

        _localSelectedContract = save.Active.selectedContractIndex;
    }

    private void OnEnable()
    {
        PreparationSessionManager.OnInstanceAvailable += TrySubscribeSession;
        TrySubscribeSession();
        EnsureDefaultContract();
        RefreshView();
        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
        SelectDefaultPreparationControl();
    }

    private void SelectDefaultPreparationControl()
    {
        if (readyButton != null && readyButton.isActiveAndEnabled && readyButton.IsInteractable())
        {
            UiSelectionUtility.Select(readyButton);
            return;
        }

        if (contractButtons != null)
        {
            for (int i = 0; i < contractButtons.Length; i++)
            {
                Button b = contractButtons[i];
                if (b != null && b.isActiveAndEnabled && b.IsInteractable())
                {
                    UiSelectionUtility.Select(b);
                    return;
                }
            }
        }

        if (chooseCharacterButton != null)
            UiSelectionUtility.Select(chooseCharacterButton);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshView));
        PreparationSessionManager.OnInstanceAvailable -= TrySubscribeSession;
        UnsubscribeSession();
        if (_imageZoomOverlay != null && _imageZoomOverlay.IsOpen)
            _imageZoomOverlay.Close();
    }

    private void TrySubscribeSession()
    {
        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session == null || session == _subscribedSession)
            return;

        UnsubscribeSession();
        session.OnPreparationStateChanged += RefreshView;
        session.OnPreparationFeedback += ShowFeedback;
        _subscribedSession = session;
        RefreshView();
    }

    private void UnsubscribeSession()
    {
        if (_subscribedSession == null)
            return;

        _subscribedSession.OnPreparationStateChanged -= RefreshView;
        _subscribedSession.OnPreparationFeedback -= ShowFeedback;
        _subscribedSession = null;
    }

    private void BindSceneReferences()
    {
        Transform canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        Transform buttonsDir = FindDeep(canvas, "Buttons_Dir");
        if (buttonsDir != null && (contractButtons == null || contractButtons.Length == 0))
        {
            contractButtons = new Button[ContractCount];
            contractCompletionBadges = new GameObject[ContractCount];
            for (int i = 0; i < ContractCount; i++)
            {
                string phaseName = $"Fase {i + 1}";
                Transform phase = buttonsDir.Find(phaseName);
                if (phase == null)
                    continue;

                contractButtons[i] = phase.GetComponent<Button>();
                Transform badge = phase.Find("Selected_Badge");
                if (badge != null)
                    contractCompletionBadges[i] = badge.gameObject;
            }
        }

        Transform contractImagesRoot = FindDeep(canvas, "Contract_images");
        if (contractImagesRoot != null && (contractPreviewImages == null || contractPreviewImages.Length == 0))
        {
            contractPreviewImages = new GameObject[ContractCount];
            for (int i = 0; i < ContractCount; i++)
            {
                Transform preview = contractImagesRoot.Find($"Contract{i + 1}");
                if (preview != null)
                    contractPreviewImages[i] = preview.gameObject;
            }
        }

        Transform iconsRoot = FindDeep(canvas, "Icons_Characters");
        if (iconsRoot != null)
        {
            if (nixieIcon == null)
                nixieIcon = iconsRoot.Find("Nyxie")?.GetComponent<Image>();
            if (coraIcon == null)
                coraIcon = iconsRoot.Find("Cora")?.GetComponent<Image>();
        }

        if (chooseCharacterButton == null)
            chooseCharacterButton = FindDeep(canvas, "ChooseCharacter")?.GetComponent<Button>();

        if (readyButton == null)
            readyButton = FindDeep(canvas, "Ready")?.GetComponent<Button>();

        if (backButton == null)
            backButton = FindDeep(canvas, "Btn_Back")?.GetComponent<Button>();

        if (leaveLobbyButton == null)
            leaveLobbyButton = FindDeep(canvas, "Lobby")?.GetComponent<Button>();
    }

    private static Transform FindSceneCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.scene.name == "Preparation")
                return canvas.transform;
        }

        return null;
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void EnsureDefaultContract()
    {
        if (_localSelectedContract >= 0)
            return;

        if (!GameSessionContext.IsSinglePlayer)
        {
            PreparationSessionManager session = PreparationSessionManager.Instance;
            if (session != null && session.SelectedContractIndex >= 0)
            {
                _localSelectedContract = session.SelectedContractIndex;
                return;
            }
        }

        if (GameSessionContext.IsSinglePlayer)
        {
            SaveProfileStore save = SaveProfileStore.Instance;
            if (save?.Active != null && save.Active.selectedContractIndex >= 0)
            {
                _localSelectedContract = save.Active.selectedContractIndex;
                ContractSceneResolver.ApplyToSession(_localSelectedContract);
                return;
            }
        }

        if (!IsContractUnlocked(0))
            return;

        _localSelectedContract = 0;
        ContractSceneResolver.ApplyToSession(0);

        if (GameSessionContext.IsSinglePlayer)
        {
            SaveProfileStore save = SaveProfileStore.Instance;
            if (save?.Active != null)
            {
                save.Active.selectedContractIndex = 0;
                save.SaveActive();
            }
        }
        else
        {
            SyncDefaultContractToServerIfHost(0);
        }
    }

    private static void SyncDefaultContractToServerIfHost(int contractIndex)
    {
        if (!IsLocalHost())
            return;

        PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
        if (session == null || !session.IsServer || session.SelectedContractIndex >= 0)
            return;

        session.SetContractIndexOnServer(contractIndex);
    }

    private void WireButtons()
    {
        if (_buttonsWired)
            return;

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(ToggleReady);
            readyButton.onClick.AddListener(ToggleReady);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBackToMenu);
            backButton.onClick.AddListener(GoBackToMenu);
        }

        if (leaveLobbyButton != null)
        {
            leaveLobbyButton.onClick.RemoveListener(LeaveLobby);
            leaveLobbyButton.onClick.AddListener(LeaveLobby);
        }

        if (chooseCharacterButton != null)
        {
            chooseCharacterButton.onClick.RemoveListener(OnChooseCharacter);
            chooseCharacterButton.onClick.AddListener(OnChooseCharacter);
        }

        if (contractButtons == null)
            return;

        for (int i = 0; i < contractButtons.Length; i++)
        {
            int index = i;
            if (contractButtons[i] == null)
                continue;

            contractButtons[i].onClick.RemoveAllListeners();
            contractButtons[i].interactable = IsContractUnlocked(index) && IsLocalHost();
            contractButtons[i].onClick.AddListener(() =>
            {
                if (!IsContractUnlocked(index))
                {
                    ShowFeedback(ContractProgressionUtility.GetLockedReason(index, SaveProfileStore.Instance?.Active));
                    return;
                }

                if (!IsLocalHost())
                {
                    ShowFeedback(LocaleText.IsPortuguese()
                        ? "Apenas o host pode escolher a fase."
                        : "Only the host can choose the phase.");
                    return;
                }

                SelectContract(index);
            });

            EventTrigger trigger = contractButtons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = contractButtons[i].gameObject.AddComponent<EventTrigger>();

            AddHover(trigger, index);
        }

        _buttonsWired = true;
    }

    private void WireContractPreviewZoom()
    {
        if (_previewZoomWired)
            return;

        if (contractPreviewImages == null || contractPreviewImages.Length == 0)
            return;

        Transform canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        _imageZoomOverlay = UiSimpleImageZoomOverlay.EnsureOnCanvas(canvas);

        for (int i = 0; i < contractPreviewImages.Length; i++)
        {
            GameObject previewGo = contractPreviewImages[i];
            if (previewGo == null)
                continue;

            Image previewImage = previewGo.GetComponent<Image>()
                                 ?? previewGo.GetComponentInChildren<Image>(true);
            if (previewImage == null)
                continue;

            previewImage.raycastTarget = true;

            EventTrigger trigger = previewGo.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = previewGo.AddComponent<EventTrigger>();

            // Remove listeners antigos deste zoom (PointerClick com zoom).
            trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerClick);

            Image captured = previewImage;
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ =>
            {
                if (!captured.gameObject.activeInHierarchy)
                    return;

                _imageZoomOverlay?.ToggleFrom(captured);
            });
            trigger.triggers.Add(entry);
        }

        _previewZoomWired = true;
    }

    private static void AddHover(EventTrigger trigger, int index)
    {
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ =>
        {
            PreparationScreenController ctrl = UnityEngine.Object.FindFirstObjectByType<PreparationScreenController>();
            ctrl?.ShowTooltip(index);
        });
        trigger.triggers.Add(entry);
    }

    private void GoBackToMenu()
    {
        RequestNavigation(SceneFlowRouteIds.ReturnToMenu);
    }

    private void LeaveLobby()
    {
        RequestNavigation(SceneFlowRouteIds.ReturnToLobby);
    }

    private static void RequestNavigation(string routeId)
    {
        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(routeId);
        else
            ScreenFlowController.Instance?.RequestRoute(routeId);
    }

    private void OnChooseCharacter()
    {
        ScreenFlowStateMachine.OpenCharactersFromPreparation();
    }

    private static bool IsLocalHost()
    {
        if (GameSessionContext.IsSinglePlayer)
            return true;

        NetworkManager net = NetworkManager.Singleton;
        return net == null || net.IsServer;
    }

    private void SelectContract(int index)
    {
        if (!IsContractUnlocked(index) || !IsLocalHost())
            return;

        _localSelectedContract = index;
        _showSelectedPhaseHint = true;

        if (!GameSessionContext.IsSinglePlayer)
        {
            PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
            if (session == null)
            {
                ShowFeedback(LocaleText.IsPortuguese()
                    ? "Aguardando sessão de rede..."
                    : "Waiting for network session...");
                return;
            }

            if (session.IsServer)
                session.SetContractIndexOnServer(index);
            else
                session.RequestSelectContractRpc(index);

            session.RequestSetReadyRpc(false);
        }

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active != null)
        {
            save.Active.selectedContractIndex = index;
            save.SaveActive();
        }

        ContractSceneResolver.ApplyToSession(index);

        string gameplayScene = contracts != null && index >= 0 && index < contracts.Length && contracts[index] != null
            ? contracts[index].gameplaySceneName
            : string.Empty;
        MidnightMeowAnalyticsTracker.NotifyContractSelected(index, gameplayScene);
        MidnightMeowAnalyticsTracker.NotifyUiClick("preparation", $"select_contract_{index + 1}");

        RefreshView();
    }

    private void ToggleReady()
    {
        if (GameSessionContext.IsSinglePlayer)
        {
            string error = ValidateSinglePlayerReady();
            if (!string.IsNullOrEmpty(error))
            {
                ShowFeedback(error);
                return;
            }

            ApplyContractScene(_localSelectedContract);
            LobbySelectionStore.CaptureSinglePlayer(ResolveLocalCharacter());
            MidnightMeowAnalyticsTracker.NotifyUiClick("preparation", "ready");
            ScreenFlowStateMachine.BeginGameplayLoading();
            return;
        }

        PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
        if (session == null)
        {
            ShowFeedback(LocaleText.IsPortuguese()
                ? "Aguardando sessão de rede..."
                : "Waiting for network session...");
            RefreshView();
            return;
        }

        bool targetReady = !session.GetLocalReadyState();
        session.RequestSetReadyRpc(targetReady);
        MidnightMeowAnalyticsTracker.NotifyUiClick("preparation", targetReady ? "ready" : "unready");
        RefreshView();
    }

    private string ValidateSinglePlayerReady()
    {
        if (_localSelectedContract < 0)
            return LocaleText.IsPortuguese()
                ? "Escolha uma fase antes de confirmar."
                : "Choose a phase before confirming.";

        if (ResolveLocalCharacter() == LobbyCharacterType.Default)
            return LocaleText.IsPortuguese()
                ? "Escolha um personagem antes de confirmar."
                : "Choose a character before confirming.";

        return string.Empty;
    }

    private void ApplyContractScene(int index)
    {
        ContractSceneResolver.ApplyToSession(index);
    }

    private static bool IsContractUnlocked(int index)
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        return ContractProgressionUtility.IsContractUnlocked(index, save?.Active);
    }

    private static bool IsContractCompleted(int index)
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        return save?.Active != null && save.Active.IsContractCompleted(index);
    }

    private void ShowTooltip(int index)
    {
        if (tooltipText == null || contracts == null || index < 0 || index >= contracts.Length || contracts[index] == null)
            return;

        PositionTooltipBelowContract(index);

        if (!IsContractUnlocked(index))
        {
            tooltipText.text = $"{contracts[index].displayName}\n\n{ContractProgressionUtility.GetLockedReason(index, SaveProfileStore.Instance?.Active)}";
            return;
        }

        ContractDefinition contract = contracts[index];
        tooltipText.text = contract.description;
    }

    private void PositionTooltipBelowContract(int index)
    {
        if (tooltipText == null || contractButtons == null || index < 0 || index >= contractButtons.Length)
            return;

        Button button = contractButtons[index];
        if (button == null)
            return;

        RectTransform tooltipRt = tooltipText.rectTransform;
        RectTransform buttonRt = button.GetComponent<RectTransform>();
        if (tooltipRt == null || buttonRt == null)
            return;

        tooltipRt.anchorMin = new Vector2(0.5f, 0f);
        tooltipRt.anchorMax = new Vector2(0.5f, 0f);
        tooltipRt.pivot = new Vector2(0.5f, 1f);

        Vector3[] corners = new Vector3[4];
        buttonRt.GetWorldCorners(corners);
        float centerX = (corners[0].x + corners[2].x) * 0.5f;
        float bottomY = corners[0].y;

        RectTransform parentRt = tooltipRt.parent as RectTransform;
        if (parentRt == null)
            return;

        Camera uiCamera = tooltipText.canvas != null && tooltipText.canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tooltipText.canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, new Vector2(centerX, bottomY), uiCamera, out Vector2 localPoint))
        {
            tooltipRt.anchoredPosition = new Vector2(localPoint.x, localPoint.y - 12f);
            tooltipRt.sizeDelta = new Vector2(420f, 180f);
        }
    }

    private void ShowFeedback(string message)
    {
        if (readyStatusText != null)
            readyStatusText.text = message;

        if (!GameSessionContext.IsSinglePlayer && !string.IsNullOrEmpty(message))
            Invoke(nameof(RefreshView), 2f);
    }

    private LobbyCharacterType ResolveLocalCharacter()
    {
        if (GameSessionContext.IsSinglePlayer)
        {
            if (LobbySelectionStore.TryGetCharacter(0, out LobbyCharacterType selected)
                && selected != LobbyCharacterType.Default)
            {
                return selected;
            }

            SaveProfileStore save = SaveProfileStore.Instance;
            if (save != null)
                return save.GetSelectedCharacter();

            return LobbyCharacterType.Default;
        }

        return HubSessionStateReader.GetLocalCharacterType();
    }

    private void RefreshView()
    {
        EnsureDefaultContract();

        if (chooseCharacterButton != null)
            chooseCharacterButton.gameObject.SetActive(true);

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(true);
            bool hasContract = _localSelectedContract >= 0;
            if (!GameSessionContext.IsSinglePlayer)
            {
                PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
                if (session != null)
                    hasContract = session.SelectedContractIndex >= 0;
            }

            bool canReady = ResolveLocalCharacter() != LobbyCharacterType.Default && hasContract;
            readyButton.interactable = canReady;
        }

        RefreshContractCompletionBadges();
        RefreshContractPreviewImages();
        RefreshCharacterIcons();

        int selectedContract = _localSelectedContract;

        if (!GameSessionContext.IsSinglePlayer)
        {
            PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
            if (session != null)
                selectedContract = session.SelectedContractIndex;
        }

        if (selectedContract >= 0)
            HighlightSelectedContract(selectedContract);

        RefreshSelectedPhaseHints(selectedContract);
        ApplyContractButtonLabels();

        if (GameSessionContext.IsSinglePlayer)
        {
            RefreshSinglePlayerStatus();
            return;
        }

        RefreshMultiplayerStatus();
    }

    private void RefreshContractCompletionBadges()
    {
        if (contractCompletionBadges == null)
            return;

        for (int i = 0; i < contractCompletionBadges.Length; i++)
        {
            if (contractCompletionBadges[i] == null)
                continue;

            contractCompletionBadges[i].SetActive(IsContractCompleted(i));
        }
    }

    private void RefreshContractPreviewImages()
    {
        if (contractPreviewImages == null)
            return;

        int selected = _localSelectedContract;
        if (!GameSessionContext.IsSinglePlayer)
        {
            PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
            if (session != null && session.SelectedContractIndex >= 0)
                selected = session.SelectedContractIndex;
        }

        for (int i = 0; i < contractPreviewImages.Length; i++)
        {
            if (contractPreviewImages[i] == null)
                continue;

            bool show = selected < 0 || i == selected;
            contractPreviewImages[i].SetActive(show);
        }
    }

    private void RefreshCharacterIcons()
    {
        EnsureCharacterIconSprites();

        bool nixChosen = IsCharacterSelectedInSession(LobbyCharacterType.CharacterA);
        bool coraChosen = IsCharacterSelectedInSession(LobbyCharacterType.CharacterB);

        ApplyCharacterIcon(nixieIcon, nixChosen ? nixieIconChosenSprite : nixieIconDefaultSprite, nixChosen);
        ApplyCharacterIcon(coraIcon, coraChosen ? coraIconChosenSprite : coraIconDefaultSprite, coraChosen);
    }

    /// <summary>
    /// Solo: personagem local. MP: qualquer jogador da sessão que tenha escolhido esse tipo.
    /// </summary>
    private bool IsCharacterSelectedInSession(LobbyCharacterType type)
    {
        if (type == LobbyCharacterType.Default)
            return false;

        if (GameSessionContext.IsSinglePlayer)
            return ResolveLocalCharacter() == type;

        return HubSessionStateReader.FindCharacterOwnerId(type).HasValue;
    }

    private static void ApplyCharacterIcon(Image icon, Sprite sprite, bool chosen)
    {
        if (icon == null)
            return;

        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.color = Color.white;
            return;
        }

        icon.color = chosen ? IconSelectedColor : IconDeselectedColor;
    }

    private void EnsureCharacterIconSprites()
    {
        if (nixieIconDefaultSprite == null)
            nixieIconDefaultSprite = FindMenuContractSprite("Nix_Selecionado");
        if (nixieIconChosenSprite == null)
            nixieIconChosenSprite = FindMenuContractSprite("Nix_Selecionado (1)");
        if (coraIconDefaultSprite == null)
            coraIconDefaultSprite = FindMenuContractSprite("Cora_Selecionada");
        if (coraIconChosenSprite == null)
            coraIconChosenSprite = FindMenuContractSprite("Cora_Selecionada (1)");
    }

    /// <summary>
    /// Prefere nome exato (ou sufixo _0 de Multiple), evitando variantes Personagem/OutroPlayer/(1) indesejadas.
    /// </summary>
    private static Sprite FindMenuContractSprite(string spriteName)
    {
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        Sprite prefixFallback = null;
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
                continue;

            string name = sprite.name;
            if (name == spriteName || name == spriteName + "_0")
                return sprite;

            if (prefixFallback != null
                || !name.StartsWith(spriteName, StringComparison.Ordinal)
                || name.IndexOf("Personagem", StringComparison.Ordinal) >= 0
                || name.IndexOf("OutroPlayer", StringComparison.Ordinal) >= 0)
            {
                continue;
            }

            // Evita "Nix_Selecionado (1)" quando o pedido é "Nix_Selecionado".
            if (name.Length > spriteName.Length)
            {
                char next = name[spriteName.Length];
                if (next is ' ' or '(')
                    continue;
            }

            prefixFallback = sprite;
        }

        return prefixFallback;
    }

    private void RefreshSinglePlayerStatus()
    {
        if (readyStatusText == null)
            return;

        LobbyCharacterType character = ResolveLocalCharacter();
        bool pt = LocaleText.IsPortuguese();

        if (character == LobbyCharacterType.Default)
        {
            readyStatusText.text = pt
                ? "Escolha um personagem para jogar."
                : "Choose a character to play.";
            return;
        }

        string characterName = character == LobbyCharacterType.CharacterB ? "Cora" : "Nixie";
        readyStatusText.text = pt
            ? $"Personagem: {characterName}. Pressione Pronto para iniciar."
            : $"Character: {characterName}. Press Ready to start.";
    }

    private void RefreshMultiplayerStatus()
    {
        PreparationSessionManager session = HubSessionStateReader.GetPreparationSession();
        if (session == null)
        {
            if (readyStatusText != null)
                readyStatusText.text = LocaleText.IsPortuguese()
                    ? "Aguardando sessão de rede..."
                    : "Waiting for network session...";
            return;
        }

        if (readyStatusText == null)
            return;

        int readyCount = 0;
        int charCount = 0;
        for (int i = 0; i < session.Players.Count; i++)
        {
            if (session.Players[i].IsReady)
                readyCount++;
            if (session.Players[i].CharacterType != LobbyCharacterType.Default)
                charCount++;
        }

        bool pt = LocaleText.IsPortuguese();
        string localReadyLabel = session.GetLocalReadyState()
            ? (pt ? " (você pronto)" : " (you ready)")
            : string.Empty;

        if (session.SelectedContractIndex < 0)
        {
            readyStatusText.text = IsLocalHost()
                ? (pt ? "Escolha uma fase" : "Choose a phase")
                : (pt ? "Aguardando o host escolher a fase" : "Waiting for the host to choose the phase");
            return;
        }

        readyStatusText.text = pt
            ? $"Prontos: {readyCount}/{session.Players.Count} | Personagens: {charCount}/{session.Players.Count}{localReadyLabel}"
            : $"Ready: {readyCount}/{session.Players.Count} | Characters: {charCount}/{session.Players.Count}{localReadyLabel}";

        if (readyButton != null)
        {
            TMP_Text label = readyButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = session.GetLocalReadyState()
                    ? (pt ? "Desmarcar Pronto" : "Unready")
                    : (pt ? "Pronto" : "Ready");
            }
        }
    }

    private void ApplyContractButtonLabels()
    {
        if (contractButtons == null)
            return;

        for (int i = 0; i < contractButtons.Length && i < ContractCount; i++)
        {
            if (contractButtons[i] == null)
                continue;

            // Disponibilidade: interactable + cor (HighlightSelectedContract) + badge/tooltip.
            // Nunca injeta "(bloqueada)" no label — o layout da Preparação é single-line.
            contractButtons[i].interactable = IsContractUnlocked(i) && IsLocalHost();
            RestorePhaseLabel(contractButtons[i], i);
        }
    }

    private static void RestorePhaseLabel(Button button, int phaseIndex)
    {
        if (button == null)
            return;

        bool pt = LocaleText.IsPortuguese();
        string clean = pt ? $"FASE {phaseIndex + 1}" : $"PHASE {phaseIndex + 1}";

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || label.gameObject.name == SelectedPhaseHintName)
                continue;

            // Só mexe em textos de fase (ignora filhos sem relação, se houver).
            string current = label.text ?? string.Empty;
            bool looksLikePhaseLabel =
                current.IndexOf("FASE", System.StringComparison.OrdinalIgnoreCase) >= 0
                || current.IndexOf("PHASE", System.StringComparison.OrdinalIgnoreCase) >= 0
                || current.IndexOf("bloque", System.StringComparison.OrdinalIgnoreCase) >= 0
                || current.IndexOf("locked", System.StringComparison.OrdinalIgnoreCase) >= 0
                || string.IsNullOrWhiteSpace(current);

            if (!looksLikePhaseLabel)
                continue;

            label.text = clean;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void RefreshSelectedPhaseHints(int selectedIndex)
    {
        EnsurePhaseSelectedHints();
        if (_phaseSelectedHints == null)
            return;

        // Só após clique explícito em SelectContract — o default automático da fase 1 não conta.
        if (!_showSelectedPhaseHint)
            selectedIndex = -1;

        bool pt = LocaleText.IsPortuguese();
        string hint = pt ? "Fase Selecionada" : "Phase Selected";

        for (int i = 0; i < _phaseSelectedHints.Length; i++)
        {
            TMP_Text label = _phaseSelectedHints[i];
            if (label == null)
                continue;

            bool show = selectedIndex >= 0 && i == selectedIndex;
            label.gameObject.SetActive(show);
            if (!show)
                continue;

            label.text = hint;
        }
    }

    private void EnsurePhaseSelectedHints()
    {
        if (contractButtons == null)
            return;

        if (_phaseSelectedHints == null || _phaseSelectedHints.Length != contractButtons.Length)
            _phaseSelectedHints = new TMP_Text[contractButtons.Length];

        TMP_FontAsset font = ResolvePhaseUiFont();

        for (int i = 0; i < contractButtons.Length; i++)
        {
            Button button = contractButtons[i];
            if (button == null)
                continue;

            if (_phaseSelectedHints[i] != null)
            {
                ApplySelectedPhaseHintStyle(_phaseSelectedHints[i], font);
                continue;
            }

            Transform existing = button.transform.Find(SelectedPhaseHintName);
            TMP_Text label = existing != null ? existing.GetComponent<TMP_Text>() : null;
            if (label == null)
                label = CreateSelectedPhaseHint(button.transform, font);

            _phaseSelectedHints[i] = label;
            ApplySelectedPhaseHintStyle(label, font);
        }
    }

    private TMP_FontAsset ResolvePhaseUiFont()
    {
        if (contractButtons == null)
            return null;

        for (int i = 0; i < contractButtons.Length; i++)
        {
            Button button = contractButtons[i];
            if (button == null)
                continue;

            TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
            for (int j = 0; j < labels.Length; j++)
            {
                TMP_Text label = labels[j];
                if (label == null || label.gameObject.name == SelectedPhaseHintName || label.font == null)
                    continue;

                return label.font;
            }
        }

        return null;
    }

    private static TMP_Text CreateSelectedPhaseHint(Transform parent, TMP_FontAsset font)
    {
        var go = new GameObject(SelectedPhaseHintName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -6f);
        rt.sizeDelta = new Vector2(280f, 24f);

        TMP_Text label = go.AddComponent<TextMeshProUGUI>();
        ApplySelectedPhaseHintStyle(label, font);
        go.SetActive(false);
        return label;
    }

    private static void ApplySelectedPhaseHintStyle(TMP_Text label, TMP_FontAsset font)
    {
        if (label == null)
            return;

        if (font != null)
            label.font = font;

        label.fontSize = SelectedPhaseHintFontSize;
        label.fontStyle = FontStyles.Italic;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.enableAutoSizing = false;
    }

    private void HighlightSelectedContract(int index)
    {
        if (contractButtons == null)
            return;

        Color selected = new Color(0.95f, 0.85f, 0.45f, 1f);
        Color normal = Color.white;
        Color locked = new Color(0.65f, 0.65f, 0.65f, 0.85f);

        for (int i = 0; i < contractButtons.Length; i++)
        {
            if (contractButtons[i] == null)
                continue;

            Image image = contractButtons[i].GetComponent<Image>();
            if (image == null)
                continue;

            if (i == index)
                image.color = selected;
            else if (!IsContractUnlocked(i))
                image.color = locked;
            else
                image.color = normal;
        }
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        canvas.sortingOrder = 200;
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "PreparationPanel", new Color(0.06f, 0.06f, 0.08f, 0.96f));

        ResolveContracts();

        contractButtons = new Button[ContractCount];
        for (int i = 0; i < ContractCount; i++)
        {
            float x = 0.25f + i * 0.25f;
            string label = $"Fase {i + 1}";
            contractButtons[i] = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, label,
                new Vector2(x, 0.55f), new Vector2(x, 0.55f),
                new Vector2(-140f, -140f), new Vector2(140f, 140f));
        }

        tooltipText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Passe o mouse sobre a fase.", 22,
            TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 20f), new Vector2(210f, 200f));

        chooseCharacterButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Escolher Personagem",
            new Vector2(0.2f, 0.35f), new Vector2(0.2f, 0.35f), new Vector2(-160f, -40f), new Vector2(160f, 40f));

        backButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Voltar ao Menu",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 210f), new Vector2(260f, 270f));

        leaveLobbyButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Sair do Lobby",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 90f), new Vector2(260f, 150f));

        readyButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Pronto",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 40f), new Vector2(-40f, 100f));
        readyStatusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Escolha uma fase", 24,
            TextAlignmentOptions.Bottom, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 20f), new Vector2(280f, 60f));
    }
}
