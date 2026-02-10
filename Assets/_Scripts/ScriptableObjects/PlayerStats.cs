///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Define as estat�sticas do jogador, como velocidade de movimento e capacidade de muni��o.
// ---------------------------------------------------------------- */

using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Combat")]
    public int maxAmmo = 5;
    public bool infinityAmmo = false;
    public float firePointRadius = 0.8f;

    [Header("Adrenaline")]
    public float maxAdrenaline = 100f;
    public float adrenalineDecayRate = 5f;
    public float adrenalineGainPerCiencia = 10f;
    public float adrenalineThresholdToActivate = 50f;
    public float adrenalineLowThreshold = 30f;

    [Header("Frenzy Mode")]
    public float frenzySpeedMultiplier = 1.5f;
    public int frenzyBonusBounces = 2;
    public float frenzyDuration = 10f;
}
