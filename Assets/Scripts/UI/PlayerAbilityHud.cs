using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD de gameplay (canto inferior direito): cooldowns de Dash/Q/R e progresso da passiva.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAbilityHud : MonoBehaviour
{
    private sealed class SlotView
    {
        public AbilitySlot Slot;
        public Image Background;
        public Image CooldownFill;
        public Image LockOverlay;
        public TMP_Text Label;
        public TMP_Text Timer;
    }

    [SerializeField] private bool buildIfMissing = true;

    private PlayerAbilityHandler _abilityHandler;
    private PlayerDash _dash;
    private PlayerPassiveHandler _passive;

    private RectTransform _root;
    private Image _passiveFill;
    private Image _passiveLock;
    private TMP_Text _passiveLabel;
    private readonly SlotView[] _slots = new SlotView[3];

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
        if (buildIfMissing && _root == null)
            BuildUi();
    }

    private void LateUpdate()
    {
        if (_abilityHandler == null)
        {
            TryBindLocalPlayer();
            if (_abilityHandler == null)
                return;
        }

        RefreshPassive();
        RefreshSlot(_slots[0], AbilitySlot.Dash, "Dash", GetDashCooldownRemaining(), GetDashCooldownTotal());
        RefreshSlot(_slots[1], AbilitySlot.Ability1, "Q", _abilityHandler.GetCooldownRemaining(AbilitySlot.Ability1), _abilityHandler.GetCooldownTotal(AbilitySlot.Ability1));
        RefreshSlot(_slots[2], AbilitySlot.Ability2, "R", _abilityHandler.GetCooldownRemaining(AbilitySlot.Ability2), _abilityHandler.GetCooldownTotal(AbilitySlot.Ability2));
    }

    public static void EnsureOnCanvas(Canvas canvas)
    {
        if (canvas == null || canvas.GetComponentInChildren<PlayerAbilityHud>(true) != null)
            return;

        GameObject go = new GameObject("PlayerAbilityHud", typeof(RectTransform), typeof(PlayerAbilityHud));
        go.transform.SetParent(canvas.transform, false);
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

        _abilityHandler = player.GetComponent<PlayerAbilityHandler>();
        _dash = player.GetComponent<PlayerDash>();
        _passive = player.GetComponent<PlayerPassiveHandler>();
    }

    private void ClearBindings()
    {
        _abilityHandler = null;
        _dash = null;
        _passive = null;
    }

    private float GetDashCooldownRemaining() => _dash != null ? _dash.GetCooldownRemaining() : 0f;
    private float GetDashCooldownTotal() => _dash != null ? _dash.GetCooldownDuration() : 1f;

    private void RefreshPassive()
    {
        if (_passiveFill == null)
            return;

        bool hasPassive = _passive != null && _passive.PassiveKillsRequired > 0;
        if (_passiveLabel != null)
            _passiveLabel.gameObject.SetActive(hasPassive);
        if (_passiveLock != null)
            _passiveLock.gameObject.SetActive(!hasPassive);

        if (!hasPassive)
        {
            _passiveFill.fillAmount = 0f;
            return;
        }

        if (_passive.IsPassiveActive)
        {
            float duration = Mathf.Max(0.01f, _passive.PassiveDuration);
            _passiveFill.fillAmount = _passive.PassiveTimeRemaining / duration;
            if (_passiveLabel != null)
                _passiveLabel.text = $"Passiva {_passive.PassiveTimeRemaining:0.#}s";
            if (_passiveLock != null)
                _passiveLock.gameObject.SetActive(false);
            return;
        }

        int required = Mathf.Max(1, _passive.PassiveKillsRequired);
        _passiveFill.fillAmount = (float)_passive.PassiveKillProgress / required;
        if (_passiveLabel != null)
            _passiveLabel.text = $"Passiva {_passive.PassiveKillProgress}/{required}";
    }

    private void RefreshSlot(SlotView slot, AbilitySlot abilitySlot, string fallbackLabel, float cooldownRemaining, float cooldownTotal)
    {
        if (slot == null || _abilityHandler == null)
            return;

        bool unlocked = _abilityHandler.IsSlotUnlocked(abilitySlot);
        if (slot.LockOverlay != null)
            slot.LockOverlay.gameObject.SetActive(!unlocked);

        if (slot.Label != null)
        {
            string custom = _abilityHandler.GetSlotDisplayName(abilitySlot);
            slot.Label.text = string.IsNullOrEmpty(custom) ? fallbackLabel : custom;
            slot.Label.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        float total = Mathf.Max(0.01f, cooldownTotal);
        float ratio = unlocked ? Mathf.Clamp01(cooldownRemaining / total) : 1f;
        if (slot.CooldownFill != null)
        {
            slot.CooldownFill.fillAmount = ratio;
            slot.CooldownFill.gameObject.SetActive(unlocked && cooldownRemaining > 0.01f);
        }

        if (slot.Timer != null)
        {
            bool showTimer = unlocked && cooldownRemaining > 0.05f;
            slot.Timer.gameObject.SetActive(showTimer);
            if (showTimer)
                slot.Timer.text = cooldownRemaining >= 10f ? $"{cooldownRemaining:0}" : $"{cooldownRemaining:0.#}";
        }
    }

    private void BuildUi()
    {
        if (_root != null && _slots[0] != null)
            return;

        _root = GetComponent<RectTransform>();
        if (_root == null)
            _root = gameObject.AddComponent<RectTransform>();

        _root.anchorMin = new Vector2(1f, 0f);
        _root.anchorMax = new Vector2(1f, 0f);
        _root.pivot = new Vector2(1f, 0f);
        _root.anchoredPosition = new Vector2(-24f, 24f);
        _root.sizeDelta = new Vector2(260f, 130f);

        CreatePassiveBar();
        _slots[0] = CreateSlot("DashSlot", AbilitySlot.Dash, "Dash", new Vector2(1f, 0f), new Vector2(-8f, 8f));
        _slots[1] = CreateSlot("Ability1Slot", AbilitySlot.Ability1, "Q", new Vector2(1f, 0f), new Vector2(-76f, 8f));
        _slots[2] = CreateSlot("Ability2Slot", AbilitySlot.Ability2, "R", new Vector2(1f, 0f), new Vector2(-144f, 8f));
    }

    private void CreatePassiveBar()
    {
        GameObject bar = CreateUiObject("PassiveBar", _root);
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(1f, 0f);
        barRt.anchorMax = new Vector2(1f, 0f);
        barRt.pivot = new Vector2(1f, 0f);
        barRt.anchoredPosition = new Vector2(-8f, 80f);
        barRt.sizeDelta = new Vector2(220f, 22f);

        Image track = bar.GetComponent<Image>();
        track.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);

        GameObject fillGo = CreateUiObject("Fill", barRt);
        Stretch(fillGo.GetComponent<RectTransform>());
        _passiveFill = fillGo.GetComponent<Image>();
        _passiveFill.color = new Color(0.85f, 0.55f, 0.15f, 0.95f);
        _passiveFill.type = Image.Type.Filled;
        _passiveFill.fillMethod = Image.FillMethod.Horizontal;
        _passiveFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        GameObject lockGo = CreateUiObject("Lock", barRt);
        Stretch(lockGo.GetComponent<RectTransform>());
        _passiveLock = lockGo.GetComponent<Image>();
        _passiveLock.color = new Color(0f, 0f, 0f, 0.55f);

        _passiveLabel = CreateText(barRt, "Passiva", 16, TextAlignmentOptions.Center, new Vector2(0f, 0f), Vector2.one);
    }

    private SlotView CreateSlot(string name, AbilitySlot slot, string label, Vector2 anchor, Vector2 position)
    {
        GameObject root = CreateUiObject(name, _root);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(56f, 56f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.14f, 0.92f);

        GameObject fillGo = CreateUiObject("Cooldown", rt);
        Stretch(fillGo.GetComponent<RectTransform>());
        Image fill = fillGo.GetComponent<Image>();
        fill.color = new Color(0f, 0f, 0f, 0.65f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Top;

        GameObject lockGo = CreateUiObject("Lock", rt);
        Stretch(lockGo.GetComponent<RectTransform>());
        Image lockImg = lockGo.GetComponent<Image>();
        lockImg.color = new Color(0f, 0f, 0f, 0.7f);

        TMP_Text title = CreateText(rt, label, 18, TextAlignmentOptions.Center, new Vector2(0f, 14f), new Vector2(1f, 1f));
        TMP_Text timer = CreateText(rt, "", 16, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 0.45f));

        return new SlotView
        {
            Slot = slot,
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
        return go;
    }

    private static TMP_Text CreateText(Transform parent, string text, int size, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
