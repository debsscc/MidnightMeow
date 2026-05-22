using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escuta GameEvents.OnDamageShown e instancia números flutuantes de dano.
/// Coloque na cena (ex.: MultiplayerManagers) ou Bootstrap.
/// </summary>
public class DamageIndicatorPresenter : MonoBehaviour
{
    [SerializeField] private float floatDistance = 0.8f;
    [SerializeField] private float lifetime = 0.75f;
    [SerializeField] private Color damageColor = new Color(1f, 0.35f, 0.25f, 1f);
    [SerializeField] private int fontSize = 4;

    private void OnEnable()
    {
        GameEvents.OnDamageShown += HandleDamageShown;
    }

    private void OnDisable()
    {
        GameEvents.OnDamageShown -= HandleDamageShown;
    }

    private void HandleDamageShown(float amount, Vector3 worldPosition)
    {
        SpawnIndicator(amount, worldPosition);
    }

    private void SpawnIndicator(float amount, Vector3 worldPosition)
    {
        var go = new GameObject("DamageIndicator");
        go.transform.position = worldPosition;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 40f;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1.2f, 0.5f);

        var textGo = new GameObject("Value");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = Mathf.RoundToInt(amount).ToString();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = damageColor;
        tmp.fontStyle = FontStyles.Bold;

        var floater = go.AddComponent<DamageIndicatorFloater>();
        floater.Initialize(floatDistance, lifetime);
    }
}

/// <summary>
/// Anima o número para cima e destrói o objeto.
/// </summary>
public class DamageIndicatorFloater : MonoBehaviour
{
    private float _floatDistance;
    private float _lifetime;
    private float _timer;
    private Vector3 _start;

    public void Initialize(float floatDistance, float lifetime)
    {
        _floatDistance = floatDistance;
        _lifetime = Mathf.Max(0.1f, lifetime);
        _start = transform.position;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / _lifetime);
        transform.position = _start + Vector3.up * (_floatDistance * t);

        if (_timer >= _lifetime)
            Destroy(gameObject);
    }
}
