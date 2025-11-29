///* ----------------------------------------------------------------
// CRIADO EM: 13-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Controla o disparo de projéteis pelo jogador quando o input de 'Fire' é acionado.
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

    public event Action OnShoot;
    public event Action OnOutOfAmmo;
    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
        _ammo = GetComponent<PlayerAmmo>();
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
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            OnShoot?.Invoke();
        }
        else
        {
            OnOutOfAmmo?.Invoke();  // Emitir som de clique vazio ou similar
            Debug.Log("Sem Munição!");
        }
    }
}