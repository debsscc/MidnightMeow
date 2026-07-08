/// <summary>
/// Parâmetros data-driven da carruagem da Fase 2.
/// </summary>

using UnityEngine;

[CreateAssetMenu(fileName = "CarriageConfig", menuName = "MidnightMeow/Gameplay/Carriage Config")]
public class CarriageConfig : ScriptableObject
{
    [Header("Vitalidade")]
    public float maxHealth = 120f;

    [Tooltip("Fração da vida máxima restaurada após conserto (0–1).")]
    [Range(0.1f, 1f)]
    public float repairRestoreHealthFraction = 0.5f;

    [Header("Visual")]
    [Tooltip("Multiplicador opcional sobre o tamanho alvo do placeholder (~2,4×1,6 uu).")]
    public float visualScale = 1f;

    [Header("Trajeto (Fase 2)")]
    public float pathStartX = -42f;
    public float pathEndX = 18f;
    public bool useCustomPathY;
    public float pathY;

    [Header("Movimento")]
    [Tooltip("Unidades por segundo ao longo do trajeto.")]
    public float moveSpeed = 1.8f;

    [Header("Chegada")]
    public float arrivalZoneRadius = 2f;

    [Header("Conserto cooperativo")]
    public float repairPromptRadius = 3.3f;
    public float repairZoneRadius = 1.2f;
    public float repairMinDistance = 1.4f;
    public float repairMaxDistance = 2.8f;
    public float repairMinZoneSeparation = 1.6f;
    public float repairDuration = 8f;
    [Range(1f, 4f)]
    public float repairDualPlayerSpeedMultiplier = 2f;
    public float repairAbandonTimeout = 2.5f;

    [Header("Visual das áreas de conserto")]
    public float repairZoneVisualScaleMultiplier = 1.05f;
    [Range(0.02f, 0.15f)]
    public float repairZoneOutlineThickness = 0.055f;
    public bool repairZoneShowInteriorFill;
    public int repairZoneSortingOrder = 250;
    public Color repairZoneBackgroundColor = new Color(0.95f, 0.7f, 0.25f, 0.22f);
    public Color repairZoneFillColor = new Color(1f, 0.85f, 0.35f, 0.5f);
    public Color repairZoneOutlineColor = new Color(1f, 0.95f, 0.75f, 0.9f);

    [Header("UI")]
    [Tooltip("Prefab world-space do label acima da carruagem (reutiliza DownedRevivePromptUI).")]
    public GameObject repairPromptPrefab;

    public float repairLabelVisibilityRadiusMultiplier = 2f;
    public string approachText = "Aproxime-se para consertar";
    public string pressEText = "Aperte E para consertar";
    public string repairProgressTextFormat = "{0}%";

    public float GetRepairZoneVisualDiameter() =>
        Mathf.Max(0.5f, repairZoneRadius * 2f * Mathf.Max(0.85f, repairZoneVisualScaleMultiplier));

    public float GetRepairLabelVisibilityRadius() =>
        Mathf.Max(repairPromptRadius, repairPromptRadius * Mathf.Max(1f, repairLabelVisibilityRadiusMultiplier));

    public string GetApproachText() => string.IsNullOrWhiteSpace(approachText)
        ? "Aproxime-se para consertar"
        : approachText;

    public string GetPressEText() => string.IsNullOrWhiteSpace(pressEText)
        ? "Aperte E para consertar"
        : pressEText;

    public string FormatRepairProgressText(int percent) =>
        string.IsNullOrWhiteSpace(repairProgressTextFormat) ? $"{percent}%" : string.Format(repairProgressTextFormat, percent);

    private void OnValidate()
    {
        if (repairPromptRadius <= 0f) repairPromptRadius = repairZoneRadius * 3f;
        if (repairAbandonTimeout <= 0f) repairAbandonTimeout = 2.5f;
        if (repairLabelVisibilityRadiusMultiplier < 1f) repairLabelVisibilityRadiusMultiplier = 2f;
    }
}
