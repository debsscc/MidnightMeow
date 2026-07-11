using UnityEngine;

/// <summary>
/// Marca inimigos boss para UI (barra de vida sempre visível, escala maior).
/// </summary>
[DisallowMultipleComponent]
public class BossEnemyMarker : MonoBehaviour
{
    [SerializeField] private float healthBarWidthMultiplier = 1.6f;
    [SerializeField] private float healthBarHeightMultiplier = 1.35f;
    [SerializeField] private string displayName = "Rei Rato";

    public float HealthBarWidthMultiplier => healthBarWidthMultiplier;
    public float HealthBarHeightMultiplier => healthBarHeightMultiplier;
    public string DisplayName => displayName;
}
