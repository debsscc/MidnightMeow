///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gerencia a munição do jogador, incluindo coleta e uso.
// ---------------------------------------------------------------- */
using UnityEngine;

public class PlayerAmmo : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    private PlayerStats _runtimeStats;
    private int _currentAmmo;

    private PlayerStats ActiveStats => _runtimeStats != null ? _runtimeStats : stats;

    public int CurrentAmmo => _currentAmmo;

    private void Start()
    {
        if (ActiveStats != null)
            _currentAmmo = ActiveStats.maxAmmo;
    }

    private void OnEnable()
    {
        GameEvents.OnAmmoCollected += HandleAmmoCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnAmmoCollected -= HandleAmmoCollected;
    }

    private void HandleAmmoCollected()
    {
        PlayerStats activeStats = ActiveStats;
        if (activeStats == null)
            return;

        if (_currentAmmo < activeStats.maxAmmo && !activeStats.infinityAmmo)
            _currentAmmo++;
    }

    public bool HasAmmo()
    {
        return _currentAmmo > 0;
    }

    public void UseAmmo(int amount = 1)
    {
        PlayerStats activeStats = ActiveStats;
        if (activeStats != null && !activeStats.infinityAmmo)
            _currentAmmo = Mathf.Max(0, _currentAmmo - amount);
    }

    /// <summary>
    /// Alinha a munição local com o valor autoritativo do servidor (ex.: após disparo em rede).
    /// </summary>
    public void ApplySyncedAmmo(int value)
    {
        _currentAmmo = Mathf.Max(0, value);
    }

    public void ApplyRuntimeStats(PlayerStats runtimeStats)
    {
        _runtimeStats = runtimeStats;
        if (runtimeStats != null)
            _currentAmmo = runtimeStats.maxAmmo;
    }
}
