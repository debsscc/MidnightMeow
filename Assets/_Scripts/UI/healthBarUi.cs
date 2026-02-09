using UnityEngine;
using UnityEngine.UI;

public class healthBarUi : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    private void OnEnable()
    {
        GameEvents.OnPlayerHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHealthChanged -= UpdateHealthBar;
    }

    private void Start()
    {
        UpdateHealthBar(1.0f, 1.0f);
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        Debug.Log($"Health updated: {currentHealth}/{maxHealth}");
        if (healthSlider != null && maxHealth > 0)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }
}