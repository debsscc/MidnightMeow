using UnityEngine;
using UnityEngine.UI;

public class ScienceIndicator : MonoBehaviour
{
    [SerializeField] private Text text;

    private int currentScience;

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
        UpdateUI();
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
