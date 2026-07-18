using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marca inimigos boss para UI (barra de vida sempre visível, escala maior).
/// </summary>
[DisallowMultipleComponent]
public class BossEnemyMarker : MonoBehaviour
{
    private static readonly List<BossEnemyMarker> ActiveMarkers = new List<BossEnemyMarker>(4);

    /// <summary>Disparado quando um boss ativo entra em cena (spawn / enable).</summary>
    public static event Action<BossEnemyMarker> OnBossAvailable;

    [SerializeField] private float healthBarWidthMultiplier = 1.6f;
    [SerializeField] private float healthBarHeightMultiplier = 1.35f;
    [SerializeField] private string displayName = "Rei Rato";

    public float HealthBarWidthMultiplier => healthBarWidthMultiplier;
    public float HealthBarHeightMultiplier => healthBarHeightMultiplier;
    public string DisplayName => displayName;

    public static IReadOnlyList<BossEnemyMarker> ActiveBosses => ActiveMarkers;

    private void OnEnable()
    {
        if (!ActiveMarkers.Contains(this))
            ActiveMarkers.Add(this);
        OnBossAvailable?.Invoke(this);
    }

    private void OnDisable()
    {
        ActiveMarkers.Remove(this);
    }
}
