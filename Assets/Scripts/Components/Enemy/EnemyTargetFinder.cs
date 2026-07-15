///* ----------------------------------------------------------------
// ATUALIZADO EM: 15-07-2026
// DESCRIÇÃO: Encontra alvo por AggroType (PlayersOnly / StructuresOnly / Dynamic).
// Dynamic: estrutura como base; swapToNearbyPlayer e swapOnDamage conforme EnemyStats.
// Ignora jogadores inconscientes. Reavalia em intervalo fixo (não todo frame).
// ---------------------------------------------------------------- */

using UnityEngine;

public class EnemyTargetFinder : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    public EnemyStats Stats => stats;

    private Transform _currentTarget;
    private float _nextScanTime;
    private Transform _damageForcedTarget;
    private bool _damageLockActive;

    public Transform CurrentTarget => _currentTarget;
    public bool HasTarget => _currentTarget != null;

    private void Start()
    {
        FindTarget();
    }

    private void Update()
    {
        if (stats == null) return;
        if (Time.time < _nextScanTime) return;
        _nextScanTime = Time.time + Mathf.Max(0.05f, stats.targetScanInterval);
        FindTarget();
    }

    /// <summary>
    /// Gatilho de dano (servidor): se Dynamic + swapOnDamage e o atacante é jogador válido, foca nele.
    /// </summary>
    public void NotifyDamagedBy(Transform attacker)
    {
        if (stats == null || attacker == null)
            return;

        if (stats.ResolveAggroType() != AggroType.Dynamic || !stats.swapOnDamage)
            return;

        if (!IsValidPlayerTarget(attacker.gameObject))
            return;

        _damageForcedTarget = attacker;
        _damageLockActive = true;
        _currentTarget = attacker;
        _nextScanTime = Time.time + Mathf.Max(0.05f, stats.targetScanInterval);
    }

    public void FindTarget()
    {
        if (stats == null)
        {
            _currentTarget = null;
            return;
        }

        float maxRange = stats.targetDetectionRange;
        AggroType aggro = stats.ResolveAggroType();

        if (_damageLockActive)
        {
            if (IsValidPlayerTarget(_damageForcedTarget != null ? _damageForcedTarget.gameObject : null)
                && IsInRange(_damageForcedTarget, maxRange))
            {
                _currentTarget = _damageForcedTarget;
                return;
            }

            ClearDamageLock();
        }

        switch (aggro)
        {
            case AggroType.PlayersOnly:
                _currentTarget = FindNearestPlayer(maxRange);
                break;

            case AggroType.StructuresOnly:
                _currentTarget = FindNearestStructure(maxRange);
                break;

            case AggroType.Dynamic:
                _currentTarget = ResolveDynamicTarget(maxRange);
                break;

            default:
                _currentTarget = FindNearestPlayer(maxRange);
                break;
        }
    }

    private Transform ResolveDynamicTarget(float maxRange)
    {
        Transform structure = FindNearestStructure(maxRange);
        Transform player = FindNearestPlayer(maxRange);

        if (structure == null)
            return player;

        if (player == null || !stats.swapToNearbyPlayer)
            return structure;

        float structureDist = Vector2.Distance(transform.position, structure.position);
        float playerDist = Vector2.Distance(transform.position, player.position);
        return playerDist < structureDist ? player : structure;
    }

    private Transform FindNearestPlayer(float maxRange)
    {
        GameObject[] candidates = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (!IsValidPlayerTarget(candidate)) continue;

            float dist = Vector2.Distance(transform.position, candidate.transform.position);
            if (dist > maxRange) continue;

            if (dist < minDist)
            {
                minDist = dist;
                nearest = candidate.transform;
            }
        }

        return nearest;
    }

    private Transform FindNearestStructure(float maxRange)
    {
        Transform nearest = null;
        float minDist = float.MaxValue;

        GameObject[] byTag = GameObject.FindGameObjectsWithTag("Structure");
        for (int i = 0; i < byTag.Length; i++)
        {
            GameObject candidate = byTag[i];
            if (!IsValidStructureTarget(candidate)) continue;

            float dist = Vector2.Distance(transform.position, candidate.transform.position);
            if (dist > maxRange) continue;

            if (dist < minDist)
            {
                minDist = dist;
                nearest = candidate.transform;
            }
        }

        if (nearest != null)
            return nearest;

        // Fallback: CarriageController singleton (caso tag ausente em runtime)
        CarriageController carriage = CarriageController.Instance;
        if (carriage != null && IsValidStructureTarget(carriage.gameObject)
            && IsInRange(carriage.transform, maxRange))
            return carriage.transform;

        return null;
    }

    private void ClearDamageLock()
    {
        _damageLockActive = false;
        _damageForcedTarget = null;
    }

    private bool IsInRange(Transform target, float maxRange)
    {
        if (target == null) return false;
        return Vector2.Distance(transform.position, target.position) <= maxRange;
    }

    private static bool IsValidPlayerTarget(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return false;

        var netHealth = go.GetComponentInParent<NetworkPlayerHealth>();
        if (netHealth != null && netHealth.IsSpawned)
            return netHealth.CanBeTargeted;

        var health = go.GetComponentInParent<HealthComponent>();
        if (health != null)
            return health.IsAlive;

        return go.CompareTag("Player");
    }

    private static bool IsValidStructureTarget(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return false;

        var carriageHealth = go.GetComponentInParent<NetworkCarriageHealth>();
        if (carriageHealth != null && carriageHealth.IsSpawned && carriageHealth.IsBroken)
            return false;

        var health = go.GetComponentInParent<HealthComponent>();
        if (health != null)
            return health.IsAlive;

        return true;
    }
}
