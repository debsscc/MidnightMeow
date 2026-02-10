///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRI��O: Controla o disparo de proj�teis pelo jogador quando o input de 'Fire' � acionado.
// ---------------------------------------------------------------- */
using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler), typeof(PlayerAmmo))]
public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private PlayerInputHandler _input;
    private PlayerAmmo _ammo;
    private PlayerAdrenaline _adrenaline;

    public event Action OnShoot;
    public event Action OnOutOfAmmo;
    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _ammo = GetComponent<PlayerAmmo>();
        _adrenaline = GetComponent<PlayerAdrenaline>();
    }

    // Assina e desassina eventos de input
    private void OnEnable()
    {
        _input.OnFireInput += HandleFireInput;
    }
    private void OnDisable()
    {
        _input.OnFireInput -= HandleFireInput;
    }
    // Lida com o input de disparo
    private void HandleFireInput()
    {
        if (_ammo.HasAmmo())
        {
            _ammo.UseAmmo(1);
            GameObject projectileInstance = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            if (_adrenaline != null && _adrenaline.IsFrenzyActive)
            {
                if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile))
                {
                    projectile.AddBonusBounces(_adrenaline.GetBonusBounces());
                }
            }

            OnShoot?.Invoke();
        }
        else
        {
            OnOutOfAmmo?.Invoke();  // Emitir som de clique vazio ou similar
            Debug.Log("Sem Muni��o!");
        }
    }
}