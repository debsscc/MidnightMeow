using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FrenzyHudEffect : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("RectTransform do elemento que vai tremer")]
    [SerializeField] private RectTransform hudElement;
    [Tooltip("Image usada como glow por cima do elemento, deve ser uma imagem de brilho radial com alpha")]
    [SerializeField] private Image glowImage;

    [Header("Shake")]
    [SerializeField] private float shakeIntensity = 6f;
    [SerializeField] private float shakeSpeed = 40f;

    [Header("Glow")]
    [SerializeField] private Color glowColor = new Color(1f, 0.4f, 0f, 0.8f);
    [SerializeField] private float glowPulseSpeed = 4f;

    private Vector2 _originalPos;
    private Color _originalGlowColor;
    private Coroutine _shakeRoutine;
    private Coroutine _glowRoutine;
    private bool _frenzyActive = false;

    private void Awake()
    {
        if (hudElement != null)
            _originalPos = hudElement.anchoredPosition;

        if (glowImage != null)
            _originalGlowColor = glowImage.color;
    }

    // Chamar esse método pelo UnityEvent OnFrenzyActivated do PlayerAdrenaline
    public void OnFrenzyStart()
    {
        _frenzyActive = true;

        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        if (_glowRoutine != null) StopCoroutine(_glowRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine());
        _glowRoutine = StartCoroutine(GlowRoutine());
    }

    // Chamar esse método pelo UnityEvent OnFrenzyDeactivated do PlayerAdrenaline
    public void OnFrenzyEnd()
    {
        _frenzyActive = false;
    }

    private IEnumerator ShakeRoutine()
    {
        // Shake por 0.3s ao ativar
        float burstTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < burstTime)
        {
            float x = Mathf.Sin(elapsed * shakeSpeed) * shakeIntensity * (1f - elapsed / burstTime);
            float y = Mathf.Cos(elapsed * shakeSpeed * 1.3f) * shakeIntensity * (1f - elapsed / burstTime);
            if (hudElement != null)
                hudElement.anchoredPosition = _originalPos + new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (hudElement != null)
            hudElement.anchoredPosition = _originalPos;

        // Tremor suave enquanto o frenesi ta ativo
        while (_frenzyActive)
        {
            float t = Time.time;
            float x = Mathf.Sin(t * shakeSpeed * 0.5f) * shakeIntensity * 0.2f;
            float y = Mathf.Cos(t * shakeSpeed * 0.7f) * shakeIntensity * 0.2f;
            if (hudElement != null)
                hudElement.anchoredPosition = _originalPos + new Vector2(x, y);
            yield return null;
        }

        if (hudElement != null)
            hudElement.anchoredPosition = _originalPos;
    }

    private IEnumerator GlowRoutine()
    {
        if (glowImage == null) yield break;

        // Fade in rápido a partir da cor original
        float fadeIn = 0.15f;
        for (float t = 0; t < fadeIn; t += Time.deltaTime)
        {
            glowImage.color = Color.Lerp(_originalGlowColor, glowColor, t / fadeIn);
            yield return null;
        }
        glowImage.color = glowColor;

        // Pulsa enquanto o frenesi ta ativo
        while (_frenzyActive)
        {
            float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);
            Color c = glowColor;
            c.a = alpha;
            glowImage.color = c;
            yield return null;
        }

        // Fade out — volta para a cor original do fill
        Color current = glowImage.color;
        float fadeOut = 0.3f;
        for (float t = 0; t < fadeOut; t += Time.deltaTime)
        {
            glowImage.color = Color.Lerp(current, _originalGlowColor, t / fadeOut);
            yield return null;
        }
        glowImage.color = _originalGlowColor;
    }
}
