using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configura e atualiza barras de progresso de loading.
/// Imagens Filled precisam de sprite; sem isso o fillAmount não aparece na tela.
/// </summary>
public static class LoadingProgressUtility
{
    private static Sprite _uiSprite;

    public static Sprite GetUiSprite()
    {
        if (_uiSprite != null)
            return _uiSprite;

        _uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.sprite");
        if (_uiSprite == null)
            _uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");

        return _uiSprite;
    }

    public static void ConfigureFillImage(Image image)
    {
        if (image == null)
            return;

        Sprite sprite = GetUiSprite();
        if (sprite != null)
            image.sprite = sprite;

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.preserveAspect = false;
        image.useSpriteMesh = false;
    }

    public static void SetProgress(Image image, float progress)
    {
        if (image == null)
            return;

        progress = Mathf.Clamp01(progress);
        ConfigureFillImage(image);

        if (image.sprite != null)
        {
            image.fillAmount = progress;
            return;
        }

        ApplyAnchorProgress(image.rectTransform, progress);
    }

    public static void ResetProgress(Image image)
    {
        SetProgress(image, 0f);
    }

    private static void ApplyAnchorProgress(RectTransform fillRect, float progress)
    {
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(progress, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }
}
