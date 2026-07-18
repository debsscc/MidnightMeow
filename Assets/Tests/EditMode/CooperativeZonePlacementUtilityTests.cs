using NUnit.Framework;
using UnityEngine;

public class CooperativeZonePlacementUtilityTests
{
    [Test]
    public void TryPlaceZones_SingleZone_AlwaysSucceeds()
    {
        var result = CooperativeZonePlacementUtility.TryPlaceZones(
            Vector2.zero,
            zoneCount: 1,
            zoneRadius: 1f,
            minDistance: 1f,
            maxDistance: 2f,
            minSeparation: 1.5f,
            obstacleMask: 0);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Positions.Length);
        Assert.GreaterOrEqual(Vector2.Distance(Vector2.zero, result.Positions[0]), 0.9f);
    }

    [Test]
    public void TryPlaceZones_DualZone_SeparatesWhenPossible()
    {
        bool separated = false;
        for (int i = 0; i < 32; i++)
        {
            var result = CooperativeZonePlacementUtility.TryPlaceZones(
                Vector2.zero,
                zoneCount: 2,
                zoneRadius: 1f,
                minDistance: 1f,
                maxDistance: 3f,
                minSeparation: 1.8f,
                obstacleMask: 0);

            if (!result.Success || result.Positions.Length < 2)
                continue;

            if (Vector2.Distance(result.Positions[0], result.Positions[1]) >= 1.8f)
            {
                separated = true;
                break;
            }
        }

        Assert.IsTrue(separated);
    }

    [Test]
    public void AreZonesClearOfObstacles_EmptyMask_AlwaysClear()
    {
        bool clear = CooperativeZonePlacementUtility.AreZonesClearOfObstacles(
            new[] { Vector2.zero, Vector2.one },
            zoneRadius: 1f,
            obstacleMask: 0);

        Assert.IsTrue(clear);
    }

    [Test]
    public void ResolveDefaultObstacleMask_IncludesWallLayers()
    {
        LayerMask mask = CooperativeZonePlacementUtility.ResolveDefaultObstacleMask();
        int wall = LayerMask.NameToLayer("Wall");
        int dashable = LayerMask.NameToLayer("DashableWall");

        if (wall >= 0)
            Assert.AreNotEqual(0, mask.value & (1 << wall));
        if (dashable >= 0)
            Assert.AreNotEqual(0, mask.value & (1 << dashable));
    }
}
