using NUnit.Framework;

public class VictoryContinueResolverTests
{
    [Test]
    public void IsFinalPhase_ContractIndex2_ReturnsTrue()
    {
        Assert.IsTrue(VictoryContinueResolver.IsFinalPhase(2, "Fase-1"));
    }

    [Test]
    public void IsFinalPhase_Fase3Scene_ReturnsTrue()
    {
        Assert.IsTrue(VictoryContinueResolver.IsFinalPhase(0, "Fase-3"));
        Assert.IsTrue(VictoryContinueResolver.IsFinalPhase(-1, "Fase-3"));
    }

    [Test]
    public void IsFinalPhase_EarlierPhases_ReturnsFalse()
    {
        Assert.IsFalse(VictoryContinueResolver.IsFinalPhase(0, "Fase-1"));
        Assert.IsFalse(VictoryContinueResolver.IsFinalPhase(1, "Fase-2"));
    }

    [Test]
    public void ResolveNextContractIndex_AdvancesByOne()
    {
        Assert.AreEqual(1, VictoryContinueResolver.ResolveNextContractIndex(0, "Fase-1"));
        Assert.AreEqual(2, VictoryContinueResolver.ResolveNextContractIndex(1, "Fase-2"));
    }

    [Test]
    public void ResolveNextContractIndex_InfersFromSceneWhenIndexMissing()
    {
        Assert.AreEqual(1, VictoryContinueResolver.ResolveNextContractIndex(-1, "Fase-1"));
        Assert.AreEqual(2, VictoryContinueResolver.ResolveNextContractIndex(-1, "Fase-2"));
    }

    [Test]
    public void InferContractIndexFromScene_MapsPhaseScenes()
    {
        Assert.AreEqual(0, VictoryContinueResolver.InferContractIndexFromScene("Fase-1"));
        Assert.AreEqual(1, VictoryContinueResolver.InferContractIndexFromScene("Fase-2"));
        Assert.AreEqual(2, VictoryContinueResolver.InferContractIndexFromScene("Fase-3"));
        Assert.AreEqual(-1, VictoryContinueResolver.InferContractIndexFromScene("Menu2"));
    }
}
