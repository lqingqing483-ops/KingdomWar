using NUnit.Framework;
using KingdomWar.Game.Shop;
using KingdomWar.Game.Cards;

public class ShopManagerTests
{
    [SetUp]
    public void SetUp()
    {
        var db = CardDatabase.Instance;
        if (db.GetAllCards().Count == 0)
            db.ReloadCardData();
    }
    [Test]
    public void Instance_IsNotNull()
    {
        Assert.That(ShopManager.Instance, Is.Not.Null);
    }

    [Test]
    public void GetDailyItems_ReturnsThreeItems()
    {
        var items = ShopManager.Instance.GetDailyItems();
        Assert.That(items.Count, Is.EqualTo(3));
    }

    [Test]
    public void GetDailyItems_ItemsHaveValidData()
    {
        var items = ShopManager.Instance.GetDailyItems();
        foreach (var item in items)
        {
            Assert.That(item.itemId, Is.Not.Empty);
            Assert.That(item.cardName, Is.Not.Empty);
            Assert.That(item.price, Is.GreaterThan(0));
        }
    }

    [Test]
    public void PurchaseItem_FailsForAlreadyPurchased()
    {
        var items = ShopManager.Instance.GetDailyItems();
        if (items.Count > 0)
        {
            ShopManager.Instance.PurchaseItem(items[0]);
            bool secondPurchase = ShopManager.Instance.PurchaseItem(items[0]);
            Assert.That(secondPurchase, Is.False);
        }
    }

    [Test]
    public void RefreshDailyShop_GeneratesNewItems()
    {
        ShopManager.Instance.RefreshDailyShop();
        var after = ShopManager.Instance.GetDailyItems();
        Assert.That(after.Count, Is.EqualTo(3));
    }
}
