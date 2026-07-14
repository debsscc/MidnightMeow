using UnityEngine;

public static class TelegraphPoseUtility
{
    public static bool TryComputeStrikePose(
        TelegraphStrikeDefinition strike,
        Vector2 attackOrigin,
        Transform target,
        out Vector2 worldPosition,
        out float rotationDegrees)
    {
        Vector2 basePos = attackOrigin;
        if (strike.anchorToTargetOnStart && target != null)
            basePos = target.position;

        float aimAngle = 0f;
        if (strike.aimAtTarget && target != null)
        {
            Vector2 dir = ((Vector2)target.position - attackOrigin).normalized;
            if (dir.sqrMagnitude > 0.0001f)
                aimAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        }

        rotationDegrees = aimAngle + strike.rotationOffsetDegrees;

        Vector2 offset = strike.localOffset;

        // ConeFrustum sem offset explícito: centro na metade do comprimento, saindo do atacante.
        if (strike.shape == TelegraphShapeType.ConeFrustum
            && offset.sqrMagnitude < 0.0001f
            && !strike.anchorToTargetOnStart)
        {
            TelegraphConeFrustumUtility.ResolveRadii(strike, out _, out _, out float length);
            offset = new Vector2(0f, length * 0.5f);
        }

        if (offset.sqrMagnitude > 0.0001f)
        {
            var rot = Quaternion.Euler(0f, 0f, rotationDegrees);
            offset = rot * (Vector3)offset;
        }

        worldPosition = basePos + offset;
        return true;
    }
}
