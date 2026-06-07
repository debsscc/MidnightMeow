using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de personagens: upgrades por magículas e seleção (quando permitido).
/// </summary>
[DisallowMultipleComponent]
public class CharactersScreenController : MonoBehaviour
{
    [SerializeField] private CharacterAbilitySet nixAbilitySet;
    [SerializeField] private CharacterAbilitySet coraAbilitySet;
    [SerializeField] private int upgradeCostPerTier = 2;

    [SerializeField] private TMP_Text magiculasText;
    [SerializeField] private Button nixPanelButton;
    [SerializeField] private Button coraPanelButton;
    [SerializeField] private Button skill1Button;
    [SerializeField] private Button skill2Button;
    [SerializeField] private Button skill3Button;
    [SerializeField] private TMP_Text skillDetailText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button backButton;

    [SerializeField] private bool buildPlaceholderIfMissing = true;

    private LobbyCharacterType _focusedCharacter = LobbyCharacterType.CharacterA;
    private AbilitySlot _focusedSlot = AbilitySlot.Ability1;

    private void Awake()
    {
        if (buildPlaceholderIfMissing && magiculasText == null)
            BuildPlaceholderUI();

        ResolveAbilitySets();
        WireButtons();
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

        RefreshView();
        ScreenFlowPlaceholderFactory.ApplyMenuCursor();
    }

    private void OnDisable()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save != null)
            save.OnProfileChanged -= RefreshView;
    }

    private void WireButtons()
    {
        if (nixPanelButton != null) nixPanelButton.onClick.AddListener(() => OnCharacterPanelClicked(LobbyCharacterType.CharacterA));
        if (coraPanelButton != null) coraPanelButton.onClick.AddListener(() => OnCharacterPanelClicked(LobbyCharacterType.CharacterB));
        if (skill1Button != null) skill1Button.onClick.AddListener(() => FocusSkill(AbilitySlot.Ability1));
        if (skill2Button != null) skill2Button.onClick.AddListener(() => FocusSkill(AbilitySlot.Ability2));
        if (skill3Button != null) skill3Button.onClick.AddListener(() => FocusSkill(AbilitySlot.PrimaryAttack));
        if (upgradeButton != null) upgradeButton.onClick.AddListener(TryUpgrade);
        if (backButton != null) backButton.onClick.AddListener(GoBack);
    }

    private void OnCharacterPanelClicked(LobbyCharacterType type)
    {
        _focusedCharacter = type;

        if (GameSessionContext.CharactersMode == GameSessionContext.CharactersScreenMode.SelectionAllowed)
        {
            SaveProfileStore save = SaveProfileStore.Instance;
            save?.SetSelectedCharacter(type);
            LobbySessionManager lobby = LobbySessionManager.Instance;
            lobby?.RequestSetCharacterRpc((byte)type);
        }

        RefreshView();
    }

    private void FocusSkill(AbilitySlot slot)
    {
        _focusedSlot = slot;
        RefreshSkillDetail();
    }

    private void TryUpgrade()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null)
            return;

        CharacterSaveData data = save.Active.GetCharacterData(_focusedCharacter);
        int currentTier = data.GetTierForSlot(_focusedSlot);
        if (currentTier >= 3)
            return;

        if (!save.TrySpendMagiculas(upgradeCostPerTier))
            return;

        data.SetTierForSlot(_focusedSlot, currentTier + 1);
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

    private void RefreshView()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (magiculasText != null)
            magiculasText.text = $"Magículas: {save?.Active?.magiculas ?? 0}";

        HighlightCharacterPanels();
        RefreshSkillDetail();
    }

    private void HighlightCharacterPanels()
    {
        SetPanelHighlight(nixPanelButton, _focusedCharacter == LobbyCharacterType.CharacterA);
        SetPanelHighlight(coraPanelButton, _focusedCharacter == LobbyCharacterType.CharacterB);
    }

    private static void SetPanelHighlight(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected ? new Color(0.75f, 0.15f, 0.15f, 0.95f) : new Color(0.18f, 0.18f, 0.22f, 0.95f);
    }

    private void RefreshSkillDetail()
    {
        if (skillDetailText == null)
            return;

        SaveProfileStore save = SaveProfileStore.Instance;
        CharacterSaveData data = save?.Active?.GetCharacterData(_focusedCharacter) ?? new CharacterSaveData();
        int tier = data.GetTierForSlot(_focusedSlot);

        CharacterAbilitySet set = _focusedCharacter == LobbyCharacterType.CharacterB ? coraAbilitySet : nixAbilitySet;
        string skillName = ResolveSkillName(set, _focusedSlot);
        string description = ResolveSkillDescription(set, _focusedSlot);

        skillDetailText.text =
            $"{skillName}\nNível: {tier}/3\n\n{description}\n\nUpgrade ({upgradeCostPerTier} magículas)";

        if (upgradeButton != null)
            upgradeButton.interactable = tier < 3 && (save?.Active?.magiculas ?? 0) >= upgradeCostPerTier;
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

    private void BuildPlaceholderUI()
    {
        Canvas canvas = ScreenFlowPlaceholderFactory.EnsureCanvas(transform);
        GameObject panel = ScreenFlowPlaceholderFactory.CreatePanel(canvas.transform, "CharactersPanel", new Color(0.05f, 0.05f, 0.08f, 0.96f));

        magiculasText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Magículas: 0", 28,
            TextAlignmentOptions.TopRight, Color.white,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-360f, -80f), new Vector2(-40f, -20f));

        nixPanelButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Nix",
            new Vector2(0.2f, 0.65f), new Vector2(0.2f, 0.65f), new Vector2(-160f, -120f), new Vector2(160f, 120f));
        coraPanelButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Cora",
            new Vector2(0.8f, 0.65f), new Vector2(0.8f, 0.65f), new Vector2(-160f, -120f), new Vector2(160f, 120f));

        skill1Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Skill 1",
            new Vector2(0.2f, 0.35f), new Vector2(0.2f, 0.35f), new Vector2(-120f, -30f), new Vector2(120f, 30f));
        skill2Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Skill 2",
            new Vector2(0.2f, 0.22f), new Vector2(0.2f, 0.22f), new Vector2(-120f, -30f), new Vector2(120f, 30f));
        skill3Button = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Skill 3",
            new Vector2(0.2f, 0.09f), new Vector2(0.2f, 0.09f), new Vector2(-120f, -30f), new Vector2(120f, 30f));

        skillDetailText = ScreenFlowPlaceholderFactory.CreateText(panel.transform, "Selecione uma skill.", 22,
            TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0.55f, 0.15f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);

        upgradeButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Upgrade",
            new Vector2(0.75f, 0.08f), new Vector2(0.75f, 0.08f), new Vector2(-140f, -35f), new Vector2(140f, 35f));
        backButton = ScreenFlowPlaceholderFactory.CreateButton(panel.transform, "Voltar",
            new Vector2(0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(-100f, -35f), new Vector2(100f, 35f));
    }
}
