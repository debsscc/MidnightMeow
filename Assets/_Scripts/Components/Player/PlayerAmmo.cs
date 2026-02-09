///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Gerencia a muni��o do jogador, incluindo coleta e uso.
// ---------------------------------------------------------------- */
using UnityEngine;

public class PlayerAmmo : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    private int _currentAmmo;

    // Propriedade p�blica para outros scripts (como UI) lerem
    public int CurrentAmmo => _currentAmmo; // O => � uma express�o de corpo para propriedades somente leitura

    private void Start()
    {
        _currentAmmo = stats.maxAmmo;
        // Idealmente, disparar um evento OnAmmoChanged aqui para a UI
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
        if (_currentAmmo < stats.maxAmmo && !stats.infinityAmmo)
        {
            _currentAmmo++;
            // Disparar evento OnAmmoChanged(_currentAmmo) para a UI
        }
    }

    public bool HasAmmo()
    {
        return _currentAmmo > 0;
    }

    public void UseAmmo(int amount = 1)
    {
        if (!stats.infinityAmmo){
            _currentAmmo = Mathf.Max(0, _currentAmmo - amount);
        }
        // Disparar evento OnAmmoChanged(_currentAmmo) para a UI
        Debug.Log($"Muni��o Usada! Restante: {_currentAmmo}");
    }
}