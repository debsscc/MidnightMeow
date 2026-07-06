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
    [SerializeField] private Color takenByOtherTint = new(0.55f, 0.45f, 0.45f, 1f);

    private PortraitState _state = PortraitState.Deselected;
    private Image _deselectedImage;
    private Color _deselectedBaseColor = Color.white;
    private bool _hovering;

    private void Awake()
    {
        if (deselectedRoot == null)
            deselectedRoot = transform.Find("Desselected")?.gameObject;
        if (selectedRoot == null)
            selectedRoot = transform.Find("Selected")?.gameObject;
        if (animationRoot == null)
            animationRoot = transform.Find("Animation")?.gameObject;

        if (deselectedRoot != null)
        {
            _deselectedImage = deselectedRoot.GetComponent<Image>();
            if (_deselectedImage != null)
                _deselectedBaseColor = _deselectedImage.color;
        }

        DisableRootRaycast();
        WireHoverTargets();
        Apply(_state);
    }

    private void DisableRootRaycast()
    {
        Image rootImage = GetComponent<Image>();
        if (rootImage != null)
            rootImage.raycastTarget = false;
    }

    private void WireHoverTargets()
    {
        WireHover(deselectedRoot);
        WireHover(selectedRoot);
        WireHover(animationRoot);
    }

    private void WireHover(GameObject target)
    {
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        AddHoverEntry(trigger, EventTriggerType.PointerEnter, true);
        AddHoverEntry(trigger, EventTriggerType.PointerExit, false);
    }

    private void AddHoverEntry(EventTrigger trigger, EventTriggerType type, bool hovering)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => SetHovering(hovering));
        trigger.triggers.Add(entry);
    }

    public void Apply(PortraitState state)
    {
        _state = state;
        bool taken = state == PortraitState.TakenByOther;
        bool selected = state == PortraitState.Selected;
        bool hover = state == PortraitState.Hover;

        if (deselectedRoot != null)
            deselectedRoot.SetActive(!selected && !hover);

        if (selectedRoot != null)
            selectedRoot.SetActive(selected);

        if (animationRoot != null)
            animationRoot.SetActive(hover);

        if (_deselectedImage != null)
            _deselectedImage.color = taken && !hover ? takenByOtherTint : _deselectedBaseColor;
    }

    public void SetHovering(bool hovering)
    {
        if (_state == PortraitState.Selected || _state == PortraitState.TakenByOther)
        {
            _hovering = false;
            Apply(_state);
            return;
        }

        _hovering = hovering;
        if (_hovering && _state == PortraitState.Deselected)
            Apply(PortraitState.Hover);
        else
            Apply(_state);
    }

    public void SetBaseState(PortraitState state)
    {
        _state = state;

        if (state == PortraitState.Selected || state == PortraitState.TakenByOther)
            _hovering = false;

        if (_hovering && state == PortraitState.Deselected)
            Apply(PortraitState.Hover);
        else
            Apply(state);
    }
}
