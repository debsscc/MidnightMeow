///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gerencia a munição do jogador, incluindo coleta e uso.
// ---------------------------------------------------------------- */
using UnityEngine;

public class PlayerAmmo : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    private int _currentAmmo;

    // Propriedade pública para outros scripts (como UI) lerem
    public int CurrentAmmo => _currentAmmo; // O => é uma expressão de corpo para propriedades somente leitura

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
        if (_currentAmmo < stats.maxAmmo)
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
        _currentAmmo = Mathf.Max(0, _currentAmmo - amount);
        // Disparar evento OnAmmoChanged(_currentAmmo) para a UI
        Debug.Log($"Munição Usada! Restante: {_currentAmmo}");
    }
}