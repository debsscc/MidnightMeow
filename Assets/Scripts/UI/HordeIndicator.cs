using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Exibe wave atual, inimigos restantes e kills. Fallback de texto só quando não há referência na cena.
// Textos traduzidos direto no script (pt-BR / en-US) conforme o idioma ativo.
[DisallowMultipleComponent]
public class HordeIndicator : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private bool buildTextIfMissing = true;

    private int _currentWave = -1;
    private int _totalWaves = -1;
    private int _enemiesRemaining = -1;
    private int _totalKilled = -1;
    private bool _hasData;

    private void Awake()
    {
        EnsureConfigured();
    }

    private void OnEnable()
    {
        GameEvents.OnWaveStatusChanged += UpdateHorde;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        UpdateUI();
    }

    private void OnDisable()
    {
        GameEvents.OnWaveStatusChanged -= UpdateHorde;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    // Garante objeto ativo e texto configurado (cena ou fallback procedural)
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
        label.text = BuildText();
        return label;
    }

    private void UpdateHorde(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
    {
        _currentWave = currentWave;
        _totalWaves = totalWaves;
        _enemiesRemaining = enemiesRemaining;
        _totalKilled = totalKilled;
        _hasData = true;
        UpdateUI();
    }

    private void HandleLocaleChanged(Locale _) => UpdateUI();

    private void UpdateUI()
    {
        if (text != null)
            text.text = BuildText();
    }

    private string BuildText()
    {
        //tradução maneira
        bool pt = IsPortuguese();

        if (!_hasData)
            return pt ? "Onda: -/-  |  Restantes: -" : "Wave: -/-  |  Remaining: -";

        return pt
            ? $"Onda: {_currentWave}/{_totalWaves}  |  Restantes: {_enemiesRemaining}  |  Abates: {_totalKilled}"
            : $"Wave: {_currentWave}/{_totalWaves}  |  Remaining: {_enemiesRemaining}  |  Kills: {_totalKilled}";
    }

    private static bool IsPortuguese()
    {
        if (!LocalizationSettings.HasSettings)
            return true;

        Locale locale = LocalizationSettings.SelectedLocale;
        // Sem locale definido, assume português (idioma base do projeto).
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }
}
