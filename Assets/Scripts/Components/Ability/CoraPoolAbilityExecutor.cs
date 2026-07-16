using UnityEngine;

/// <summary>
/// Habilidade R da Cora — Poça circular de dano na posição do mouse.
/// </summary>
[DisallowMultipleComponent]
public class CoraPoolAbilityExecutor : MonoBehaviour, IAbilityExecutor
{
    [SerializeField] private GameObject poolPrefab;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    public CharacterAbilityType AbilityType => CharacterAbilityType.CoraPool;

    private PlayerAbilityHandler _abilityHandler;

    private void Awake()
    {
        _abilityHandler = GetComponent<PlayerAbilityHandler>();
    }

    public float Execute(AbilityTierData tierData, AbilityExecutionContext context)
    {
        if (poolPrefab == null) return 0f;

        var spawner = context.User.GetComponent<NetworkAbilityObjectSpawner>();
        if (spawner != null)
        {
            spawner.SpawnPool(poolPrefab, context.PlacementPosition, tierData);
            return 0.2f;
        }

        var instance = Instantiate(poolPrefab, context.PlacementPosition, Quaternion.identity);
        if (instance.TryGetComponent<CoraDamagePool>(out var pool))
            pool.Initialize(tierData, context.OwnerClientId);

        return 0.2f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        var definition = _abilityHandler != null ? _abilityHandler.AbilitySet?.ability2 : null;
        if (definition == null || definition.abilityType != CharacterAbilityType.CoraPool) return;

        Vector2 forward = Vector2.up;
        if (TryGetComponent<PlayerAim>(out var aim) && aim.TryGetAimDirection(out Vector2 dir, out _))
            forward = dir;

        var host = GetComponent<AbilityDebugVisualHost>();
        if (host != null)
            host.DrawPreviewGizmo(CharacterAbilityType.CoraPool, transform.position, forward, definition.tier1);
        else
            AbilityDebugGizmoUtility.DrawCircle(transform.position, definition.tier1.ResolvePuddleRadius(),
                new Color(0.75f, 0.2f, 0.95f, 0.25f), new Color(0.9f, 0.5f, 1f, 0.9f));
    }
}
