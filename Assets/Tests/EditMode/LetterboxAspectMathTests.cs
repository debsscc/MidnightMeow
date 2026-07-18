using NUnit.Framework;
using UnityEngine;

public class LetterboxAspectMathTests
{
    [Test]
    public void CalculateNormalizedViewport_Exact16x9_ReturnsFullRect()
    {
        Rect rect = LetterboxAspectMath.CalculateNormalizedViewport(1920, 1080, 16f / 9f);
        Assert.AreEqual(0f, rect.x, 0.0001f);
        Assert.AreEqual(0f, rect.y, 0.0001f);
        Assert.AreEqual(1f, rect.width, 0.0001f);
        Assert.AreEqual(1f, rect.height, 0.0001f);
        Assert.IsFalse(LetterboxAspectMath.HasBars(rect));
    }

    [Test]
    public void CalculateNormalizedViewport_Ultrawide2560x1080_PillarboxesSides()
    {
        Rect rect = LetterboxAspectMath.CalculateNormalizedViewport(2560, 1080, 16f / 9f);

        // Viewport width = (16/9) / (2560/1080) = 1920/2560 = 0.75
        Assert.AreEqual(0.125f, rect.x, 0.0001f);
        Assert.AreEqual(0f, rect.y, 0.0001f);
        Assert.AreEqual(0.75f, rect.width, 0.0001f);
        Assert.AreEqual(1f, rect.height, 0.0001f);
        Assert.IsTrue(LetterboxAspectMath.HasBars(rect));
    }

    [Test]
    public void CalculateNormalizedViewport_Tall4x3_LetterboxesTopBottom()
    {
        Rect rect = LetterboxAspectMath.CalculateNormalizedViewport(1440, 1080, 16f / 9f);

        // screenAspect = 1440/1080 = 4/3; height = (4/3) / (16/9) = 0.75
        Assert.AreEqual(0f, rect.x, 0.0001f);
        Assert.AreEqual(0.125f, rect.y, 0.0001f);
        Assert.AreEqual(1f, rect.width, 0.0001f);
        Assert.AreEqual(0.75f, rect.height, 0.0001f);
        Assert.IsTrue(LetterboxAspectMath.HasBars(rect));
    }

    [Test]
    public void CalculateNormalizedViewport_InvalidInputs_ReturnsFullRect()
    {
        Rect rect = LetterboxAspectMath.CalculateNormalizedViewport(0, 1080, 16f / 9f);
        Assert.AreEqual(1f, rect.width, 0.0001f);
        Assert.AreEqual(1f, rect.height, 0.0001f);
    }
}
