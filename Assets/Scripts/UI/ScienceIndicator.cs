using UnityEngine;
using UnityEngine.UI;

public class ScienceIndicator : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField] private RectTransform scoreAnchor;
    [SerializeField] private Vector2 topRightPadding = new Vector2(-12f, -12f);
    [SerializeField] private Vector2 scoreSize = new Vector2(96f, 40f);

    private int currentScience;

    private void Awake()
    {
        EnsureScoreLayout();
    }

    private void OnEnable()
    {
        GameEvents.OnCienciaCollected += UpdateScience;
    }

    private void OnDisable()
    {
        GameEvents.OnCienciaCollected -= UpdateScience;
    }

    private void Start()
    {
        EnsureScoreLayout();
        UpdateUI();
    }

    private void EnsureScoreLayout()
    {
        RectTransform self = transform as RectTransform;
        if (self == null)
            return;

        RectTransform anchor = scoreAnchor;
        if (anchor == null)
            anchor = FindScoreAnchor(self);

        if (anchor != null && self.parent != anchor)
            self.SetParent(anchor, false);

        self.anchorMin = new Vector2(1f, 1f);
        self.anchorMax = new Vector2(1f, 1f);
        self.pivot = new Vector2(1f, 1f);
        self.anchoredPosition = topRightPadding;
        self.sizeDelta = scoreSize;

        if (text != null)
            text.alignment = TextAnchor.UpperRight;
    }

    private static RectTransform FindScoreAnchor(RectTransform self)
    {
        Transform current = self.parent;
        while (current != null)
        {
            if (current.name.Contains("Indicator"))
                return current as RectTransform;
            current = current.parent;
        }

        Canvas canvas = self.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null && rects[i].name == "Indicator")
                return rects[i];
        }

        return null;
    }

    private void UpdateScience(int amount)
    {
        currentScience += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (text != null)
        {
            text.text = currentScience.ToString();
        }
    }
}
