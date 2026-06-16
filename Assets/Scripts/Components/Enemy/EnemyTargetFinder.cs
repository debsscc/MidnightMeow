///* ----------------------------------------------------------------
// ATUALIZADO EM: 22-05-2026
// DESCRIÇÃO: Encontra alvo por prioridade, distância e raio de detecção configurável.
// Ignora jogadores inconscientes. Reavalia em intervalo fixo (não todo frame).
// ---------------------------------------------------------------- */

using UnityEngine;

public class EnemyTargetFinder : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    public EnemyStats Stats => stats;

    private Transform _currentTarget;
    private float _nextScanTime;

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

    public void FindTarget()
    {
        if (stats == null)
        {
            _currentTarget = null;
            return;
        }

        string targetTag = stats.targetPriority == TargetPriority.Player ? "Player" : "Structure";
        GameObject[] candidates = GameObject.FindGameObjectsWithTag(targetTag);

        Transform nearest = null;
        float minDist = float.MaxValue;
        float maxRange = stats.targetDetectionRange;

        foreach (var candidate in candidates)
        {
            if (!IsValidTarget(candidate)) continue;

            float dist = Vector2.Distance(transform.position, candidate.transform.position);
            if (dist > maxRange) continue;

            if (dist < minDist)
            {
                minDist = dist;
                nearest = candidate.transform;
            }
        }

        if (nearest == null && stats.targetPriority == TargetPriority.Player)
        {
            candidates = GameObject.FindGameObjectsWithTag("Structure");
            foreach (var candidate in candidates)
            {
                if (!candidate.activeInHierarchy) continue;
                float dist = Vector2.Distance(transform.position, candidate.transform.position);
                if (dist > maxRange) continue;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = candidate.transform;
                }
            }
        }

        _currentTarget = nearest;
    }

    private static bool IsValidTarget(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return false;

        var netHealth = go.GetComponent<NetworkPlayerHealth>();
        if (netHealth != null && netHealth.IsSpawned)
            return netHealth.CanBeTargeted;

        var health = go.GetComponent<HealthComponent>();
        if (health != null)
            return health.IsAlive;

        return true;
    }
}
