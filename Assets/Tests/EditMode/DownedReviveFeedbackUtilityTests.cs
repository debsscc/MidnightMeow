using NUnit.Framework;

public class DownedReviveFeedbackUtilityTests
{
    [Test]
    public void ShouldShowFeedback_ReturnsFalse_WhenTimerExpiredOrBleedingOut()
    {
        Assert.IsFalse(DownedReviveFeedbackUtility.ShouldShowFeedback(true, false, 12f));
        Assert.IsFalse(DownedReviveFeedbackUtility.ShouldShowFeedback(true, true, 0f));
        Assert.IsFalse(DownedReviveFeedbackUtility.ShouldShowFeedback(false, true, 12f));
    }

    [Test]
    public void ShouldShowFeedback_ReturnsTrue_WhenAllyAliveAndReviveWindowOpen()
    {
        Assert.IsTrue(DownedReviveFeedbackUtility.ShouldShowFeedback(true, true, 30f));
    }

    [Test]
    public void ComputeUrgency_IncreasesAsTimeRunsOut()
    {
        float early = DownedReviveFeedbackUtility.ComputeUrgency(40f, 45f);
        float late = DownedReviveFeedbackUtility.ComputeUrgency(5f, 45f);

        Assert.Less(early, late);
        Assert.AreEqual(0f, DownedReviveFeedbackUtility.ComputeUrgency(45f, 45f), 0.001f);
        Assert.AreEqual(1f, DownedReviveFeedbackUtility.ComputeUrgency(0f, 45f), 0.001f);
    }

    [Test]
    public void ComputePulseStress_ScalesWithUrgency()
    {
        float calm = DownedReviveFeedbackUtility.ComputePulseStress(0.4f, 0f);
        float urgent = DownedReviveFeedbackUtility.ComputePulseStress(0.4f, 1f);

        Assert.Less(calm, urgent);
    }

    [Test]
    public void BindSfxOutput_ReturnsFalse_WhenSourceIsNull()
    {
        Assert.IsFalse(GameAudioSettings.BindSfxOutput(null));
    }
}
