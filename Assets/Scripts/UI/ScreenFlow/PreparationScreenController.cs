using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hub de preparação: contratos, personagem selecionado e confirmação de pronto.
/// </summary>
[DisallowMultipleComponent]
public class PreparationScreenController : MonoBehaviour
{
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

    private void Awake()
    {
        ResolveContracts();

        if (buildPlaceholderIfMissing && (contractButtons == null || contractButtons.Length == 0))
            BuildPlaceholderUI();

        WireButtons();
    }

    private void ResolveContracts()
    {
        if (contracts != null && contracts.Length > 0)
            return;

        ContractDefinition[] loaded = Resources.FindObjectsOfTypeAll<ContractDefinition>();
        if (loaded.Length == 0)
            return;

        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
        contracts = loaded;
    }

    private void OnEnable()
    {
        if (PreparationSessionManager.Instance != null)
            PreparationSessionManager.Instance.OnPreparationStateChanged += RefreshView;

        RefreshCharacterLabel();
        RefreshView();
        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
    }

    private void OnDisable()
    {
        if (PreparationSessionManager.Instance != null)
            PreparationSessionManager.Instance.OnPreparationStateChanged -= RefreshView;
    }

    private void WireButtons()
    {
        if (chooseCharacterButton != null)
            chooseCharacterButton.onClick.AddListener(OpenCharacters);

        if (readyButton != null)
            readyButton.onClick.AddListener(ToggleReady);

        if (contractButtons == null)
            return;

        for (int i = 0; i < contractButtons.Length; i++)
        {
            int index = i;
            if (contractButtons[i] == null)
                continue;

            contractButtons[i].onClick.AddListener(() => SelectContract(index));

            EventTrigger trigger = contractButtons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = contractButtons[i].gameObject.AddComponent<EventTrigger>();

            AddHover(trigger, index);
        }
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

    private void OpenCharacters()
    {
        GameSessionContext.CharactersMode = GameSessionContext.CharactersScreenMode.SelectionAllowed;
        GameSessionContext.ReturnRouteId = SceneFlowRouteIds.PreparationToHub;

        if (GameFlowOrchestrator.Instance != null)
            GameFlowOrchestrator.Instance.TryRequestRoute(SceneFlowRouteIds.PreparationToCharacters);
        else
            ScreenFlowController.Instance?.RequestRoute(SceneFlowRouteIds.PreparationToCharacters);
    }

    private void SelectContract(int index)
    {
        _localSelectedContract = index;
        PreparationSessionManager session = PreparationSessionManager.Instance;
        session?.RequestSelectContractRpc(index);

        SaveProfileStore save = SaveProfileStore.Instance;
        if (save?.Active != null)
        {
            save.Active.selectedContractIndex = index;
            save.SaveActive();
        }

        HighlightSelectedContract(index);
    }

    private void ToggleReady()
    {
        _localReady = !_localReady;
        PreparationSessionManager session = PreparationSessionManager.Instance;
        session?.RequestSetReadyRpc(_localReady);
        RefreshView();
    }

    private void ShowTooltip(int index)
    {
        if (tooltipText == null || contracts == null || index < 0 || index >= contracts.Length || contracts[index] == null)
            return;

        ContractDefinition contract = contracts[index];
        tooltipText.text =
            $"{contract.displayName}\nDificuldade: {contract.difficulty}/5\nRecompensa: {contract.magiculaReward} magículas\n\n{contract.description}";
    }

    private void RefreshCharacterLabel()
    {
        if (selectedCharacterText == null)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        LobbyCharacterType selected = save != null ? save.GetSelectedCharacter() : LobbyCharacterType.CharacterA;
        string name = selected == LobbyCharacterType.CharacterB ? "Cora" : "Nix";
        selectedCharacterText.text = $"Personagem: {name}";
    }

    private void RefreshView()
    {
        PreparationSessionManager session = PreparationSessionManager.Instance;
        if (session == null)
        {
            if (readyStatusText != null)
                readyStatusText.text = "Aguardando sessão de rede...";
            return;
        }

        int readyCount = 0;
        for (int i = 0; i < session.Players.Count; i++)
        {
            if (session.Players[i].IsReady)
                readyCount++;
        }

        if (readyStatusText != null)
            readyStatusText.text = $"Prontos: {readyCount}/{session.Players.Count}";

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

            image.color = i == index
                ? new Color(0.75f, 0.15f, 0.15f, 0.95f)
                : new Color(0.18f, 0.18f, 0.22f, 0.95f);
        }
    }

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "PreparationPanel", new Color(0.06f, 0.06f, 0.08f, 0.96f));

        contractButtons = new Button[3];
        contracts = new ContractDefinition[3];
        for (int i = 0; i < 3; i++)
        {
            contracts[i] = ScriptableObject.CreateInstance<ContractDefinition>();
            contracts[i].displayName = $"Contrato {i + 1}";
            contracts[i].description = "Descrição placeholder da fase.";
            contracts[i].difficulty = i + 1;
            contracts[i].magiculaReward = i + 1;

            contractButtons[i] = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, contracts[i].displayName,
                new Vector2(0.15f + i * 0.28f, 0.55f), new Vector2(0.15f + i * 0.28f, 0.55f),
                new Vector2(-120f, -140f), new Vector2(120f, 140f));
        }

        tooltipText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Passe o mouse sobre um contrato.", 22,
            TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-520f, -180f), new Vector2(-40f, 180f));

        selectedCharacterText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Personagem: Nix", 28,
            TextAlignmentOptions.BottomLeft, Color.white,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 120f), new Vector2(420f, 180f));

        chooseCharacterButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Escolher Personagem",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(320f, 100f));
        readyButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Pronto",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 40f), new Vector2(-40f, 100f));
        readyStatusText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Prontos: 0/0", 24,
            TextAlignmentOptions.Bottom, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200f, 20f), new Vector2(200f, 60f));
    }
}
