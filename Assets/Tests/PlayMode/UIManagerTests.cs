using NUnit.Framework;
using KingdomWar.UI;

public class UIManagerTests
{
    [Test]
    public void Instance_IsNotNull()
    {
        Assert.That(UIManager.Instance, Is.Not.Null);
    }

    [Test]
    public void GetPanelAsync_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            UIManager.Instance.GetPanelAsync(UIPanelType.mainPanel, null);
        });
    }

    [Test]
    public void CreatePromptMessageAsync_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            UIManager.Instance.CreatePromptMessageAsync("test");
        });
    }

    [Test]
    public void GetPanel_ReturnsNull_ForInvalidType()
    {
        var panel = UIManager.Instance.GetPanel((UIPanelType)999);
        Assert.That(panel, Is.Null);
    }

    [Test]
    public void ParseUIText_PopulatesPanelPathDic()
    {
        var field = typeof(UIManager).GetField("panelPathDic",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dic = field?.GetValue(UIManager.Instance) as System.Collections.Generic.Dictionary<UIPanelType, string>;
        Assert.That(dic, Is.Not.Null);
        Assert.That(dic.Count, Is.GreaterThan(0));
    }

    [Test]
    public void ClearPanelCache_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => UIManager.Instance.ClearPanelCache(UIPanelType.mainPanel));
    }
}
