using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buraco de spawn de ratos na cena. Não colide com o jogador; pode ser selado para parar spawns.
/// </summary>
[DisallowMultipleComponent]
public class RatHoleSpawnPoint : MonoBehaviour
{
    private static readonly List<RatHoleSpawnPoint> Registry = new List<RatHoleSpawnPoint>(8);

    [SerializeField] private ushort holeId = 1;
    [SerializeField] private SpriteRenderer holeSprite;
    [SerializeField] private float spawnScatterRadius = 0.6f;

    public ushort HoleId => holeId;
    public Vector2 AnchorPosition => transform.position;
    public float SpawnScatterRadius => spawnScatterRadius;
    public bool CanSpawn => !IsSealed;

    public bool IsSealed =>
        NetworkRatHoleSealManager.Instance != null &&
        NetworkRatHoleSealManager.Instance.IsHoleSealed(holeId);

    public static IReadOnlyList<RatHoleSpawnPoint> All => Registry;

    private void OnEnable()
    {
        if (!Registry.Contains(this))
            Registry.Add(this);

        if (GetComponent<RatHoleSealStatusUI>() == null)
            gameObject.AddComponent<RatHoleSealStatusUI>();
    }

    private void OnDisable()
    {
        Registry.Remove(this);
    }

    private void Reset()
    {
        EnsureTriggerCollider();

        if (holeSprite == null)
            holeSprite = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Garante collider de proximidade. Seguro durante Undo.AddComponent no Editor.
    /// </summary>
    public void EnsureTriggerCollider(float radius = 2.4f)
    {
        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        if (trigger == null)
            trigger = gameObject.AddComponent<CircleCollider2D>();

        if (trigger == null)
            return;

        trigger.isTrigger = true;
        trigger.radius = radius;
    }

    public Vector3 GetSpawnPosition()
    {
        Vector2 offset = Random.insideUnitCircle * spawnScatterRadius;
        return AnchorPosition + offset;
    }

    public static RatHoleSpawnPoint FindById(ushort id)
    {
        for (int i = 0; i < Registry.Count; i++)
        {
            if (Registry[i] != null && Registry[i].holeId == id)
                return Registry[i];
        }

        return null;
    }
}
