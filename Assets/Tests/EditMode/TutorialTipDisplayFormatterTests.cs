using NUnit.Framework;

public class TutorialTipDisplayFormatterTests
{
    [Test]
    public void Format_RequiredOne_ReturnsTextWithoutCounter()
    {
        string result = TutorialTipDisplayFormatter.Format("Rápido! Movimente-se usando WASD", 0, 1);
        Assert.AreEqual("Rápido! Movimente-se usando WASD", result);
    }

    [Test]
    public void Format_RequiredThree_AppendsProgressCounter()
    {
        string result = TutorialTipDisplayFormatter.Format("Agora acabe com essa infestação", 0, 3);
        Assert.AreEqual("Agora acabe com essa infestação 0/3", result);
    }

    [Test]
    public void Format_ProgressUpdates_ClampsToRequired()
    {
        Assert.AreEqual("Agora acabe com essa infestação 2/3",
            TutorialTipDisplayFormatter.Format("Agora acabe com essa infestação", 2, 3));
        Assert.AreEqual("Agora acabe com essa infestação 3/3",
            TutorialTipDisplayFormatter.Format("Agora acabe com essa infestação", 99, 3));
    }

    [Test]
    public void FormatAbilityKeys_NoneUsed_ShowsQR()
    {
        Assert.AreEqual(
            "Muito bom, agora use suas habilidades! Q R",
            TutorialTipDisplayFormatter.FormatAbilityKeys("Muito bom, agora use suas habilidades!", 0));
    }

    [Test]
    public void FormatAbilityKeys_QUsed_ShowsOnlyR()
    {
        Assert.AreEqual(
            "Muito bom, agora use suas habilidades! R",
            TutorialTipDisplayFormatter.FormatAbilityKeys("Muito bom, agora use suas habilidades!", 1 << 0));
    }

    [Test]
    public void FormatAbilityKeys_RUsed_ShowsOnlyQ()
    {
        Assert.AreEqual(
            "Muito bom, agora use suas habilidades! Q",
            TutorialTipDisplayFormatter.FormatAbilityKeys("Muito bom, agora use suas habilidades!", 1 << 1));
    }

    [Test]
    public void FormatAbilityKeys_BothUsed_ShowsBaseOnly()
    {
        Assert.AreEqual(
            "Muito bom, agora use suas habilidades!",
            TutorialTipDisplayFormatter.FormatAbilityKeys("Muito bom, agora use suas habilidades!", (1 << 0) | (1 << 1)));
    }
}
