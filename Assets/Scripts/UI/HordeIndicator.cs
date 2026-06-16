using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe wave atual, inimigos restantes e kills. Fallback de texto só quando não há referência na cena.
/// </summary>
[DisallowMultipleComponent]
public class HordeIndicator : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private bool buildTextIfMissing = true;

    private string _currentHorde = "Wave: -/-  |  Restantes: -";

    private void Awake()
    {
        EnsureConfigured();
    }

    private void OnEnable()
    {
        GameEvents.OnWaveStatusChanged += UpdateHorde;
        UpdateUI();
    }

    private void OnDisable()
    {
        GameEvents.OnWaveStatusChanged -= UpdateHorde;
    }

    /// <summary>Garante objeto ativo e texto configurado (cena ou fallback procedural).</summary>
    public void EnsureConfigured()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (text == null)
            text = GetComponent<Text>();

        EnsureRectTransform();
        if (text == null && buildTextIfMissing)
            text = CreateFallbackText();

        UpdateUI();
    }

    private void EnsureRectTransform()
    {
        RectTransform rt = transform as RectTransform;
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(720f, 48f);
    }

    private Text CreateFallbackText()
    {
        GameObject textGo = new GameObject("WaveText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text label = textGo.GetComponent<Text>();
        label.alignment = TextAnchor.UpperCenter;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.text = _currentHorde;
        return label;
    }

    private void UpdateHorde(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
    {
        _currentHorde = $"Wave: {currentWave}/{totalWaves}  |  Restantes: {enemiesRemaining}  |  Kills: {totalKilled}";
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (text != null)
            text.text = _currentHorde;
    }
}
