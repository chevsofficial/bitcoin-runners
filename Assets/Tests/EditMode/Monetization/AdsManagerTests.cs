using NUnit.Framework;
using UnityEngine;

public class AdsManagerTests
{
    class InMemorySaveStorage : ISaveStorage
    {
        string _data;

        public bool TryLoad(out string serializedData)
        {
            serializedData = _data;
            return !string.IsNullOrEmpty(_data);
        }

        public void Save(string serializedData)
        {
            _data = serializedData;
        }
    }

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();
        SaveSystem.SetStorage(new InMemorySaveStorage());
        SaveSystem.SetMigrations(null);
        ServiceLocator.ShutdownAll();
    }

    [TearDown]
    public void TearDown()
    {
        ServiceLocator.ShutdownAll();
        PlayerPrefs.DeleteAll();
    }

    [Test]
    public void ResultsInterstitialHonorsRemoveAdsPreferenceWhenIapIsUnavailable()
    {
        Prefs.RemoveAds = true;

        var go = new GameObject("AdsManagerTest");
        try
        {
            var adsManager = go.AddComponent<AdsManager>();
            Assert.IsNotNull(AdsManager.I, "AdsManager failed to register with the service locator.");

            adsManager.OnRunEnded();
            adsManager.OnRunEnded();

            bool started = adsManager.TryShowInterstitial("results_interstitial");

            Assert.IsFalse(started, "Interstitial should not show when the remove-ads entitlement exists in saved preferences.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
