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

    [Test]
    public void DeleteSlot_RemovesFileAndCanContinueBecomesFalse()
    {
        SaveProfileStore store = _host.GetComponent<SaveProfileStore>();
        store.Active.wasHost = true;
        store.SaveActive();

        Assert.IsTrue(store.CanContinue(0));
        Assert.IsTrue(store.DeleteSlot(0));
        Assert.IsFalse(store.HasSave(0));
        Assert.IsFalse(store.CanContinue(0));
    }

    [Test]
    public void DeleteAllSlots_RemovesEverySaveFile()
    {
        SaveProfileStore store = _host.GetComponent<SaveProfileStore>();
        store.Active.wasHost = true;
        store.SaveActive();

        int deleted = store.DeleteAllSlots();
        Assert.AreEqual(1, deleted);
        Assert.IsFalse(store.HasAnySave());
        Assert.IsFalse(store.HasAnyHostSave());
    }
}
