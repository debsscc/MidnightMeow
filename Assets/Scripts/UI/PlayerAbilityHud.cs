using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

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
        public Text Label;
        public Text Timer;
    }

    private static Font _runtimeFont;

    [Header("Arte opcional (substitui fallback quando atribuído)")]
    [SerializeField] private PlayerAbilityHudTheme theme;
    [SerializeField] private Sprite passiveIcon;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite ability1Icon;
    [SerializeField] private Sprite ability2Icon;

    [SerializeField] private bool buildIfMissing = true;

    private PlayerAbilityHandler _abilityHandler;
    private PlayerDash _dash;
    private PlayerPassiveHandler _passive;

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
        if (_slots[0] != null)
            return;

        if (buildIfMissing)
            BuildUi();

        ApplyLayoutFromTheme();
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
        TryBindLocalPlayer();
        EnsureBuilt();

        RefreshPassiveSlot();
        RefreshDashSlot();
        RefreshAbilitySlot(_slots[2], AbilitySlot.Ability1, "Q");
        RefreshAbilitySlot(_slots[3], AbilitySlot.Ability2, "R");
    }

    public static void EnsureOnCanvas(Canvas canvas, PlayerAbilityHudTheme hudTheme = null)
    {
        if (canvas == null)
            return;

        EnsureOnParent(canvas.transform, hudTheme);
    }

    public static void EnsureOnParent(Transform parent, PlayerAbilityHudTheme hudTheme = null)
    {
        if (parent == null)
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
        GameObject go = new GameObject("PlayerAbilityHud", typeof(RectTransform), typeof(PlayerAbilityHud));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        PlayerAbilityHud hud = go.GetComponent<PlayerAbilityHud>();
        if (hudTheme != null)
            hud.ApplyTheme(hudTheme);
        hud.EnsureBuilt();
        return hud;
    }

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

        float spacing = theme != null ? theme.slotSpacing : 88f;
        float slotSize = theme != null ? theme.slotSize : 76f;
        Vector2 anchor = ResolveHudAnchorPosition();

        _root.anchorMin = new Vector2(0f, 0f);
        _root.anchorMax = new Vector2(0f, 0f);
        _root.pivot = new Vector2(0f, 0f);
        _root.anchoredPosition = anchor;
        _root.sizeDelta = new Vector2(spacing * 3f + slotSize + 20f, slotSize + 20f);

        if (_slots[0] == null)
            return;

        ApplySlotLayout(_slots[0], new Vector2(0f, 10f), slotSize, theme != null ? theme.labelFontSize : 16, theme != null ? theme.timerFontSize : 22);
        ApplySlotLayout(_slots[1], new Vector2(spacing * 1f, 10f), slotSize, theme != null ? theme.labelFontSize : 16, theme != null ? theme.timerFontSize : 22);
        ApplySlotLayout(_slots[2], new Vector2(spacing * 2f, 10f), slotSize, theme != null ? theme.labelFontSize : 16, theme != null ? theme.timerFontSize : 22);
        ApplySlotLayout(_slots[3], new Vector2(spacing * 3f, 10f), slotSize, theme != null ? theme.labelFontSize : 16, theme != null ? theme.timerFontSize : 22);
    }

    private static void ApplySlotLayout(SlotView slot, Vector2 position, float slotSize, int labelSize, int timerSize)
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
            slot.Label.fontSize = labelSize;
        if (slot.Timer != null)
            slot.Timer.fontSize = timerSize;
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
    }

    private void HandleAbilitySetChanged() { }

    private void ClearBindings()
    {
        if (_abilityHandler != null)
            _abilityHandler.OnAbilitySetChanged -= HandleAbilitySetChanged;

        _abilityHandler = null;
        _dash = null;
        _passive = null;
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
            ? _abilityHandler.GetSlotDisplayName(AbilitySlot.Dash)
            : string.Empty;

        RefreshCooldownSlot(slot, unlocked, cooldownRemaining, cooldownTotal,
            string.IsNullOrEmpty(label) ? "Dash" : label, unlockWave: 1);
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
        string custom = _abilityHandler.GetSlotDisplayName(abilitySlot);
        int unlockWave = _abilityHandler.GetSlotUnlockWave(abilitySlot);

        RefreshCooldownSlot(slot, unlocked, cooldownRemaining, cooldownTotal,
            string.IsNullOrEmpty(custom) ? fallbackLabel : custom, unlockWave);
    }

    private static void RefreshCooldownSlot(
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
            slot.Label.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        if (!unlocked)
        {
            if (slot.LockOverlay != null)
                slot.LockOverlay.gameObject.SetActive(true);
            if (slot.CooldownFill != null)
            {
                slot.CooldownFill.fillAmount = 1f;
                slot.CooldownFill.gameObject.SetActive(true);
            }
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

        //tradução maneira
        string passiveLabel = IsPortuguese() ? "Passiva" : "Passive";
        bool hasPassive = _passive != null && _passive.PassiveKillsRequired > 0;
        if (slot.LockOverlay != null)
            slot.LockOverlay.gameObject.SetActive(!hasPassive);

        if (!hasPassive)
        {
            if (slot.CooldownFill != null)
            {
                slot.CooldownFill.fillAmount = 0f;
                slot.CooldownFill.gameObject.SetActive(false);
            }
            if (slot.Timer != null)
            {
                slot.Timer.gameObject.SetActive(false);
                slot.Timer.text = string.Empty;
            }
            if (slot.Label != null)
                slot.Label.text = passiveLabel;
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

            if (slot.Timer != null)
            {
                slot.Timer.gameObject.SetActive(true);
                slot.Timer.text = $"{_passive.PassiveTimeRemaining:0.#}";
            }

            if (slot.Label != null)
                slot.Label.text = passiveLabel;
            return;
        }

        int required = Mathf.Max(1, _passive.PassiveKillsRequired);
        float progress = (float)_passive.PassiveKillProgress / required;
        if (slot.CooldownFill != null)
        {
            slot.CooldownFill.fillAmount = 1f - progress;
            slot.CooldownFill.gameObject.SetActive(progress < 0.999f);
        }

        if (slot.Timer != null)
        {
            slot.Timer.gameObject.SetActive(true);
            slot.Timer.text = $"{_passive.PassiveKillProgress}/{required}";
        }

        if (slot.Label != null)
            slot.Label.text = passiveLabel;
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
        Vector2 margin = theme != null ? theme.anchoredPosition : new Vector2(28f, 28f);
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

        float spacing = theme != null ? theme.slotSpacing : 88f;
        float slotSize = theme != null ? theme.slotSize : 76f;
        int labelSize = theme != null ? theme.labelFontSize : 16;
        int timerSize = theme != null ? theme.timerFontSize : 22;
        Vector2 anchor = ResolveHudAnchorPosition();
        _root.anchoredPosition = anchor;
        _root.sizeDelta = new Vector2(spacing * 3f + slotSize + 20f, slotSize + 20f);

        _slots[0] = CreateSlot("PassiveSlot", AbilitySlot.Ability1, "Passiva", passiveIcon, new Vector2(0f, 10f), slotSize, labelSize, timerSize);
        _slots[1] = CreateSlot("DashSlot", AbilitySlot.Dash, "Dash", dashIcon, new Vector2(spacing * 1f, 10f), slotSize, labelSize, timerSize);
        _slots[2] = CreateSlot("Ability1Slot", AbilitySlot.Ability1, "Q", ability1Icon, new Vector2(spacing * 2f, 10f), slotSize, labelSize, timerSize);
        _slots[3] = CreateSlot("Ability2Slot", AbilitySlot.Ability2, "R", ability2Icon, new Vector2(spacing * 3f, 10f), slotSize, labelSize, timerSize);
    }

    private SlotView CreateSlot(
        string name,
        AbilitySlot slot,
        string label,
        Sprite iconSprite,
        Vector2 position,
        float slotSize,
        int labelFontSize,
        int timerFontSize)
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
        Stretch(fillGo.GetComponent<RectTransform>());
        Image fill = fillGo.GetComponent<Image>();
        fill.color = theme != null ? theme.cooldownOverlayColor : new Color(0f, 0f, 0f, 0.65f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Top;
        fill.gameObject.SetActive(false);

        GameObject lockGo = CreateUiObject("Lock", rt);
        Stretch(lockGo.GetComponent<RectTransform>());
        Image lockImg = lockGo.GetComponent<Image>();
        lockImg.color = new Color(0f, 0f, 0f, 0.7f);
        lockGo.SetActive(false);

        Text title = CreateText(rt, label, labelFontSize, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0.3f));
        Text timer = CreateText(rt, string.Empty, timerFontSize, TextAnchor.UpperCenter, new Vector2(0f, 0.3f), new Vector2(1f, 1f));
        timer.gameObject.SetActive(false);

        return new SlotView
        {
            Slot = slot,
            Icon = icon,
            Background = bg,
            CooldownFill = fill,
            LockOverlay = lockImg,
            Label = title,
            Timer = timer
        };
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        LoadingProgressUtility.ApplySolidSprite(go.GetComponent<Image>());
        return go;
    }

    private static Text CreateText(Transform parent, string text, int size, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
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
        label.color = Color.white;
        label.font = GetRuntimeFont();
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private static Font GetRuntimeFont()
    {
        if (_runtimeFont != null)
            return _runtimeFont;

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
}
