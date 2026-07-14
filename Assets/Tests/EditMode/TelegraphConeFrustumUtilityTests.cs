using NUnit.Framework;
using UnityEngine;

public class TelegraphConeFrustumUtilityTests
{
    [Test]
    public void ContainsPoint_AcceptsPointInsideTrapezoid()
    {
        Vector2 center = Vector2.zero;
        float rotation = 0f;
        float inner = 0.5f;
        float outer = 1.5f;
        float length = 4f;

        // Centro do trapézio
        Assert.IsTrue(TelegraphConeFrustumUtility.ContainsPoint(
            Vector2.zero, center, rotation, inner, outer, length));

        // Perto da base (Y local negativo): meia-largura ~ inner
        Assert.IsTrue(TelegraphConeFrustumUtility.ContainsPoint(
            new Vector2(0.4f, -1.8f), center, rotation, inner, outer, length));
        Assert.IsFalse(TelegraphConeFrustumUtility.ContainsPoint(
            new Vector2(0.8f, -1.8f), center, rotation, inner, outer, length));

        // Perto da ponta: meia-largura ~ outer
        Assert.IsTrue(TelegraphConeFrustumUtility.ContainsPoint(
            new Vector2(1.4f, 1.8f), center, rotation, inner, outer, length));
        Assert.IsFalse(TelegraphConeFrustumUtility.ContainsPoint(
            new Vector2(1.7f, 1.8f), center, rotation, inner, outer, length));
    }

    [Test]
    public void ResolveRadii_DerivesOuterFromOpeningAngleWhenOuterUnset()
    {
        var strike = new TelegraphStrikeDefinition
        {
            size = new Vector2(0.5f, 2f),
            coneInnerRadius = 0.5f,
            coneOuterRadius = 0f,
            coneOpeningAngleDegrees = 45f
        };

        TelegraphConeFrustumUtility.ResolveRadii(strike, out float inner, out float outer, out float length);
        Assert.AreEqual(0.5f, inner, 0.001f);
        Assert.AreEqual(2f, length, 0.001f);
        Assert.Greater(outer, inner);
        Assert.AreEqual(0.5f + 2f * Mathf.Tan(45f * Mathf.Deg2Rad), outer, 0.01f);
    }

    [Test]
    public void GetAabbSize_UsesMaxRadiusTimesTwo()
    {
        Vector2 aabb = TelegraphConeFrustumUtility.GetAabbSize(0.4f, 1.2f, 3f);
        Assert.AreEqual(2.4f, aabb.x, 0.001f);
        Assert.AreEqual(3f, aabb.y, 0.001f);
    }
}
