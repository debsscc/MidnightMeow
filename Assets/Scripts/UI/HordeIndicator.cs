using UnityEngine;
using UnityEngine.UI;

public class HordeIndicator : MonoBehaviour
{
    [SerializeField] private Text text;

    private string currentHorde;

    private void OnEnable()
    {
        GameEvents.OnWaveStatusChanged += UpdateHorde;
    }

    private void OnDisable()
    {
        GameEvents.OnWaveStatusChanged -= UpdateHorde;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateHorde(int currentWave, int totalWaves, int enemiesRemaining, int totalKilled)
    {
        currentHorde = $"Wave:  {currentWave}/{totalWaves} - Restante: {enemiesRemaining} - Kills: {totalKilled}";
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (text != null)
        {
            text.text = currentHorde;
        }
    }
}
