//--------------------------------------------------
// FEITO POR: DEBS CARVALHO
// DATA: 06/07/2026
// DESCRIÇÃO: Script para o painel de habilidades do personagem
//--------------------------------------------------

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterSkillsPanel : MonoBehaviour
{
    private readonly struct SkillBarSetup
    {
        public readonly string BarName;
        public readonly AbilitySlot Slot;
        public readonly string LocalizationKey;
        public readonly string AnimatorTrigger;

        public SkillBarSetup(string barName, AbilitySlot slot, string localizationKey, string animatorTrigger)
        {
            BarName = barName;
            Slot = slot;
            LocalizationKey = localizationKey;
            AnimatorTrigger = animatorTrigger;
        }
    }

    private static readonly SkillBarSetup[] NixBars =
    {
        new("Empurrão", AbilitySlot.Ability1, "Nix.Empurrao.description", "OnAbility1"),
        new("Investida", AbilitySlot.Ability2, "Nix.Investida.description", "OnDashAttack"),
        new("AtaqueNormal", AbilitySlot.PrimaryAttack, "Nix.AtaqueNormal.description", "OnShoot"),
    };

    private static readonly SkillBarSetup[] CoraBars =
    {
        new("Barreira", AbilitySlot.Ability1, "Cora.Barreira.description", "OnAbility1"),
        new("Poça", AbilitySlot.Ability2, "Cora.Poca.description", "OnDamage"),
        new("AtaqueNormal", AbilitySlot.PrimaryAttack, "Cora.AtaqueNormal.description", "OnShoot"),
    };

    [SerializeField] private LobbyCharacterType character = LobbyCharacterType.CharacterA;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Animator previewAnimator;
    [SerializeField] private Button exitButton;
    [SerializeField] private Transform upgradeBarsRoot;

    private SkillBarEntry[] _bars = Array.Empty<SkillBarEntry>();
    private SkillBarEntry _selectedBar;
    private LocalizedString _localizedDescription;
    private string _descriptionKey;
    private bool _localeSubscribed;
    private int _upgradeCostPerTier = 2;
    private bool _browseMode;

    public event Action ExitRequested;
    public event Action<AbilitySlot> UpgradeRequested;

    public void Bind(CharactersScreenController host, LobbyCharacterType characterType, int upgradeCostPerTier, bool browseMode)
    {
        character = characterType;
        _upgradeCostPerTier = Mathf.Max(1, upgradeCostPerTier);
        _browseMode = browseMode;
        ResolveReferences();
        BuildBars();
        WireExitButton();
        RefreshBars();
    }

    public void SetBrowseMode(bool browseMode)
    {
        _browseMode = browseMode;
        RefreshBars();
    }

    public void RefreshBars()
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        int magiculas = save?.Active?.magiculas ?? 0;

        for (int i = 0; i < _bars.Length; i++)
        {
            SkillBarEntry bar = _bars[i];
            if (bar == null)
                continue;

            int tier = save?.Active?.GetCharacterData(character).GetTierForSlot(bar.AbilitySlot) ?? 0;
            bool canAfford = tier < 3 && magiculas >= _upgradeCostPerTier;
            bar.ApplyUpgradeVisual(tier, canAfford);
            bar.SetSelected(bar == _selectedBar);
        }
    }

    private void OnEnable()
    {
        if (_localeSubscribed)
            return;

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        _localeSubscribed = true;
    }

    private void OnDisable()
    {
        if (_localeSubscribed)
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            _localeSubscribed = false;
        }

        UnbindDescription();
    }

    private void ResolveReferences()
    {
        if (upgradeBarsRoot == null)
        {
            Transform bars = transform.Find("Upgrade_barras");
            if (bars != null)
                upgradeBarsRoot = bars;
        }

        if (descriptionText == null)
            descriptionText = transform.Find("Description_Skill")?.GetComponent<TMP_Text>();

        if (previewAnimator == null)
        {
            string previewName = character == LobbyCharacterType.CharacterB ? "Cora" : "Nyxie";
            Transform preview = transform.Find(previewName);
            if (preview != null)
                previewAnimator = preview.GetComponent<Animator>();
        }

        EnsurePreviewEventReceiver();

        if (exitButton == null)
            exitButton = transform.Find("ExitButton")?.GetComponent<Button>();

        DisableBlockingRaycasts();
    }

    private void EnsurePreviewEventReceiver()
    {
        if (previewAnimator == null)
            return;

        if (previewAnimator.GetComponent<UiAnimationEventStub>() == null)
            previewAnimator.gameObject.AddComponent<UiAnimationEventStub>();
    }

    private void DisableBlockingRaycasts()
    {
        if (descriptionText != null)
            descriptionText.raycastTarget = false;

        if (upgradeBarsRoot == null)
            return;

        for (int i = 0; i < upgradeBarsRoot.childCount; i++)
        {
            Transform bar = upgradeBarsRoot.GetChild(i);
            if (bar == null)
                continue;

            Transform skillName = bar.Find("Skill_name");
            if (skillName != null)
            {
                Graphic graphic = skillName.GetComponent<Graphic>();
                if (graphic != null)
                    graphic.raycastTarget = false;
            }
        }
    }

    private void BuildBars()
    {
        if (upgradeBarsRoot == null)
            return;

        SkillBarSetup[] setups = character == LobbyCharacterType.CharacterB ? CoraBars : NixBars;
        _bars = new SkillBarEntry[setups.Length];

        for (int i = 0; i < setups.Length; i++)
        {
            SkillBarSetup setup = setups[i];
            Transform barTransform = FindBarTransform(setup.BarName);
            if (barTransform == null)
                continue;

            SkillBarEntry entry = barTransform.GetComponent<SkillBarEntry>();
            if (entry == null)
                entry = barTransform.gameObject.AddComponent<SkillBarEntry>();

            entry.Configure(setup.Slot, setup.LocalizationKey, setup.AnimatorTrigger);
            entry.Clicked -= HandleBarClicked;
            entry.Clicked += HandleBarClicked;
            _bars[i] = entry;
        }

        if (_selectedBar == null && _bars.Length > 0 && _bars[0] != null)
            SelectBar(_bars[0]);
    }

    private Transform FindBarTransform(string barName)
    {
        for (int i = 0; i < upgradeBarsRoot.childCount; i++)
        {
            Transform child = upgradeBarsRoot.GetChild(i);
            if (child != null && BarNamesMatch(child.name, barName))
                return child;
        }

        return null;
    }

    private static bool BarNamesMatch(string sceneName, string expectedName)
    {
        if (string.Equals(sceneName, expectedName, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(NormalizeBarName(sceneName), NormalizeBarName(expectedName), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeBarName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        return name
            .Replace("ç", "c", StringComparison.OrdinalIgnoreCase)
            .Replace("ã", "a", StringComparison.OrdinalIgnoreCase);
    }

    private void WireExitButton()
    {
        if (exitButton == null)
            return;

        exitButton.onClick.RemoveListener(HandleExitClicked);
        exitButton.onClick.AddListener(HandleExitClicked);
    }

    private void HandleExitClicked()
    {
        ExitRequested?.Invoke();
    }

    private void HandleBarClicked(SkillBarEntry bar)
    {
        if (bar == null)
            return;

        if (!_browseMode && bar == _selectedBar && CanUpgrade(bar.AbilitySlot))
        {
            UpgradeRequested?.Invoke(bar.AbilitySlot);
            return;
        }

        SelectBar(bar);
    }

    private void SelectBar(SkillBarEntry bar)
    {
        _selectedBar = bar;

        for (int i = 0; i < _bars.Length; i++)
        {
            if (_bars[i] != null)
                _bars[i].SetSelected(_bars[i] == bar);
        }

        BindDescription(bar.LocalizationKey);
        PlayPreviewAnimation(bar.AnimatorTrigger);
        RefreshBars();
    }

    private bool CanUpgrade(AbilitySlot slot)
    {
        SaveProfileStore save = SaveProfileStore.Instance;
        if (save == null)
            return false;

        int tier = save.Active.GetCharacterData(character).GetTierForSlot(slot);
        return tier < 3 && save.Active.magiculas >= _upgradeCostPerTier;
    }

    private void BindDescription(string key)
    {
        UnbindDescription();

        _descriptionKey = key;
        if (descriptionText == null || string.IsNullOrEmpty(key))
            return;

        _localizedDescription = new LocalizedString("UI", key);
        _localizedDescription.StringChanged += OnDescriptionChanged;
    }

    private void UnbindDescription()
    {
        if (_localizedDescription != null)
        {
            _localizedDescription.StringChanged -= OnDescriptionChanged;
            _localizedDescription = null;
        }

        _descriptionKey = null;
    }

    private void OnDescriptionChanged(string value)
    {
        if (descriptionText == null)
            return;

        descriptionText.text = string.IsNullOrEmpty(value)
            ? UiLocalization.Get(_descriptionKey, string.Empty)
            : value;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _)
    {
        _localizedDescription?.RefreshString();
    }

    private void PlayPreviewAnimation(string triggerName)
    {
        if (previewAnimator == null || string.IsNullOrEmpty(triggerName))
            return;

        previewAnimator.ResetTrigger(triggerName);
        previewAnimator.SetTrigger(triggerName);
    }
}
