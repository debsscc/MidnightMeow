using UnityEngine;

/// <summary>
/// Move um visual do ponto de disparo até a zona de telegraph; não aplica dano no trajeto.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TelegraphZoneTraveler : MonoBehaviour
{
    private Vector2 _target;
    private float _speed;
    private bool _arrived;

    public void Launch(Vector2 target, float speed)
    {
        _target = target;
        _speed = Mathf.Max(0.5f, speed);
        _arrived = false;

        DisableCombatOnPath();

        Vector2 dir = _target - (Vector2)transform.position;
        ProjectileAimUtility.ApplyRotation(transform, dir, ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees);
    }

    public bool HasArrived => _arrived;

    private void Update()
    {
        if (_arrived) return;

        var pos = (Vector2)transform.position;
        Vector2 dir = _target - pos;
        if (dir.sqrMagnitude > 0.0001f)
            ProjectileAimUtility.ApplyRotation(transform, dir, ProjectileAimUtility.EnemyRatProjectileForwardOffsetDegrees);

        var next = Vector2.MoveTowards(pos, _target, _speed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);

        if (Vector2.Distance(next, _target) <= 0.08f)
            _arrived = true;
    }

    private void DisableCombatOnPath()
    {
        if (TryGetComponent<EnemyProjectile>(out var projectile))
            projectile.enabled = false;

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }
}
