using UnityEngine;
using UnityEngine.UI;

public class ScienceIndicator : MonoBehaviour
{
    [SerializeField] private Text text;

    private void OnEnable()
    {
        RoundMagiculaTracker tracker = RoundMagiculaTracker.Instance;
        if (tracker != null)
            tracker.OnRoundTotalChanged += HandleRoundTotalChanged;

        GameEvents.OnCienciaCollected += HandleCienciaCollected;
    }

    private void OnDisable()
    {
        RoundMagiculaTracker tracker = RoundMagiculaTracker.Instance;
        if (tracker != null)
            tracker.OnRoundTotalChanged -= HandleRoundTotalChanged;

        GameEvents.OnCienciaCollected -= HandleCienciaCollected;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void HandleCienciaCollected(int amount)
    {
        if (amount > 0)
            UpdateUI();
    }

    private void HandleRoundTotalChanged(int _) => UpdateUI();

    private void UpdateUI()
    {
        if (text == null)
            return;

        RoundMagiculaTracker tracker = RoundMagiculaTracker.Instance;
        int roundTotal = tracker != null ? tracker.RoundTotal : 0;
        text.text = roundTotal.ToString();
    }
}
