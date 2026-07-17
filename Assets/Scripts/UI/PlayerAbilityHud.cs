using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using TMPro;

// HUD de gameplay (canto inferior esquerdo): Passiva, Dash, Q e R com cooldowns.
// Sprites opcionais por slot; fallback procedural quando ausentes.
// Textos traduzidos direto no script (pt-BR / en-US) conforme o idioma ativo.
[DisallowMultipleComponent]
public class PlayerAbilityHud : MonoBehaviour
{
    private sealed class SlotView
    {
        public AbilitySlot Slot;
        public Image Icon;
        public Image Background;
        public Image CooldownFill;
        public Image LockOverlay;
        public TextMeshProUGUI Label;
        public Text Timer;
        public TextMeshProUGUI PassiveCounter;
    }

    private static Font _runtimeFont;
    private static TMP_FontAsset _hudInknutFont;

    [Header("Arte opcional (substitui fallback quando atribuído)")]
    [SerializeField] private PlayerAbilityHudTheme theme;
    [SerializeField] private Sprite passiveIcon;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite ability1Icon;
    [SerializeField] private Sprite ability2Icon;

    [SerializeField] private bool buildIfMissing = true;
    [Header("Hotkeys (opcional - conectar no prefab)")]
    [SerializeField] private TMP_Text dashHotkeyText;
    [SerializeField] private TMP_Text ability1HotkeyText;
    [SerializeField] private TMP_Text ability2HotkeyText;
    [SerializeField] private bool includeHotkeyInAbilityLabel = true;

    private PlayerAbilityHandler _abilityHandler;
    private PlayerDash _dash;
    private PlayerPassiveHandler _passive;
    private string _cachedDashLabel = "Dash";
    private string _cachedAbility1Label = "Q";
    private string _cachedAbility2Label = "R";

    private RectTransform _root;
    private readonly SlotView[] _slots = new SlotView[4];

    private void OnEnable()
    {
        NetworkPlayerController.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        NetworkPlayerController.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
        TryBindLocalPlayer();
    }

    private void OnDisable()
    {
        NetworkPlayerController.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        NetworkPlayerController.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
        ClearBindings();
    }

    private void Awake()
    {
        ResolveThemeIcons();
        EnsureBuilt();
    }

    public void EnsureBuilt()
    {
        if (!IsAllowedInActiveScene())
            return;

        if (_slots[0] != null)
            return;

        if (buildIfMissing)
            BuildUi();

        ApplyLayoutFromTheme();
        ApplyCharacterHudIcons();
    }

    private void ResolveThemeIcons()
    {
        if (theme == null)
            theme = Resources.Load<PlayerAbilityHudTheme>("DefaultPlayerAbilityHudTheme");

        if (theme == null)
            return;

        if (passiveIcon == null) passiveIcon = theme.passiveIcon;
        if (dashIcon == null) dashIcon = theme.dashIcon;
        if (ability1Icon == null) ability1Icon = theme.ability1Icon;
        if (ability2Icon == null) ability2Icon = theme.ability2Icon;
    }

    private void LateUpdate()
    {
        if (!IsAllowedInActiveScene())
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }

        TryBindLocalPlayer();
        EnsureBuilt();

