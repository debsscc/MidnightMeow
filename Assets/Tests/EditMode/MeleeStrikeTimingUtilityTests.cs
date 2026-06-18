using NUnit.Framework;

public class MeleeStrikeTimingUtilityTests
{
    private MeleeCombatStats _stats;

    [SetUp]
    public void SetUp()
    {
        _stats = UnityEngine.ScriptableObject.CreateInstance<MeleeCombatStats>();
        _stats.strikeNormalizedTime = 0.35f;
        _stats.recoveryNormalizedTime = 0f;
        _stats.attackAnimationSpeedMultiplier = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        if (_stats != null)
            UnityEngine.Object.DestroyImmediate(_stats);
    }

    [Test]
    public void ComputeStrikeDelay_UsesNormalizedClipTime()
    {
        float delay = MeleeStrikeTimingUtility.ComputeStrikeDelay(_stats, clipLength: 1f, attackSpeedMultiplier: 1f);
        Assert.AreEqual(0.35f, delay, 0.001f);
    }

    [Test]
    public void ComputeStrikeDelay_ScalesWithAttackSpeed()
    {
        float delay = MeleeStrikeTimingUtility.ComputeStrikeDelay(_stats, clipLength: 1f, attackSpeedMultiplier: 2f);
        Assert.AreEqual(0.175f, delay, 0.001f);
    }

    [Test]
    public void ComputeRecoveryDelay_AutoFillsRemainderOfClip()
    {
        float recovery = MeleeStrikeTimingUtility.ComputeRecoveryDelay(_stats, clipLength: 1f, attackSpeedMultiplier: 1f);
        Assert.AreEqual(0.65f, recovery, 0.001f);
    }

    [Test]
    public void ComputeRecoveryDelay_UsesExplicitOverride()
    {
        _stats.recoveryNormalizedTime = 0.2f;
        float recovery = MeleeStrikeTimingUtility.ComputeRecoveryDelay(_stats, clipLength: 1f, attackSpeedMultiplier: 1f);
        Assert.AreEqual(0.2f, recovery, 0.001f);
    }
}
