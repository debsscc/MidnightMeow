/* ----------------------------------------------------------------
AUTOR: Débora Carvalho
DATA: 2026-07-12
DESCRIÇÃO: Juice de câmera do jogador local — shake/zoom punch em eventos
importantes + lean/breathing contínuos. Tiro normal NÃO treme a câmera.
---------------------------------------------------------------- */

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class PlayerCameraJuice : MonoBehaviour
{
    [SerializeField] private PlayerAbilityHandler abilityHandler;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private bool shakeOnDash = true;
    [SerializeField] private bool shakeOnAbilities = true;
    [SerializeField] private bool shakeOnEnemyKill = true;
    [Tooltip("Lean + breathing (camera bounce). Desmarque para desligar o feed de bounce deste jogador (acessibilidade).")]
    [FormerlySerializedAs("locomotionFeel")]
    [SerializeField] private bool enableCameraBounce = true;

    private NetworkObject _networkObject;

    private void Awake()
    {
        if (abilityHandler == null)
            abilityHandler = GetComponent<PlayerAbilityHandler>();
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
        if (body == null)
            body = GetComponent<Rigidbody2D>();
        _networkObject = GetComponent<NetworkObject>();
    }

    private void OnEnable()
    {
        if (abilityHandler != null)
            abilityHandler.OnAbilityActivated += HandleAbilityActivated;

        GameEvents.OnEnemyKilledByPlayer += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        if (abilityHandler != null)
            abilityHandler.OnAbilityActivated -= HandleAbilityActivated;

        GameEvents.OnEnemyKilledByPlayer -= HandleEnemyKilled;

        if (enableCameraBounce && IsLocalAuthority())
            PlayerCameraFeedback.SetLocomotionFeel(Vector2.zero, 0f);
    }

    private void LateUpdate()
    {
        if (!enableCameraBounce || !IsLocalAuthority())
            return;

        Vector2 moveInput = movement != null ? movement.MoveDirection : Vector2.zero;
        float speed = body != null ? body.linearVelocity.magnitude : 0f;
        PlayerCameraFeedback.SetLocomotionFeel(moveInput, speed);
    }

    private void HandleAbilityActivated(CharacterAbilityType abilityType)
    {
        if (!IsLocalAuthority())
            return;

        switch (abilityType)
        {
            case CharacterAbilityType.Dash:
                if (shakeOnDash)
                    PlayerCameraFeedback.ShakeOnDash();
                break;

            case CharacterAbilityType.CoraBarrier:
            case CharacterAbilityType.CoraPool:
            case CharacterAbilityType.NixPush:
            case CharacterAbilityType.NixCharge:
                if (shakeOnAbilities)
                    PlayerCameraFeedback.ShakeOnAbility();
                break;

            default:
                break;
        }
    }

    private void HandleEnemyKilled(ulong killerClientId)
    {
        if (!shakeOnEnemyKill || !IsLocalAuthority() || !IsLocalKiller(killerClientId))
            return;

        PlayerCameraFeedback.ShakeOnEnemyKill();
    }

    private bool IsLocalAuthority()
    {
        if (_networkObject != null && _networkObject.IsSpawned)
            return _networkObject.IsOwner;
        return true;
    }

    private bool IsLocalKiller(ulong killerClientId)
    {
        if (_networkObject != null && _networkObject.IsSpawned)
            return killerClientId == _networkObject.OwnerClientId;

        return true;
    }
}
