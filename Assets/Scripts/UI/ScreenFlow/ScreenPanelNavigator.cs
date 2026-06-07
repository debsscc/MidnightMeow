using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Navegação local entre painéis na mesma cena (state machine leve de UI).
/// </summary>
[DisallowMultipleComponent]
public class ScreenPanelNavigator : MonoBehaviour
{
    [Serializable]
    public class PanelEntry
    {
        public string panelId;
        public GameObject root;
    }

    [SerializeField] private PanelEntry[] panels = Array.Empty<PanelEntry>();
    [SerializeField] private string defaultPanelId;

    private readonly Dictionary<string, GameObject> _lookup = new Dictionary<string, GameObject>();
    private string _currentPanelId;

    public string CurrentPanelId => _currentPanelId;
    public event Action<string> OnPanelChanged;

    private void Awake()
    {
        _lookup.Clear();
        for (int i = 0; i < panels.Length; i++)
        {
            PanelEntry entry = panels[i];
            if (entry == null || string.IsNullOrEmpty(entry.panelId) || entry.root == null)
                continue;

            if (!_lookup.ContainsKey(entry.panelId))
                _lookup.Add(entry.panelId, entry.root);
        }

        HideAll();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(defaultPanelId))
            ShowPanel(defaultPanelId);
    }

    public void RegisterPanel(string panelId, GameObject root)
    {
        if (string.IsNullOrEmpty(panelId) || root == null)
            return;

        _lookup[panelId] = root;
        root.SetActive(false);
    }

    public void ShowPanel(string panelId)
    {
        if (!_lookup.TryGetValue(panelId, out GameObject target))
        {
            Debug.LogWarning($"ScreenPanelNavigator: painel '{panelId}' não registrado.");
            return;
        }

        foreach (var pair in _lookup)
            pair.Value.SetActive(pair.Key == panelId);

        _currentPanelId = panelId;
        OnPanelChanged?.Invoke(panelId);
    }

    public void HideAll()
    {
        foreach (var pair in _lookup)
            pair.Value.SetActive(false);

        _currentPanelId = null;
    }
}