        RefreshPassiveSlot();
        RefreshDashSlot();
        RefreshAbilitySlot(_slots[2], AbilitySlot.Ability1, _cachedAbility1Label);
        RefreshAbilitySlot(_slots[3], AbilitySlot.Ability2, _cachedAbility2Label);
    }

    public static void EnsureOnCanvas(Canvas canvas, PlayerAbilityHudTheme hudTheme = null)
    {
        if (canvas == null || !IsAllowedInActiveScene())
            return;

        EnsureOnParent(canvas.transform, hudTheme);
    }

    public static void EnsureOnParent(Transform parent, PlayerAbilityHudTheme hudTheme = null)
    {
        if (parent == null || !IsAllowedInActiveScene())
            return;

        PlayerAbilityHud existing = parent.GetComponentInChildren<PlayerAbilityHud>(true);
        if (existing != null)
        {
            if (hudTheme != null)
                existing.ApplyTheme(hudTheme);
            existing.EnsureBuilt();
            if (!IsUnderGameplayHudLayers(parent))
                existing.transform.SetAsLastSibling();
            return;
        }

        CreateUnder(parent, hudTheme);
    }

    public static PlayerAbilityHud CreateUnder(Transform parent, PlayerAbilityHudTheme hudTheme = null)
    {
        if (parent == null || !IsAllowedInActiveScene())
            return null;

        GameObject go = new GameObject("PlayerAbilityHud", typeof(RectTransform), typeof(PlayerAbilityHud));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        PlayerAbilityHud hud = go.GetComponent<PlayerAbilityHud>();
        if (hudTheme != null)
            hud.ApplyTheme(hudTheme);
        hud.EnsureBuilt();
        return hud;
    }

    private static bool IsAllowedInActiveScene() =>
        GameplaySceneBootstrap.IsGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

    public void ApplyTheme(PlayerAbilityHudTheme hudTheme)
    {
        theme = hudTheme;
        ResolveThemeIcons();
        ApplyLayoutFromTheme();
    }

    private void ApplyLayoutFromTheme()
    {
        if (_root == null)
            return;

        float spacing = theme != null ? theme.slotSpacing : 130f;
        float slotSize = theme != null ? theme.slotSize : 128f;
        Vector2 anchor = ResolveHudAnchorPosition();

        _root.anchorMin = new Vector2(0f, 0f);
        _root.anchorMax = new Vector2(0f, 0f);
        _root.pivot = new Vector2(0f, 0f);
        _root.anchoredPosition = anchor;
        _root.sizeDelta = new Vector2(spacing * 3f + slotSize + 20f, slotSize + 20f);

        if (_slots[0] == null)
            return;

        ApplySlotLayout(_slots[0], new Vector2(0f, 0f), slotSize, ResolveLabelFontSize(), theme != null ? theme.timerFontSize : 22);
        ApplySlotLayout(_slots[1], new Vector2(spacing * 1f, 0f), slotSize, ResolveLabelFontSize(), theme != null ? theme.timerFontSize : 22);
        ApplySlotLayout(_slots[2], new Vector2(spacing * 2f, 0f), slotSize, ResolveLabelFontSize(), theme != null ? theme.timerFontSize : 22);
        ApplySlotLayout(_slots[3], new Vector2(spacing * 3f, 0f), slotSize, ResolveLabelFontSize(), theme != null ? theme.timerFontSize : 22);
        ConfigurePassiveSlotLayout(_slots[0]);
    }

    private void ApplySlotLayout(SlotView slot, Vector2 position, float slotSize, int labelSize, int timerSize)
    {
        if (slot?.Background == null)
            return;

        RectTransform rt = slot.Background.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        if (slot.Label != null)
        {
            slot.Label.fontSize = labelSize;
            slot.Label.color = ResolveLabelColor(unlocked: true);
            ApplyHotkeyLabelLayout(slot.Label);
        }
        if (slot.Timer != null)
        {
            slot.Timer.fontSize = timerSize;
            ApplyCooldownTimerLayout(slot.Timer);
        }

        ApplyOverlayInset(slot);
    }

    private void ApplyCooldownTimerLayout(Text timer)
    {
        if (timer == null)
            return;

        float minX = theme != null ? theme.cooldownTimerMinX : -0.03f;
        float maxX = theme != null ? theme.cooldownTimerMaxX : 0.97f;
        float minY = theme != null ? theme.cooldownTimerMinY : 0.84f;
        float maxY = theme != null ? theme.cooldownTimerMaxY : 1.14f;
        RectTransform rt = timer.rectTransform;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        timer.alignment = TextAnchor.UpperCenter;
    }

    private void ApplyHotkeyLabelLayout(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        float minX = theme != null ? theme.labelBandMinX : -0.04f;
        float maxX = theme != null ? theme.labelBandMaxX : 0.82f;
        float minY = theme != null ? theme.labelBandMinY : 0.14f;
        float maxY = theme != null ? theme.labelBandMaxY : 0.30f;
        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;
        ApplyHudInknutFont(label);
    }

    private float ResolveOverlayInset()
        => theme != null ? theme.overlayInset : 0.18f;

    private float ResolveOverlayShiftX()
        => theme != null ? theme.overlayShiftX : -0.10f;

    private float ResolveOverlayInsetY()
        => theme != null ? theme.overlayInsetY : 0.07f;

    private void ApplyInset(RectTransform rt, float inset)
    {
        float padX = Mathf.Clamp(inset, 0.05f, 0.35f);
        float padY = Mathf.Clamp(ResolveOverlayInsetY(), 0.02f, 0.35f);
        float shiftX = ResolveOverlayShiftX();
        float minX = Mathf.Clamp01(padX + shiftX);
        float maxX = Mathf.Clamp01(1f - padX + shiftX);
        if (maxX <= minX + 0.05f)
        {
            minX = padX;
            maxX = 1f - padX;
        }

        rt.anchorMin = new Vector2(minX, padY);
        rt.anchorMax = new Vector2(maxX, 1f - padY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void ApplyOverlayInset(SlotView slot)
    {
        if (slot == null)
            return;

        float inset = ResolveOverlayInset();
        if (slot.CooldownFill != null)
            ApplyInset(slot.CooldownFill.rectTransform, inset);
        if (slot.LockOverlay != null)
            ApplyInset(slot.LockOverlay.rectTransform, inset);
    }

    private void ConfigurePassiveSlotLayout(SlotView slot)
    {
        if (slot == null)
            return;

        if (slot.Label != null)
            slot.Label.gameObject.SetActive(false);

        if (slot.PassiveCounter != null)
        {
            // Mesma faixa inferior das teclas (Shift/Q/R).
            float minX = theme != null ? theme.labelBandMinX : -0.04f;
            float maxX = theme != null ? theme.labelBandMaxX : 0.82f;
            float minY = theme != null ? theme.labelBandMinY : 0.14f;
            float maxY = theme != null ? theme.labelBandMaxY : 0.30f;
            RectTransform counterRt = slot.PassiveCounter.rectTransform;
            counterRt.anchorMin = new Vector2(minX, minY);
            counterRt.anchorMax = new Vector2(maxX, maxY);
            counterRt.offsetMin = Vector2.zero;
            counterRt.offsetMax = Vector2.zero;
            slot.PassiveCounter.fontSize = ResolveLabelFontSize();
            slot.PassiveCounter.alignment = TextAlignmentOptions.Center;
            slot.PassiveCounter.color = theme != null ? theme.passiveCounterColor : Color.white;
            ApplyHudInknutFont(slot.PassiveCounter);
        }

        ApplyOverlayInset(slot);
    }

    private void ApplyHudInknutFont(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        TMP_FontAsset font = ResolveHudInknutFont();
        if (font != null)
            text.font = font;
    }

    private TMP_FontAsset ResolveHudInknutFont()
    {
        if (theme != null && theme.hudFont != null)
            return theme.hudFont;

        if (_hudInknutFont != null)
            return _hudInknutFont;

        PlayerAbilityHudTheme defaultTheme = Resources.Load<PlayerAbilityHudTheme>("DefaultPlayerAbilityHudTheme");
        if (defaultTheme != null && defaultTheme.hudFont != null)
            _hudInknutFont = defaultTheme.hudFont;

        return _hudInknutFont;
    }

    private void HandleLocalPlayerSpawned(NetworkPlayerController player) => BindPlayer(player != null ? player.gameObject : null);

    private void HandleLocalPlayerDespawned(ulong _) => ClearBindings();

    private void TryBindLocalPlayer()
    {
        if (_abilityHandler != null)
            return;

        NetworkPlayerController[] players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsOwner)
            {
                BindPlayer(players[i].gameObject);
                return;
            }
        }

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            BindPlayer(tagged);
    }

    private void BindPlayer(GameObject player)
    {
        if (player == null)
            return;

        if (_abilityHandler != null)
            _abilityHandler.OnAbilitySetChanged -= HandleAbilitySetChanged;

        _abilityHandler = player.GetComponent<PlayerAbilityHandler>();
        _dash = player.GetComponent<PlayerDash>();
        _passive = player.GetComponent<PlayerPassiveHandler>();

        if (_abilityHandler != null)
            _abilityHandler.OnAbilitySetChanged += HandleAbilitySetChanged;

        RebuildAbilityLabels();
        RefreshHotkeyTexts();
        ApplyCharacterHudIcons();
    }

    private void HandleAbilitySetChanged()
    {
        RebuildAbilityLabels();
        RefreshHotkeyTexts();
        ApplyCharacterHudIcons();
    }

    private void ClearBindings()
    {
        if (_abilityHandler != null)
            _abilityHandler.OnAbilitySetChanged -= HandleAbilitySetChanged;

        _abilityHandler = null;
        _dash = null;
        _passive = null;
        RebuildAbilityLabels();
        RefreshHotkeyTexts();
        ApplyCharacterHudIcons();
    }

    /// <summary>
    /// Troca os ícones da barra conforme o CharacterAbilitySet do jogador local (Cora/Nix).
    /// </summary>
    private void ApplyCharacterHudIcons()
    {
        EnsureBuilt();
        if (_slots[0] == null)
            return;

        CharacterAbilitySet set = _abilityHandler != null ? _abilityHandler.AbilitySet : null;

        ApplySlotArt(
            _slots[0],
            ResolveHudIcon(set != null ? set.passiveHudIcon : null, passiveIcon),
            theme != null ? theme.passiveFallbackColor : new Color(0.85f, 0.55f, 0.15f, 0.9f));
        ApplySlotArt(
            _slots[1],
            ResolveHudIcon(set != null ? set.dashHudIcon : null, dashIcon),
            theme != null ? theme.dashFallbackColor : new Color(0.35f, 0.75f, 0.95f, 0.9f));
        ApplySlotArt(
            _slots[2],
            ResolveHudIcon(set != null ? set.ability1HudIcon : null, ability1Icon),
            theme != null ? theme.abilityFallbackColor : new Color(0.75f, 0.55f, 0.2f, 0.9f));
        ApplySlotArt(
            _slots[3],
            ResolveHudIcon(set != null ? set.ability2HudIcon : null, ability2Icon),
            theme != null ? theme.abilityFallbackColor : new Color(0.75f, 0.55f, 0.2f, 0.9f));
    }

    private static Sprite ResolveHudIcon(Sprite characterIcon, Sprite fallbackIcon)
        => characterIcon != null ? characterIcon : fallbackIcon;

    private void ApplySlotArt(SlotView slot, Sprite art, Color fallbackColor)
    {
        if (slot == null)
            return;

        if (art != null)
        {
            // Arte completa no background do slot; esconde o fill colorido antigo.
            if (slot.Background != null)
            {
                slot.Background.sprite = art;
                slot.Background.type = Image.Type.Simple;
                // Mantém proporção do asset (ícones são mais altos que largos).
                slot.Background.preserveAspect = true;
                slot.Background.color = Color.white;
            }

            if (slot.Icon != null)
                slot.Icon.enabled = false;
            return;
        }

        if (slot.Background != null)
        {
            LoadingProgressUtility.ApplySolidSprite(slot.Background);
            slot.Background.preserveAspect = false;
            slot.Background.color = theme != null ? theme.backgroundColor : new Color(0.1f, 0.1f, 0.14f, 0.92f);
        }

        if (slot.Icon != null)
        {
            slot.Icon.enabled = true;
            slot.Icon.sprite = null;
            LoadingProgressUtility.ApplySolidSprite(slot.Icon);
            slot.Icon.color = fallbackColor;
        }
    }

    private void RefreshDashSlot()
    {
        SlotView slot = _slots[1];
        if (slot == null)
            return;

        bool unlocked = _abilityHandler == null || _abilityHandler.IsSlotUnlocked(AbilitySlot.Dash);
        float cooldownRemaining = _dash != null ? _dash.GetCooldownRemaining() : 0f;
        float cooldownTotal = _dash != null ? _dash.GetCooldownDuration() : 1f;
        string label = _abilityHandler != null
            ? _cachedDashLabel
            : _cachedDashLabel;

        RefreshCooldownSlot(slot, unlocked, cooldownRemaining, cooldownTotal,
            string.IsNullOrEmpty(label) ? _cachedDashLabel : label, unlockWave: 1);
    }

    private void RefreshAbilitySlot(SlotView slot, AbilitySlot abilitySlot, string fallbackLabel)
    {
        if (slot == null)
            return;

        if (_abilityHandler == null)
        {
            RefreshCooldownSlot(slot, unlocked: true, cooldownRemaining: 0f, cooldownTotal: 1f, fallbackLabel, unlockWave: 1);
            return;
        }

        bool unlocked = _abilityHandler.IsSlotUnlocked(abilitySlot);
        float cooldownRemaining = _abilityHandler.GetCooldownRemaining(abilitySlot);
        float cooldownTotal = _abilityHandler.GetCooldownTotal(abilitySlot);
        int unlockWave = _abilityHandler.GetSlotUnlockWave(abilitySlot);

        RefreshCooldownSlot(slot, unlocked, cooldownRemaining, cooldownTotal, fallbackLabel, unlockWave);
    }

    private void RefreshCooldownSlot(
        SlotView slot,
        bool unlocked,
        float cooldownRemaining,
        float cooldownTotal,
        string label,
        int unlockWave)
    {
        if (slot.Label != null)
        {
            slot.Label.text = label;
            slot.Label.color = ResolveLabelColor(unlocked);
        }

        if (!unlocked)
        {
            // Sem fade preto: ícone continua visível; só indica bloqueio no timer.
            if (slot.LockOverlay != null)
                slot.LockOverlay.gameObject.SetActive(false);
            if (slot.CooldownFill != null)
                slot.CooldownFill.gameObject.SetActive(false);
            if (slot.Timer != null)
            {
                slot.Timer.gameObject.SetActive(true);
                slot.Timer.text = unlockWave > 1
                    ? $"W{unlockWave}"
                    //tradução maneira
                    : (IsPortuguese() ? "Bloq" : "Lock");
            }
            return;
        }

        if (slot.LockOverlay != null)
            slot.LockOverlay.gameObject.SetActive(false);

        float total = Mathf.Max(0.01f, cooldownTotal);
        float ratio = Mathf.Clamp01(cooldownRemaining / total);
        bool onCooldown = cooldownRemaining > 0.05f;

        if (slot.CooldownFill != null)
        {
            slot.CooldownFill.fillAmount = ratio;
            slot.CooldownFill.gameObject.SetActive(onCooldown);
        }

        if (slot.Timer != null)
        {
            slot.Timer.gameObject.SetActive(onCooldown);
            if (onCooldown)
                slot.Timer.text = cooldownRemaining >= 10f ? $"{cooldownRemaining:0}" : $"{cooldownRemaining:0.#}";
        }
    }

    private void RefreshPassiveSlot()
    {
        SlotView slot = _slots[0];
        if (slot == null)
            return;

        if (slot.Label != null)
            slot.Label.gameObject.SetActive(false);

        bool hasPassive = _passive != null && _passive.PassiveKillsRequired > 0;
        if (slot.LockOverlay != null)
            slot.LockOverlay.gameObject.SetActive(false);

        if (!hasPassive)
        {
            if (slot.CooldownFill != null)
                slot.CooldownFill.gameObject.SetActive(false);
            if (slot.PassiveCounter != null)
            {
                slot.PassiveCounter.gameObject.SetActive(false);
                slot.PassiveCounter.text = string.Empty;
            }
            return;
        }

        if (_passive.IsPassiveActive)
        {
            float duration = Mathf.Max(0.01f, _passive.PassiveDuration);
            float ratio = _passive.PassiveTimeRemaining / duration;
            if (slot.CooldownFill != null)
            {
                slot.CooldownFill.fillAmount = ratio;
                slot.CooldownFill.gameObject.SetActive(true);
            }

            if (slot.PassiveCounter != null)
            {
                slot.PassiveCounter.gameObject.SetActive(true);
                slot.PassiveCounter.text = $"{_passive.PassiveTimeRemaining:0.#}";
            }

            return;
        }

        // Progresso 0/5 só no texto — sem escurecer o ícone.
        if (slot.CooldownFill != null)
            slot.CooldownFill.gameObject.SetActive(false);

        int required = Mathf.Max(1, _passive.PassiveKillsRequired);
        if (slot.PassiveCounter != null)
        {
            slot.PassiveCounter.gameObject.SetActive(true);
            slot.PassiveCounter.text = $"{_passive.PassiveKillProgress}/{required}";
        }
    }

    private int ResolveLabelFontSize()
        => theme != null ? theme.labelFontSize : 17;

    private Color ResolveLabelColor(bool unlocked)
    {
        if (unlocked)
            return theme != null ? theme.labelColor : Color.black;

        return theme != null ? theme.labelLockedColor : new Color(0f, 0f, 0f, 0.45f);
    }

    //tradução maneira
    private static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        // Sem locale definido, assume português (idioma base do projeto).
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnderGameplayHudLayers(Transform node)
    {
        while (node != null)
        {
            if (node.name == GameplayHudController.LayersRootName)
                return true;
            node = node.parent;
        }

        return false;
    }

    private Vector2 ResolveHudAnchorPosition()
    {
        Vector2 margin = theme != null ? theme.anchoredPosition : new Vector2(12f, 10f);
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return margin;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return margin;

        Rect safe = Screen.safeArea;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, safe.min, cam, out Vector2 localMin))
            return margin;

        return new Vector2(margin.x + localMin.x, margin.y + localMin.y);
    }

    private void BuildUi()
    {
        if (_root != null && _slots[0] != null)
            return;

        _root = GetComponent<RectTransform>();
        if (_root == null)
            _root = gameObject.AddComponent<RectTransform>();

        _root.anchorMin = new Vector2(0f, 0f);
        _root.anchorMax = new Vector2(0f, 0f);
        _root.pivot = new Vector2(0f, 0f);

        float spacing = theme != null ? theme.slotSpacing : 130f;
        float slotSize = theme != null ? theme.slotSize : 128f;
        int labelSize = ResolveLabelFontSize();
        int timerSize = theme != null ? theme.timerFontSize : 22;
        Vector2 anchor = ResolveHudAnchorPosition();
        _root.anchoredPosition = anchor;
        _root.sizeDelta = new Vector2(spacing * 3f + slotSize + 20f, slotSize + 20f);

        _slots[0] = CreateSlot("PassiveSlot", AbilitySlot.Ability1, string.Empty, passiveIcon, new Vector2(0f, 0f), slotSize, labelSize, timerSize, isPassiveSlot: true);
        _slots[1] = CreateSlot("DashSlot", AbilitySlot.Dash, "Dash", dashIcon, new Vector2(spacing * 1f, 0f), slotSize, labelSize, timerSize);
        _slots[2] = CreateSlot("Ability1Slot", AbilitySlot.Ability1, "Q", ability1Icon, new Vector2(spacing * 2f, 0f), slotSize, labelSize, timerSize);
        _slots[3] = CreateSlot("Ability2Slot", AbilitySlot.Ability2, "R", ability2Icon, new Vector2(spacing * 3f, 0f), slotSize, labelSize, timerSize);
        ConfigurePassiveSlotLayout(_slots[0]);
    }

    private SlotView CreateSlot(
        string name,
        AbilitySlot slot,
        string label,
        Sprite iconSprite,
        Vector2 position,
        float slotSize,
        int labelFontSize,
        int timerFontSize,
        bool isPassiveSlot = false)
    {
        GameObject root = CreateUiObject(name, _root);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        Image bg = root.GetComponent<Image>();
        bg.color = theme != null ? theme.backgroundColor : new Color(0.1f, 0.1f, 0.14f, 0.92f);

        GameObject iconGo = CreateUiObject("Icon", rt);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.1f, 0.1f);
        iconRt.anchorMax = new Vector2(0.9f, 0.9f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image icon = iconGo.GetComponent<Image>();
        if (iconSprite != null)
        {
            icon.sprite = iconSprite;
            icon.color = Color.white;
        }
        else
        {
            icon.color = name.Contains("Passive")
                ? (theme != null ? theme.passiveFallbackColor : new Color(0.85f, 0.55f, 0.15f, 0.9f))
                : slot == AbilitySlot.Dash
                    ? (theme != null ? theme.dashFallbackColor : new Color(0.35f, 0.75f, 0.95f, 0.9f))
                    : (theme != null ? theme.abilityFallbackColor : new Color(0.75f, 0.55f, 0.2f, 0.9f));
        }

        GameObject fillGo = CreateUiObject("Cooldown", rt);
        ApplyInset(fillGo.GetComponent<RectTransform>(), ResolveOverlayInset());
        Image fill = fillGo.GetComponent<Image>();
        fill.color = theme != null ? theme.cooldownOverlayColor : new Color(0f, 0f, 0f, 0.65f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Top;
        fill.gameObject.SetActive(false);

        GameObject lockGo = CreateUiObject("Lock", rt);
        ApplyInset(lockGo.GetComponent<RectTransform>(), ResolveOverlayInset());
        Image lockImg = lockGo.GetComponent<Image>();
        lockImg.color = new Color(0f, 0f, 0f, 0.7f);
        lockGo.SetActive(false);

        TextMeshProUGUI title = CreateHotkeyLabel(rt, label, labelFontSize, ResolveLabelColor(true));
        if (isPassiveSlot)
            title.gameObject.SetActive(false);
        else
            ApplyHotkeyLabelLayout(title);

        Text timer = null;
        TextMeshProUGUI passiveCounter = null;
        if (isPassiveSlot)
            passiveCounter = CreatePassiveCounter(rt, labelFontSize);
        else
        {
            timer = CreateText(
                rt,
                string.Empty,
                timerFontSize,
                TextAnchor.UpperCenter,
                new Vector2(theme != null ? theme.cooldownTimerMinX : -0.03f, theme != null ? theme.cooldownTimerMinY : 0.84f),
                new Vector2(theme != null ? theme.cooldownTimerMaxX : 0.97f, theme != null ? theme.cooldownTimerMaxY : 1.14f),
                Color.white);
            timer.gameObject.SetActive(false);
        }

        return new SlotView
        {
            Slot = slot,
            Icon = icon,
            Background = bg,
            CooldownFill = fill,
            LockOverlay = lockImg,
            Label = title,
            Timer = timer,
            PassiveCounter = passiveCounter
        };
    }

    private TextMeshProUGUI CreateHotkeyLabel(Transform parent, string text, int fontSize, Color color)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        ApplyHudInknutFont(label);
        return label;
    }

    private TextMeshProUGUI CreatePassiveCounter(Transform parent, int fontSize)
    {
        GameObject go = new GameObject("PassiveCounter", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        TextMeshProUGUI counter = go.AddComponent<TextMeshProUGUI>();
        counter.text = string.Empty;
        counter.fontSize = fontSize;
        counter.alignment = TextAlignmentOptions.Center;
        counter.color = theme != null ? theme.passiveCounterColor : Color.white;
        counter.raycastTarget = false;
        counter.textWrappingMode = TextWrappingModes.NoWrap;
        counter.overflowMode = TextOverflowModes.Overflow;
        ApplyHudInknutFont(counter);
        go.SetActive(false);
        return counter;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        LoadingProgressUtility.ApplySolidSprite(go.GetComponent<Image>());
        return go;
    }

    private static Text CreateText(
        Transform parent,
        string text,
        int size,
        TextAnchor alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Text label = go.GetComponent<Text>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.font = GetRuntimeFont();
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private static Font GetRuntimeFont()
    {
        if (_runtimeFont != null)
            return _runtimeFont;

        GameplayUiFontConfig config = Resources.Load<GameplayUiFontConfig>("GameplayUiFontConfig");
        if (config != null && config.LegacyFont != null)
            _runtimeFont = config.LegacyFont;
        else
            _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _runtimeFont;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void RebuildAbilityLabels()
    {
        _cachedDashLabel = BuildSlotLabel(AbilitySlot.Dash);
        _cachedAbility1Label = BuildSlotLabel(AbilitySlot.Ability1);
        _cachedAbility2Label = BuildSlotLabel(AbilitySlot.Ability2);
    }

    private string BuildSlotLabel(AbilitySlot slot)
    {
        string hotkey = GetHotkeyLabel(slot);
        if (string.IsNullOrEmpty(hotkey))
            return string.Empty;

        // Só a tecla de acesso (ex.: Shift, Q, R) — o ícone já identifica a skill.
        return hotkey;
    }

    private static string GetHotkeyLabel(AbilitySlot slot)
    {
        return slot switch
        {
            AbilitySlot.Dash => "Shift",
            AbilitySlot.Ability1 => "Q",
            AbilitySlot.Ability2 => "R",
            _ => string.Empty
        };
    }

    private void RefreshHotkeyTexts()
    {
        SetHotkeyText(dashHotkeyText, GetHotkeyLabel(AbilitySlot.Dash));
        SetHotkeyText(ability1HotkeyText, GetHotkeyLabel(AbilitySlot.Ability1));
        SetHotkeyText(ability2HotkeyText, GetHotkeyLabel(AbilitySlot.Ability2));
    }

    private static void SetHotkeyText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
