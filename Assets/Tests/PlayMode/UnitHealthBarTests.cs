using NUnit.Framework;
using KingdomWar.UI;
using UnityEngine;

public class UnitHealthBarTests
{
    private GameObject cameraGo;

    [SetUp]
    public void SetUp()
    {
        cameraGo = new GameObject("MainCamera");
        cameraGo.tag = "MainCamera";
        cameraGo.AddComponent<Camera>();
    }

    [TearDown]
    public void TearDown()
    {
        if (cameraGo != null)
            Object.DestroyImmediate(cameraGo);
    }

    [Test]
    public void Initialize_CreatesCanvas()
    {
        var go = new GameObject();
        var healthBar = go.AddComponent<UnitHealthBar>();
        healthBar.Initialize(go.transform, new Vector3(0, 2, 0));

        var canvas = go.transform.Find("HealthBarCanvas");
        Assert.That(canvas, Is.Not.Null);

        var bg = canvas.Find("Background");
        Assert.That(bg, Is.Not.Null);

        var bar = canvas.Find("Bar");
        Assert.That(bar, Is.Not.Null);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void UpdateHealth_ChangesBarFill()
    {
        var go = new GameObject();
        var healthBar = go.AddComponent<UnitHealthBar>();
        healthBar.Initialize(go.transform, new Vector3(0, 2, 0));

        healthBar.UpdateHealth(0.5f);

        var canvas = go.transform.Find("HealthBarCanvas");
        var bar = canvas.Find("Bar");
        var barRect = bar.GetComponent<RectTransform>();
        Assert.That(barRect.anchorMax.x, Is.EqualTo(0.5f).Within(0.01f));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void UpdateHealth_FullHealth_ShowsGreen()
    {
        var go = new GameObject();
        var healthBar = go.AddComponent<UnitHealthBar>();
        healthBar.Initialize(go.transform, new Vector3(0, 2, 0));

        healthBar.UpdateHealth(1.0f);

        var canvas = go.transform.Find("HealthBarCanvas");
        var bar = canvas.Find("Bar");
        var barRect = bar.GetComponent<RectTransform>();
        Assert.That(barRect.anchorMax.x, Is.EqualTo(1.0f).Within(0.01f));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Cleanup_DestroysCanvas()
    {
        var go = new GameObject();
        var healthBar = go.AddComponent<UnitHealthBar>();
        healthBar.Initialize(go.transform, new Vector3(0, 2, 0));

        healthBar.Cleanup();

        var canvas = go.transform.Find("HealthBarCanvas");
        Assert.That(canvas, Is.Null);

        Object.DestroyImmediate(go);
    }
}
