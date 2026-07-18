using UnityEngine;

///* ----------------------------------------------------------------
// ATUALIZADO EM: 18-07-2026
// DESCRIÇÃO: Matemática pura de letterbox/pillarbox para aspect fixo (ex.: 16:9).
// ---------------------------------------------------------------- */

/// <summary>
/// Calcula o viewport normalizado (0–1) centrado que preserva um aspect ratio alvo.
/// </summary>
public static class LetterboxAspectMath
{
    public const float DefaultTargetAspect = 16f / 9f;

    /// <summary>
    /// Retorna o retângulo normalizado do viewport (como em <see cref="Camera.rect"/>).
    /// Telas mais largas que o alvo → pillarbox (barras laterais).
    /// Telas mais altas → letterbox (barras em cima/baixo).
    /// </summary>
    public static Rect CalculateNormalizedViewport(int screenWidth, int screenHeight, float targetAspect)
    {
        if (screenWidth <= 0 || screenHeight <= 0 || targetAspect <= 0f)
            return new Rect(0f, 0f, 1f, 1f);

        float screenAspect = (float)screenWidth / screenHeight;

        if (Mathf.Abs(screenAspect - targetAspect) < 0.0001f)
            return new Rect(0f, 0f, 1f, 1f);

        if (screenAspect > targetAspect)
        {
            float width = targetAspect / screenAspect;
            float x = (1f - width) * 0.5f;
            return new Rect(x, 0f, width, 1f);
        }

        float height = screenAspect / targetAspect;
        float y = (1f - height) * 0.5f;
        return new Rect(0f, y, 1f, height);
    }

    /// <summary>
    /// True quando o viewport não cobre a tela inteira (há barras pretas).
    /// </summary>
    public static bool HasBars(Rect normalizedViewport)
    {
        return normalizedViewport.x > 0.0001f
               || normalizedViewport.y > 0.0001f
               || normalizedViewport.width < 0.9999f
               || normalizedViewport.height < 0.9999f;
    }
}
