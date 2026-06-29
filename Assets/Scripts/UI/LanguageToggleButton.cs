/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-06-28
DESCRIÇÃO: Botão que alterna o idioma do jogo (pt-BR <-> en-US) em runtime
via Unity Localization. Atualiza o próprio rótulo (PT/BR ou ENG) e
persiste a escolha em PlayerPrefs.
---------------------------------------------------------------- */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class LanguageToggleButton : MonoBehaviour
{
    public const string LocalePrefKey = "midnightmeow.locale";

    private const string PortugueseCode = "pt-BR";
    private const string EnglishCode = "en-US";

    private const string PortugueseLabel = "PT/BR";
    private const string EnglishLabel = "ENG";

    [Tooltip("Texto exibido no botão. Se vazio, busca um TMP_Text filho.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Botão alvo. Se vazio, usa o Button deste GameObject.")]
    [SerializeField] private Button button;

    private bool _switching;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        if (button != null)
        {
            button.onClick.RemoveListener(ToggleLanguage);
            button.onClick.AddListener(ToggleLanguage);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        yield return LocalizationSettings.InitializationOperation;
        ApplySavedLocale();
        RefreshLabel();
    }

    // Alterna entre português e inglês. 
    public void ToggleLanguage()
    {
        if (_switching)
            return;

        StartCoroutine(ToggleRoutine());
    }

    private IEnumerator ToggleRoutine()
    {
        _switching = true;

        yield return LocalizationSettings.InitializationOperation;

        string targetCode = IsPortuguese(LocalizationSettings.SelectedLocale) ? EnglishCode : PortugueseCode;
        Locale target = LocalizationSettings.AvailableLocales.GetLocale(targetCode);

        if (target != null)
        {
            LocalizationSettings.SelectedLocale = target;
            PlayerPrefs.SetString(LocalePrefKey, targetCode);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning($"[LanguageToggleButton] Locale '{targetCode}' não está em Available Locales.");
        }

        RefreshLabel();
        _switching = false;
    }

    private void ApplySavedLocale()
    {
        string saved = PlayerPrefs.GetString(LocalePrefKey, string.Empty);
        if (string.IsNullOrEmpty(saved))
            return;

        Locale target = LocalizationSettings.AvailableLocales.GetLocale(saved);
        if (target != null && LocalizationSettings.SelectedLocale != target)
            LocalizationSettings.SelectedLocale = target;
    }

    private void RefreshLabel()
    {
        if (label == null)
            return;

        label.text = IsPortuguese(LocalizationSettings.SelectedLocale) ? PortugueseLabel : EnglishLabel;
    }

    private static bool IsPortuguese(Locale locale)
    {
        // Sem locale definido, assume português
        return locale == null || locale.Identifier.Code.StartsWith("pt", System.StringComparison.OrdinalIgnoreCase);
    }
}
