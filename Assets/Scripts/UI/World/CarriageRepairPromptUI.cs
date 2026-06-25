using TMPro;
using UnityEngine;

/// <summary>
/// Popup "consertar" acima da carruagem quebrada.
/// </summary>
[DisallowMultipleComponent]
public class CarriageRepairPromptUI : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, 0f);

    private PlayerInputHandler _input;
    private Canvas _canvas;
    private TextMeshProUGUI _label;

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        BuildUI();
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (_input != null)
            _input.OnInteractHoldChanged += HandleInteract;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnInteractHoldChanged -= HandleInteract;
    }

    private void LateUpdate()
    {
        NetworkCarriage carriage = NetworkCarriage.Instance;
        bool show = carriage != null && carriage.IsBroken && !carriage.RepairActive;
        SetVisible(show);

        if (!show || carriage == null)
            return;

        _canvas.transform.position = carriage.transform.position + offset;
    }

    private void HandleInteract(bool pressed)
    {
        if (!pressed)
            return;

        NetworkCarriage carriage = NetworkCarriage.Instance;
        if (carriage == null || !carriage.IsBroken || carriage.RepairActive)
            return;

        float dist = Vector2.Distance(transform.position, carriage.transform.position);
        if (dist > 3f)
            return;

        carriage.RequestStartRepairRpc();
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(visible);
    }

    private void BuildUI()
    {
        var root = new GameObject("CarriageRepairPrompt");
        root.transform.SetParent(transform, false);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2.8f, 0.5f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(2.8f, 0.5f);
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.text = "Aperte E para consertar";
        _label.fontSize = 2.2f;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = new Color(1f, 0.9f, 0.55f, 1f);
    }
}
