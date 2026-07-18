//--------------------------------------------------
// FUNÇÃO: Zoom simples de Image (lightbox) — backdrop escuro + preview ampliado.
//--------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Overlay de zoom para ver melhor uma Image: fundo escurecido/suave e preview central.
/// Clique no backdrop, na imagem ou Escape fecha.
/// </summary>
[DisallowMultipleComponent]
public class UiSimpleImageZoomOverlay : MonoBehaviour
{
    private const float OpenDuration = 0.18f;
    private const float CloseDuration = 0.14f;
    private const float ZoomedMaxWidth = 980f;
    private const float ZoomedMaxHeight = 620f;

    private RectTransform _root;
    private CanvasGroup _group;
    private Image _backdrop;
    private Image _blurHint;
    private Image _preview;
    private Coroutine _anim;
    private bool _isOpen;
    private Image _source;

    public bool IsOpen => _isOpen;

    public static UiSimpleImageZoomOverlay EnsureOnCanvas(Transform canvasRoot)
    {
        if (canvasRoot == null)
            return null;

        UiSimpleImageZoomOverlay existing = canvasRoot.GetComponentInChildren<UiSimpleImageZoomOverlay>(true);
        if (existing != null)
            return existing;

        GameObject go = new GameObject("ImageZoomOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(UiSimpleImageZoomOverlay));
        go.transform.SetParent(canvasRoot, false);
        UiSimpleImageZoomOverlay overlay = go.GetComponent<UiSimpleImageZoomOverlay>();
        overlay.Build();
        go.SetActive(false);
        return overlay;
    }

    public void ToggleFrom(Image source)
    {
        if (source == null)
            return;

        if (_isOpen && _source == source)
        {
            Close();
            return;
        }

        Open(source);
    }

    public void Open(Image source)
    {
        if (source == null || source.sprite == null)
            return;

        if (_root == null)
            Build();

        _source = source;
        _preview.sprite = source.sprite;
        _preview.preserveAspect = true;
        _preview.color = Color.white;

        if (_blurHint != null)
        {
            _blurHint.sprite = source.sprite;
            _blurHint.preserveAspect = false;
            _blurHint.color = new Color(0.35f, 0.35f, 0.38f, 0.35f);
        }

        FitPreviewToSprite(source.sprite);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        _isOpen = true;

        if (_anim != null)
            StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateOpen());
    }

    public void Close()
    {
        if (!_isOpen && !gameObject.activeSelf)
            return;

        if (_anim != null)
            StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateClose());
    }

    private void Update()
    {
        if (!_isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void Build()
    {
        _root = GetComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

        _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = true;
        _group.interactable = true;

        _backdrop = CreateImage("Backdrop", transform, new Color(0.02f, 0.02f, 0.04f, 0.72f));
        StretchFull(_backdrop.rectTransform);
        _backdrop.raycastTarget = true;
        AddClick(_backdrop.gameObject, Close);

        _blurHint = CreateImage("BlurHint", transform, new Color(0.35f, 0.35f, 0.38f, 0.35f));
        StretchFull(_blurHint.rectTransform);
        _blurHint.raycastTarget = false;

        _preview = CreateImage("ZoomedPreview", transform, Color.white);
        RectTransform previewRt = _preview.rectTransform;
        previewRt.anchorMin = new Vector2(0.5f, 0.5f);
        previewRt.anchorMax = new Vector2(0.5f, 0.5f);
        previewRt.pivot = new Vector2(0.5f, 0.5f);
        previewRt.sizeDelta = new Vector2(720f, 420f);
        _preview.preserveAspect = true;
        _preview.raycastTarget = true;
        AddClick(_preview.gameObject, Close);
    }

    private void FitPreviewToSprite(Sprite sprite)
    {
        if (_preview == null || sprite == null)
            return;

        float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        float width = ZoomedMaxWidth;
        float height = width / aspect;
        if (height > ZoomedMaxHeight)
        {
            height = ZoomedMaxHeight;
            width = height * aspect;
        }

        _preview.rectTransform.sizeDelta = new Vector2(width, height);
    }

    private IEnumerator AnimateOpen()
    {
        _group.alpha = 0f;
        RectTransform previewRt = _preview.rectTransform;
        Vector3 startScale = Vector3.one * 0.88f;
        previewRt.localScale = startScale;

        float t = 0f;
        while (t < OpenDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / OpenDuration);
            float eased = 1f - (1f - u) * (1f - u);
            _group.alpha = eased;
            previewRt.localScale = Vector3.Lerp(startScale, Vector3.one, eased);
            yield return null;
        }

        _group.alpha = 1f;
        previewRt.localScale = Vector3.one;
        _anim = null;
    }

    private IEnumerator AnimateClose()
    {
        RectTransform previewRt = _preview != null ? _preview.rectTransform : null;
        Vector3 startScale = previewRt != null ? previewRt.localScale : Vector3.one;
        float startAlpha = _group != null ? _group.alpha : 1f;

        float t = 0f;
        while (t < CloseDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / CloseDuration);
            float eased = u * u;
            if (_group != null)
                _group.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            if (previewRt != null)
                previewRt.localScale = Vector3.Lerp(startScale, Vector3.one * 0.92f, eased);
            yield return null;
        }

        if (_group != null)
            _group.alpha = 0f;
        _isOpen = false;
        _source = null;
        _anim = null;
        gameObject.SetActive(false);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        LoadingProgressUtility.ApplySolidSprite(image);
        image.color = color;
        return image;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddClick(GameObject target, UnityEngine.Events.UnityAction onClick)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
        trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerClick);

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(_ => onClick?.Invoke());
        trigger.triggers.Add(entry);
    }
}
