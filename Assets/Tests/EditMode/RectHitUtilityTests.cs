using NUnit.Framework;
using UnityEngine;

public class RectHitUtilityTests
{
    [Test]
    public void IsInsideOrientedRect_DetectsPointAhead()
    {
        Vector2 origin = Vector2.zero;
        Vector2 forward = Vector2.up;
        Vector2 point = new Vector2(0f, 1.5f);

        bool inside = RectHitUtility.IsInsideOrientedRect(origin, forward, depth: 3f, halfWidth: 1f, point);
        Assert.IsTrue(inside);
    }

    [Test]
    public void IsInsideOrientedRect_RejectsLateralPoint()
    {
        Vector2 origin = Vector2.zero;
        Vector2 forward = Vector2.up;
        Vector2 point = new Vector2(5f, 1f);

        bool inside = RectHitUtility.IsInsideOrientedRect(origin, forward, depth: 3f, halfWidth: 0.5f, point);
        Assert.IsFalse(inside);
    }
}
