//--------------------------------
// FEITO POR: PEDRO CAURIO
// DESCRICAO: Retrato Nix/Cora na Characters — idle / hover / selecionado (local ou outro jogador).
// --------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterPortraitVisual : MonoBehaviour
{
    public enum PortraitState
    {
        Deselected,
        Selected,
        Hover,
        TakenByOther
    }

    [SerializeField] private GameObject deselectedRoot;
    [SerializeField] private GameObject selectedRoot;
    [SerializeField] private GameObject animationRoot;

    [Header("Sprites (opcional — resolve por nome se vazio)")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite selectedSprite;

    [SerializeField] private string idleSpriteName;
    [SerializeField] private string hoverSpriteName;
    [SerializeField] private string selectedSpriteName;

    /// <summary>Estado persistente (sem hover): idle ou selecionado.</summary>
    private PortraitState _baseState = PortraitState.Deselected;
    private Image _displayImage;
    private bool _hovering;

    private void Awake()
    {
        if (deselectedRoot == null)
            deselectedRoot = transform.Find("Desselected")?.gameObject;
        if (selectedRoot == null)
            selectedRoot = transform.Find("Selected")?.gameObject;
        if (animationRoot == null)
            animationRoot = transform.Find("Animation")?.gameObject;

        // Um único Image ativo evita PointerExit falso ao trocar de root no hover.
        _displayImage = deselectedRoot != null ? deselectedRoot.GetComponent<Image>() : null;

        ResolveSprites();
        DisableRootRaycast();
        CollapseVariantRoots();
        WireHoverTarget();
        RefreshDisplay();
    }

    /// <summary>
    /// Configura nomes das sprites (CharactersScreenController). Resolve imediatamente se Awake já rodou.
    /// </summary>
    public void ConfigureSpriteNames(string idle, string hover, string selected)
    {
        idleSpriteName = idle;
        hoverSpriteName = hover;
        selectedSpriteName = selected;
        ResolveSprites();
        RefreshDisplay();
    }

    private void ResolveSprites()
    {
        if (idleSprite == null && !string.IsNullOrEmpty(idleSpriteName))
            idleSprite = FindPortraitSprite(idleSpriteName);
        if (hoverSprite == null && !string.IsNullOrEmpty(hoverSpriteName))
            hoverSprite = FindPortraitSprite(hoverSpriteName);
        if (selectedSprite == null && !string.IsNullOrEmpty(selectedSpriteName))
            selectedSprite = FindPortraitSprite(selectedSpriteName);
    }

    /// <summary>
    /// Prefere nome exato ou sufixo _0 (Sprite Mode Multiple).
    /// </summary>
    private static Sprite FindPortraitSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return null;

        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        Sprite exact = null;
        Sprite withZero = null;

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
                continue;

            string name = sprite.name;
            if (name == spriteName)
                exact = sprite;
            else if (name == spriteName + "_0")
                withZero = sprite;
        }

        // Multiple (_0) costuma ser a arte nova em NOVAS COISAS JU.
        return withZero != null ? withZero : exact;
    }

    private void DisableRootRaycast()
    {
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
            rootImage.raycastTarget = false;
    }

    private void CollapseVariantRoots()
    {
        if (deselectedRoot != null)
            deselectedRoot.SetActive(true);

        // Selected/Animation ficam só como referência visual na cena; o display usa Desselected.
        if (selectedRoot != null)
            selectedRoot.SetActive(false);
        if (animationRoot != null)
            animationRoot.SetActive(false);
    }

    private void WireHoverTarget()
    {
        if (deselectedRoot == null)
            return;

        if (_displayImage != null)
            _displayImage.raycastTarget = true;

        EventTrigger trigger = deselectedRoot.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = deselectedRoot.AddComponent<EventTrigger>();

        trigger.triggers.RemoveAll(entry =>
            entry.eventID == EventTriggerType.PointerEnter
            || entry.eventID == EventTriggerType.PointerExit);

        AddHoverEntry(trigger, EventTriggerType.PointerEnter, true);
        AddHoverEntry(trigger, EventTriggerType.PointerExit, false);
    }

    private void AddHoverEntry(EventTrigger trigger, EventTriggerType type, bool hovering)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => SetHovering(hovering));
        trigger.triggers.Add(entry);
    }

    private bool IsSelectedBase =>
        _baseState == PortraitState.Selected || _baseState == PortraitState.TakenByOther;

    private void RefreshDisplay()
    {
        CollapseVariantRoots();

        Sprite sprite;
        if (IsSelectedBase)
            sprite = selectedSprite != null ? selectedSprite : idleSprite;
        else if (_hovering)
            sprite = hoverSprite != null ? hoverSprite : idleSprite;
        else
            sprite = idleSprite;

        if (_displayImage == null)
            return;

        if (sprite != null)
            _displayImage.sprite = sprite;
        _displayImage.color = Color.white;
    }

    public void SetHovering(bool hovering)
    {
        // Já selecionado: mantém OutroPlayer; hover não troca a arte.
        if (IsSelectedBase)
        {
            _hovering = false;
            RefreshDisplay();
            return;
        }

        _hovering = hovering;
        RefreshDisplay();
    }

    public void SetBaseState(PortraitState state)
    {
        if (state == PortraitState.Hover)
            state = PortraitState.Deselected;

        _baseState = state;

        if (IsSelectedBase)
            _hovering = false;

        RefreshDisplay();
    }

    /// <summary>Compat: Apply força o estado base (ignora Hover como persistente).</summary>
    public void Apply(PortraitState state)
    {
        SetBaseState(state);
    }
}
