using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hub de preparação: escolha de contrato, personagem e confirmação de pronto (sem ordem obrigatória).
/// </summary>
[DisallowMultipleComponent]
public class PreparationScreenController : MonoBehaviour
{
    private const int ContractCount = 3;

    [SerializeField] private ContractDefinition[] contracts;
    [SerializeField] private Button[] contractButtons;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private TMP_Text selectedCharacterText;
    [SerializeField] private Button chooseCharacterButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyStatusText;
    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private int _localSelectedContract = -1;
    private bool _localReady;
    private LobbyCharacterType _soloCharacter = LobbyCharacterType.Default;
    private bool _buttonsWired;

    private void Awake()
    {
        ResolveContracts();
        EnsureUi();
        WireButtons();
    }

    private void Start()
    {
        ScreenFlowController.Instance?.ClearTransitionOverlay();
        EnsureUi();
        if (!_buttonsWired)
            WireButtons();
        RefreshView();
    }

    private void EnsureUi()
    {
        bool missingUi = contractButtons == null || contractButtons.Length == 0
                         || readyButton == null || chooseCharacterButton == null;

        if (buildPlaceholderIfMissing && missingUi)
            BuildPlaceholderUI();
    }

    private void ResolveContracts()
    {
        if (contracts != null && contracts.Length >= ContractCount)
            return;

        ContractDefinition[] loaded = Resources.FindObjectsOfTypeAll<ContractDefinition>();
        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));

        contracts = new ContractDefinition[ContractCount];
        if (loaded.Length > 0)
            contracts[0] = loaded[0];

        for (int i = 0; i < ContractCount; i++)
        {
            if (contracts[i] != null)
                continue;

            contracts[i] = ScriptableObject.CreateInstance<ContractDefinition>();
            contracts[i].displayName = i == 0 ? "Contrato 1" : $"Contrato {i + 1}";
            contracts[i].description = i == 0 ? "Fase inicial." : "Bloqueado por enquanto.";
        }
    }

    private void OnEnable()
    {
        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session != null)
        {
            session.OnPreparationStateChanged += RefreshView;
            session.OnPreparationFeedback += ShowFeedback;
        }

        RefreshCharacterLabel();
        RefreshView();
        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
    }

    private void OnDisable()
    {
        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session != null)
        {
            session.OnPreparationStateChanged -= RefreshView;
            session.OnPreparationFeedback -= ShowFeedback;
        }
    }

    private void WireButtons()
    {
        if (_buttonsWired)
            return;

        if (readyButton != null)
            readyButton.onClick.AddListener(ToggleReady);

        if (chooseCharacterButton != null)
            chooseCharacterButton.onClick.AddListener(OnChooseCharacter);

        if (contractButtons == null)
            return;

        for (int i = 0; i < contractButtons.Length; i++)
        {
            int index = i;
            if (contractButtons[i] == null)
                continue;

            contractButtons[i].interactable = index == 0;
            contractButtons[i].onClick.AddListener(() =>
            {
                if (index > 0)
                    ShowFeedback("Este contrato está bloqueado por enquanto.");
                else
                    SelectContract(index);
            });

            EventTrigger trigger = contractButtons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = contractButtons[i].gameObject.AddComponent<EventTrigger>();

            AddHover(trigger, index);
        }

        _buttonsWired = true;
    }

    private static void AddHover(EventTrigger trigger, int index)
    {
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ =>
        {
            PreparationScreenController ctrl = Object.FindFirstObjectByType<PreparationScreenController>();
            ctrl?.ShowTooltip(index);
        });
        trigger.triggers.Add(entry);
    }

    private void OnChooseCharacter()
    {
        ScreenFlowStateMachine.OpenCharactersFromPreparation();
    }

    private void SelectContract(int index)
    {
        if (index > 0)
            return;

        _localSelectedContract = index;
        _localReady = false;

        if (!GameSessionContext.IsSinglePlayer)
        {
            PreparationSessionManager session = PreparationSessionManager.Instance;
            session?.RequestSelectContractRpc(index);
            session?.RequestSetReadyRpc(false);
        }

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active != null)
        {
            save.Active.selectedContractIndex = index;
            save.SaveActive();
        }

        HighlightSelectedContract(index);
        RefreshView();
    }

    private void ToggleReady()
    {
        _localReady = !_localReady;

        if (GameSessionContext.IsSinglePlayer)
        {
            string error = ValidateSinglePlayerReady();
            if (!string.IsNullOrEmpty(error))
            {
                _localReady = false;
                ShowFeedback(error);
                RefreshView();
                return;
            }

            if (_localReady)
            {
                ApplyContractScene(_localSelectedContract);
                LobbySelectionStore.CaptureSinglePlayer(_soloCharacter);
                ScreenFlowStateMachine.BeginGameplayLoading();
            }

            RefreshView();
            return;
        }

        PreparationSessionManager session = PreparationSessionManager.Instance;
        session?.RequestSetReadyRpc(_localReady);
        RefreshView();
    }

    private string ValidateSinglePlayerReady()
    {
        if (!_localReady)
            return string.Empty;

        if (_localSelectedContract < 0)
            return "Escolha um contrato antes de confirmar.";

        if (_soloCharacter == LobbyCharacterType.Default)
            return "Escolha um personagem antes de confirmar.";

        return string.Empty;
    }

    private void ApplyContractScene(int index)
    {
        string sceneName = "Fase-1";
        if (contracts != null && index >= 0 && index < contracts.Length && contracts[index] != null)
            sceneName = contracts[index].gameplaySceneName;

        GameSessionContext.ActiveGameplaySceneName = sceneName;
    }

    private void ShowTooltip(int index)
    {
        if (tooltipText == null || contracts == null || index < 0 || index >= contracts.Length || contracts[index] == null)
            return;

        PositionTooltipBelowContract(index);

        if (index > 0)
        {
            tooltipText.text = $"{contracts[index].displayName}\n\nBloqueado por enquanto.";
            return;
        }

        ContractDefinition contract = contracts[index];
        tooltipText.text =
            $"{contract.displayName}\nDificuldade: {contract.difficulty}/5\nRecompensa: {contract.magiculaReward} magículas\n\n{contract.description}";
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
    }

    private void RefreshCharacterLabel()
    {
        if (selectedCharacterText == null)
            return;

        LobbyCharacterType selected = ResolveLocalCharacter();
        selectedCharacterText.text = selected == LobbyCharacterType.Default
            ? "Personagem: (não escolhido)"
            : $"Personagem: {(selected == LobbyCharacterType.CharacterB ? "Cora" : "Nixie")}";
    }

    private LobbyCharacterType ResolveLocalCharacter()
    {
        if (GameSessionContext.IsSinglePlayer)
        {
            if (LobbySelectionStore.TryGetCharacter(0, out LobbyCharacterType selected))
                return _soloCharacter = selected;
            return _soloCharacter = LobbyCharacterType.Default;
        }

        PreparationSessionManager session = PreparationSessionManager.Instance;
        return session != null ? session.GetLocalCharacterType() : LobbyCharacterType.Default;
    }

    private void RefreshView()
    {
        RefreshCharacterLabel();

        if (GameSessionContext.IsSinglePlayer)
        {
            _soloCharacter = ResolveLocalCharacter();

            if (readyStatusText != null)
            {
                if (_localSelectedContract < 0)
                    readyStatusText.text = "Escolha um contrato";
                else if (_soloCharacter == LobbyCharacterType.Default)
                    readyStatusText.text = "Escolha um personagem";
                else
                    readyStatusText.text = _localReady ? "Iniciando fase..." : "Confirme para jogar";
            }

            if (_localSelectedContract >= 0)
                HighlightSelectedContract(_localSelectedContract);
            return;
        }

        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session == null)
        {
            if (readyStatusText != null)
                readyStatusText.text = "Aguardando sessão de rede...";
            return;
        }

        int readyCount = 0;
        int charCount = 0;
        for (int i = 0; i < session.Players.Count; i++)
        {
            if (session.Players[i].IsReady)
                readyCount++;
            if (session.Players[i].CharacterType != LobbyCharacterType.Default)
                charCount++;
        }

        if (readyStatusText != null)
        {
            readyStatusText.text = session.SelectedContractIndex < 0
                ? "Escolha um contrato"
                : $"Prontos: {readyCount}/{session.Players.Count} | Personagens: {charCount}/{session.Players.Count}";
        }

        if (session.SelectedContractIndex >= 0)
            HighlightSelectedContract(session.SelectedContractIndex);
    }

    private void HighlightSelectedContract(int index)
    {
        if (contractButtons == null)
            return;

        for (int i = 0; i < contractButtons.Length; i++)
        {
            if (contractButtons[i] == null)
                continue;

            Image image = contractButtons[i].GetComponent<Image>();
            if (image == null)
                continue;

            if (i > 0)
                image.color = new Color(0.12f, 0.12f, 0.14f, 0.7f);
            else
                image.color = i == index
                    ? new Color(0.75f, 0.15f, 0.15f, 0.95f)
                    : new Color(0.18f, 0.18f, 0.22f, 0.95f);
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
            string label = i == 0 && contracts[0] != null
                ? contracts[0].displayName
                : $"Contrato {i + 1} (bloqueado)";
            contractButtons[i] = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, label,
                new Vector2(x, 0.55f), new Vector2(x, 0.55f),
                new Vector2(-140f, -140f), new Vector2(140f, 140f));
        }

        tooltipText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Passe o mouse sobre o contrato.", 22,
            TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 20f), new Vector2(210f, 200f));

        selectedCharacterText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Personagem: (não escolhido)", 28,
            TextAlignmentOptions.BottomLeft, Color.white,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 120f), new Vector2(520f, 180f));

        chooseCharacterButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Escolher Personagem",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(320f, 100f));

        readyButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Pronto",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 40f), new Vector2(-40f, 100f));
        readyStatusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Escolha um contrato", 24,
            TextAlignmentOptions.Bottom, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 20f), new Vector2(280f, 60f));
    }
}
