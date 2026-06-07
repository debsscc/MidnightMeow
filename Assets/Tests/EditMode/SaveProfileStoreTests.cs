using NUnit.Framework;
using System.IO;
using UnityEngine;

public class SaveProfileStoreTests
{
    private GameObject _host;
    private string _saveDir;

    [SetUp]
    public void SetUp()
    {
        _saveDir = Path.Combine(Application.persistentDataPath, "MidnightMeow", "saves");
        if (Directory.Exists(_saveDir))
        {
            foreach (string file in Directory.GetFiles(_saveDir, "save_slot_*.json"))
                File.Delete(file);
        }

        _host = new GameObject("SaveProfileStoreTest");
        _host.AddComponent<SaveProfileStore>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_host != null)
            Object.DestroyImmediate(_host);
    }

    [Test]
    public void CanContinue_IsFalseWithoutSave()
    {
        SaveProfileStore store = _host.GetComponent<SaveProfileStore>();
        Assert.IsFalse(store.CanContinue(0));
    }

    [Test]
    public void CanContinue_RequiresHostSave()
    {
        SaveProfileStore store = _host.GetComponent<SaveProfileStore>();
        store.Active.wasHost = true;
        store.SaveActive();

        Assert.IsTrue(store.CanContinue(0));
    }

    [Test]
    public void TrySpendMagiculas_ReducesBalance()
    {
        SaveProfileStore store = _host.GetComponent<SaveProfileStore>();
        store.Active.magiculas = 5;
        store.SaveActive();

        Assert.IsTrue(store.TrySpendMagiculas(2));
        Assert.AreEqual(3, store.Active.magiculas);
    }
}
