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
            minSeparation: 1.5f);

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
                minSeparation: 1.8f);

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
}
