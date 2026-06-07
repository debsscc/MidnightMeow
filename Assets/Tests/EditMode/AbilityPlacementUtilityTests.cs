using NUnit.Framework;
using UnityEngine;

public class AbilityPlacementUtilityTests
{
    [Test]
    public void TryGetPlacement_ClampsToMaxRange()
    {
        var userGo = new GameObject("user");
        userGo.transform.position = Vector3.zero;

        var camGo = new GameObject("cam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        var result = AbilityPlacementUtility.TryGetPlacement(
            userGo.transform,
            cam,
            maxRange: 2f,
            fallbackDirection: Vector2.up);

        Assert.IsTrue(result.Success);
        Assert.LessOrEqual(Vector2.Distance(userGo.transform.position, result.WorldPosition), 2.01f);

        Object.DestroyImmediate(userGo);
        Object.DestroyImmediate(camGo);
    }
}
