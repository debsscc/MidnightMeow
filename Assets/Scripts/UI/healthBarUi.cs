using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class healthBarUi : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    private Coroutine _refreshRoutine;

    private void OnEnable()
    {
        GameEvents.OnPlayerHealthChanged += UpdateHealthBar;
        NetworkPlayerHealth.OnNetworkHealthChanged += HandleNetworkHealthChanged;
        NetworkPlayerController.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        QueueRefresh();
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHealthChanged -= UpdateHealthBar;
        NetworkPlayerHealth.OnNetworkHealthChanged -= HandleNetworkHealthChanged;
        NetworkPlayerController.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;

        if (_refreshRoutine != null)
        {
            StopCoroutine(_refreshRoutine);
            _refreshRoutine = null;
        }
    }

    private void Start()
    {
        QueueRefresh();
    }

    private void HandleLocalPlayerSpawned(NetworkPlayerController _)
    {
        QueueRefresh();
    }

    private void HandleNetworkHealthChanged(ulong clientId, float current, float max)
    {
        if (NetworkManager.Singleton == null || clientId != NetworkManager.Singleton.LocalClientId)
            return;

        UpdateHealthBar(current, max);
    }

    private void QueueRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (_refreshRoutine != null)
            StopCoroutine(_refreshRoutine);

        _refreshRoutine = StartCoroutine(RefreshAfterLayoutRoutine());
    }

    private IEnumerator RefreshAfterLayoutRoutine()
    {
        yield return null;

        if (TryGetLocalHealth(out float current, out float max))
            UpdateHealthBar(current, max);
        else
            ApplyNormalizedHealth(1f);

        _refreshRoutine = null;
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f)
            return;

        ApplyNormalizedHealth(currentHealth / maxHealth);
    }

    private void ApplyNormalizedHealth(float normalized)
    {
        if (healthSlider == null)
            return;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;

        float clamped = Mathf.Clamp01(normalized);
        if (Mathf.Approximately(healthSlider.value, clamped))
            healthSlider.SetValueWithoutNotify(0f);

        healthSlider.value = clamped;
        Canvas.ForceUpdateCanvases();
    }

    private static bool TryGetLocalHealth(out float current, out float max)
    {
        current = 0f;
        max = 0f;

        NetworkPlayerHealth[] players = Object.FindObjectsByType<NetworkPlayerHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerHealth health = players[i];
            if (health == null || !health.IsSpawned || !health.IsOwner || !health.CanFight)
                continue;

            current = health.CurrentHealth;
            max = health.MaxHealth;
            return max > 0f;
        }

        HealthComponent legacy = Object.FindFirstObjectByType<HealthComponent>(FindObjectsInactive.Exclude);
        if (legacy != null && legacy.CompareTag("Player") && legacy.IsAlive)
        {
            current = legacy.CurrentHealth;
            max = legacy.MaxHealth;
            return max > 0f;
        }

        return false;
    }
}
