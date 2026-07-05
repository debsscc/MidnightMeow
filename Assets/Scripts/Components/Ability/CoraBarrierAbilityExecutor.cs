using UnityEngine;

/// <summary>
/// Habilidade Q da Cora — Barreira física na posição do mouse (com alcance máximo).
/// </summary>
[DisallowMultipleComponent]
public class CoraBarrierAbilityExecutor : MonoBehaviour, IAbilityExecutor
{
    [SerializeField] private GameObject barrierPrefab;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    public CharacterAbilityType AbilityType => CharacterAbilityType.CoraBarrier;

    private PlayerAbilityHandler _abilityHandler;

    private void Awake()
    {
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
    }

    public float Execute(AbilityTierData tierData, AbilityExecutionContext context)
    {
        if (barrierPrefab == null) return 0f;

        Quaternion rotation = AbilityPlacementUtility.RotationFromDirection(context.AimDirection);

        var spawner = context.User.GetComponent<NetworkAbilityObjectSpawner>();
        if (spawner != null)
        {
            spawner.SpawnBarrier(barrierPrefab, context.PlacementPosition, context.AimDirection, tierData);
            return 0.2f;
        }

        var instance = Instantiate(barrierPrefab, context.PlacementPosition, rotation);
        if (instance.TryGetComponent<CoraBarrier>(out var barrier))
            barrier.Initialize(tierData, context.OwnerClientId);

        return 0.2f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        var definition = _abilityHandler != null ? _abilityHandler.AbilitySet?.ability1 : null;
        if (definition == null || definition.abilityType != CharacterAbilityType.CoraBarrier) return;

        Vector2 forward = Vector2.up;
        if (TryGetComponent<PlayerAim>(out var aim) && aim.TryGetAimDirection(out Vector2 dir, out _))
            forward = dir;

        var host = GetComponent<AbilityDebugVisualHost>();
        if (host != null)
            host.DrawPreviewGizmo(CharacterAbilityType.CoraBarrier, transform.position, forward, definition.tier1);
        else
        {
            CoraBarrier.GetBarrierDimensions(definition.tier1, out float length, out float thickness);
            AbilityDebugGizmoUtility.DrawCenteredOrientedRect(
                transform.position,
                forward,
                thickness,
                length * 0.5f,
                new Color(0.2f, 0.95f, 0.45f, 0.25f),
                new Color(0.5f, 1f, 0.65f, 0.9f));
        }
    }
}
