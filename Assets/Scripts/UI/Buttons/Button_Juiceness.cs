///* ----------------------------------------------------------------
// CRIADO EM: 17-02-2026
// FEITO POR: Debora Carvalho
// DESCRIÇÃO: Juiceness pros botões.
// ---------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.EventSystems;

public class Button_Juiceness : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 10f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * 1.1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * 0.9f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale * 1.1f;
    }
}