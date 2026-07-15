///* ----------------------------------------------------------------
// DESCRIÇÃO: Escala suave nos botões (hover / press) — complementa ColorTint.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class Button_Juiceness : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.07f;
    [SerializeField] private float pressScale = 0.94f;
    [SerializeField] private float lerpSpeed = 14f;

    private Selectable _selectable;
    private Vector3 _originalScale;
    private Vector3 _targetScale;
    private bool _pointerInside;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        CaptureOriginalScale();
    }

    private void OnEnable()
    {
        CaptureOriginalScale();
        _targetScale = _originalScale;
        transform.localScale = _originalScale;
        _pointerInside = false;
    }

    private void OnDisable()
    {
        transform.localScale = _originalScale;
        _targetScale = _originalScale;
        _pointerInside = false;
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            _targetScale,
            Time.unscaledDeltaTime * lerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;
        RefreshTargetScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;
        RefreshTargetScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        _targetScale = _originalScale * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RefreshTargetScale();
    }

    public void OnSelect(BaseEventData eventData)
    {
        RefreshTargetScale();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _pointerInside = false;
        RefreshTargetScale();
    }

    private void RefreshTargetScale()
    {
        if (!IsInteractable())
        {
            _targetScale = _originalScale;
            return;
        }

        if (_pointerInside || IsSelected())
            _targetScale = _originalScale * hoverScale;
        else
            _targetScale = _originalScale;
    }

    private bool IsSelected()
    {
        return EventSystem.current != null
               && EventSystem.current.currentSelectedGameObject == gameObject;
    }

    private bool IsInteractable() => _selectable == null || _selectable.IsInteractable();

    private void CaptureOriginalScale()
    {
        Vector3 scale = transform.localScale;
        if (scale.sqrMagnitude < 0.0001f)
            scale = Vector3.one;

        _originalScale = scale;
    }
}
