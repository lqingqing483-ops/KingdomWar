using NUnit.Framework;
using UnityEngine;
using KingdomWar.Game.Cards;

public class CardDatabaseTests
{
    [Test]
    public void AllCards_LoadFromResources_AtLeastEight()
    {
        var cards = Resources.LoadAll<CardData>("Cards");
        Assert.That(cards, Is.Not.Null);
        Assert.That(cards.Length, Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void Knight_IsLoaded_AsUnit_3Elixir()
    {
        CardData card = Resources.Load<CardData>("Cards/骑士");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Unit));
        Assert.That(card.elixirCost, Is.EqualTo(3));
    }

    [Test]
    public void Archer_IsLoaded_AsUnit_3Elixir()
    {
        CardData card = Resources.Load<CardData>("Cards/弓箭手");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Unit));
        Assert.That(card.elixirCost, Is.EqualTo(3));
    }

    [Test]
    public void Fireball_IsLoaded_AsSpell_4Elixir()
    {
        CardData card = Resources.Load<CardData>("Cards/火球");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Spell));
        Assert.That(card.elixirCost, Is.EqualTo(4));
    }

    [Test]
    public void Giant_IsLoaded_AsUnit_5Elixir()
    {
        CardData card = Resources.Load<CardData>("Cards/巨人");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Unit));
        Assert.That(card.elixirCost, Is.EqualTo(5));
    }

    [Test]
    public void Lightning_IsLoaded_AsSpell_6Elixir()
    {
        CardData card = Resources.Load<CardData>("Cards/闪电");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Spell));
        Assert.That(card.elixirCost, Is.EqualTo(6));
    }

    [Test]
    public void PEKKA_IsLoaded_AsUnit()
    {
        CardData card = Resources.Load<CardData>("Cards/皮卡超人");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Unit));
        Assert.That(card.elixirCost, Is.InRange(3, 10));
    }

    [Test]
    public void ElixirCollector_IsLoaded_AsBuilding()
    {
        CardData card = Resources.Load<CardData>("Cards/圣水收集器");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Building));
        Assert.That(card.elixirCost, Is.InRange(3, 10));
    }

    [Test]
    public void Zap_IsLoaded_AsSpell_2Elixir()
    {
        CardData card = Resources.Load<CardData>("Cards/电击");
        Assert.That(card, Is.Not.Null);
        Assert.That(card.cardType, Is.EqualTo(CardType.Spell));
        Assert.That(card.elixirCost, Is.EqualTo(2));
    }

    [Test]
    public void CardDatabase_LoadsAllCardsFromResources()
    {
        var cards = Resources.LoadAll<CardData>("Cards");
        Assert.That(cards, Is.Not.Null);
        Assert.That(cards.Length, Is.GreaterThanOrEqualTo(8));
    }
}
