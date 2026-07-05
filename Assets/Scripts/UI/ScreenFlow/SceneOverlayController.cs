using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gerencia sobreposições na cena atual (pause, baú, diálogo) sem carregar outra cena.
/// Coloque num Canvas da cena e registre cada overlay por ID.
/// </summary>
[DisallowMultipleComponent]
public class SceneOverlayController : MonoBehaviour
{
    [Serializable]
    public class OverlayEntry
    {
        [Tooltip("ID usado em SceneOverlayRequest e em código.")]
        public string overlayId;

        public GameObject root;

        [Tooltip("Define Time.timeScale = 0 ao abrir.")]
        public bool pauseGameTime;

        [Tooltip("Fecha outros overlays abertos antes de abrir este.")]
        public bool closeOthersOnOpen;
    }

    [SerializeField] private OverlayEntry[] overlays = Array.Empty<OverlayEntry>();

    [Header("Eventos globais desta cena")]
    public UnityEvent<string> onOverlayOpened;
    public UnityEvent<string> onOverlayClosed;

    [SerializeField] private FlowEventRelay openEvents = new FlowEventRelay();
    [SerializeField] private FlowEventRelay closeEvents = new FlowEventRelay();

    private readonly Stack<string> _openStack = new Stack<string>();
    private readonly Dictionary<string, OverlayEntry> _lookup = new Dictionary<string, OverlayEntry>();

    private void Awake()
    {
        _lookup.Clear();
        for (int i = 0; i < overlays.Length; i++)
        {
            OverlayEntry entry = overlays[i];
            if (entry == null || string.IsNullOrEmpty(entry.overlayId) || entry.root == null)
                continue;

            if (!_lookup.ContainsKey(entry.overlayId))
                _lookup.Add(entry.overlayId, entry);

            entry.root.SetActive(false);
        }
    }

    public bool IsOpen(string overlayId) => _openStack.Count > 0 && _openStack.Peek() == overlayId;

    public void OpenOverlay(string overlayId)
    {
        if (!_lookup.TryGetValue(overlayId, out OverlayEntry entry))
        {
            Debug.LogWarning($"SceneOverlayController: overlay '{overlayId}' não registrado.");
            return;
        }

        if (entry.closeOthersOnOpen)
            CloseAllOverlays();

        entry.root.SetActive(true);
        _openStack.Push(overlayId);

        GameplayHudController.BringOverlayToFront(entry.root.transform);

        if (entry.pauseGameTime)
            Time.timeScale = 0f;

        openEvents.InvokeBefore(this);
        onOverlayOpened?.Invoke(overlayId);
    }

    public void CloseOverlay(string overlayId)
    {
        if (!_lookup.TryGetValue(overlayId, out OverlayEntry entry))
            return;

        if (entry.root.activeSelf)
        {
            entry.root.SetActive(false);
            closeEvents.InvokeBefore(this);
            onOverlayClosed?.Invoke(overlayId);
        }

        RemoveFromStack(overlayId);
        RestoreTimeScaleIfNeeded();
        closeEvents.InvokeAfter(this);
    }

    public void CloseTopOverlay()
    {
        if (_openStack.Count == 0)
            return;

        string top = _openStack.Pop();
        if (_lookup.TryGetValue(top, out OverlayEntry entry))
            entry.root.SetActive(false);

        onOverlayClosed?.Invoke(top);
        RestoreTimeScaleIfNeeded();
        closeEvents.InvokeAfter(this);
    }

    public void CloseAllOverlays()
    {
        while (_openStack.Count > 0)
        {
            string id = _openStack.Pop();
            if (_lookup.TryGetValue(id, out OverlayEntry entry))
                entry.root.SetActive(false);
            onOverlayClosed?.Invoke(id);
        }

        Time.timeScale = 1f;
    }

    private void RemoveFromStack(string overlayId)
    {
        if (_openStack.Count == 0)
            return;

        var buffer = new Stack<string>();
        while (_openStack.Count > 0)
        {
            string id = _openStack.Pop();
            if (id != overlayId)
                buffer.Push(id);
        }

        while (buffer.Count > 0)
            _openStack.Push(buffer.Pop());
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (_openStack.Count == 0)
        {
            Time.timeScale = 1f;
            return;
        }

        if (_lookup.TryGetValue(_openStack.Peek(), out OverlayEntry top) && top.pauseGameTime)
            Time.timeScale = 0f;
    }
}
