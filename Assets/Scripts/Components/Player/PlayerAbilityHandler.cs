///* ----------------------------------------------------------------
// CRIADO EM: 17-11-2025
// FEITO POR: Pedro Caurio
// DESCRIÇÃO: Gerencia a ativação de habilidades do jogador, incluindo entrada do jogador e cooldown.
// ---------------------------------------------------------------- */
using UnityEngine;
using System;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAbilityHandler : MonoBehaviour
{
    [SerializeField] private Ability currentAbility;

    [SerializeField] private Transform firePoint;

    private PlayerInputHandler _input;
    private float _cooldownTimer;

    // Getter público para que as habilidades possam encontrar o firePoint
    public Transform FirePoint => firePoint;
    public event Action<Ability> OnAbilityActivated;
    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
        }
    }

    private void OnEnable()
    {
        _input.OnAbilityInput += HandleAbilityInput;
    }

    private void OnDisable()
    {
        _input.OnAbilityInput -= HandleAbilityInput;
    }

    private void HandleAbilityInput()
    {
        if (currentAbility == null || _cooldownTimer > 0)
        {
            return;
        }
        currentAbility.Activate(this.gameObject);

        // Inicia o cooldown
        _cooldownTimer = currentAbility.cooldown;

        OnAbilityActivated?.Invoke(currentAbility);
    }

    public void EquipAbility(Ability newAbility)
    {
        currentAbility = newAbility;
        _cooldownTimer = 0;
    }


}