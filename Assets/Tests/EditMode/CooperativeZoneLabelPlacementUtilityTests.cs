using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CooperativeZoneLabelPlacementUtilityTests
{
    [Test]
    public void ResolvePosition_WithoutZones_UsesFallback()
    {
        Vector3 result = CooperativeZoneLabelPlacementUtility.ResolvePosition(
            null,
            zoneVisualRadius: 1.5f,
            fallbackAnchor: Vector2.zero,
            fallbackOffset: new Vector3(0f, 1.85f, 0f));

        Assert.AreEqual(new Vector3(0f, 1.85f, 0f), result);
    }

    [Test]
    public void ResolvePosition_WithZoneBelowAnchor_PlacesAboveCircle()
    {
        var zones = new List<Vector2> { new Vector2(0f, 2f) };
        Vector3 result = CooperativeZoneLabelPlacementUtility.ResolvePosition(
            zones,
            zoneVisualRadius: 1f,
            fallbackAnchor: Vector2.zero,
            fallbackOffset: new Vector3(0f, 1.85f, 0f),
            entityAnchorForSideChoice: Vector2.zero);

        float minY = 2f + 1f + GameplayUiFonts.WorldInteractionCanvasSize.y * 0.5f;
        Assert.Greater(result.y, minY);
        Assert.AreEqual(0f, result.x, 0.001f);
    }

    [Test]
    public void ResolvePosition_WithZoneAboveEntity_PlacesAboveCluster()
    {
        var zones = new List<Vector2> { new Vector2(1f, 3f), new Vector2(-1f, 3f) };
        Vector3 result = CooperativeZoneLabelPlacementUtility.ResolvePosition(
            zones,
            zoneVisualRadius: 1.2f,
            fallbackAnchor: Vector2.zero,
            fallbackOffset: new Vector3(0f, 1.85f, 0f),
            entityAnchorForSideChoice: Vector2.zero);

        Assert.AreEqual(0f, result.x, 0.001f);
        Assert.Greater(result.y, 3f + 1.2f);
    }

    [Test]
    public void CollectSealZones_RespectsZoneCount()
    {
        var buffer = new List<Vector2>();
        var session = new RatHoleSealSession
        {
            ZoneA = Vector2.right,
            ZoneB = Vector2.up,
            ZoneCount = 2
        };

        CooperativeZoneLabelPlacementUtility.CollectSealZones(session, buffer);
        Assert.AreEqual(2, buffer.Count);
        Assert.AreEqual(Vector2.right, buffer[0]);
        Assert.AreEqual(Vector2.up, buffer[1]);
    }
}
