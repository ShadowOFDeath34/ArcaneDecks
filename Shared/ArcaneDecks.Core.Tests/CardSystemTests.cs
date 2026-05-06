using System.Collections.Generic;
using System.Linq;
using ArcaneDecks.Core.Systems;
using Xunit;

namespace ArcaneDecks.Core.Tests;

public class CardSystemTests
{
    private readonly CardSystem _system = new();

    private static CardDefinition CreateCard(string id, int cost, CardRarity rarity, params CardEffect[] effects)
    {
        return new CardDefinition(id, $"card.{id}.name", $"card.{id}.desc", cost, CardType.Attack, rarity, effects.ToList());
    }

    [Fact]
    public void RegisterCard_AndGetCard_ReturnsCard()
    {
        var card = CreateCard("strike", 1, CardRarity.Common, new CardEffect(EffectType.Damage, 6));
        _system.RegisterCard(card);

        var result = _system.GetCard("strike");

        Assert.NotNull(result);
        Assert.Equal("strike", result.Id);
        Assert.Equal(1, result.Cost);
    }

    [Fact]
    public void GetCard_MissingId_ReturnsNull()
    {
        var result = _system.GetCard("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void GetCardsByRarity_FiltersCorrectly()
    {
        _system.RegisterCard(CreateCard("common1", 1, CardRarity.Common, new CardEffect(EffectType.Damage, 1)));
        _system.RegisterCard(CreateCard("uncommon1", 2, CardRarity.Uncommon, new CardEffect(EffectType.Damage, 2)));
        _system.RegisterCard(CreateCard("common2", 1, CardRarity.Common, new CardEffect(EffectType.Damage, 3)));

        var uncommon = _system.GetCardsByRarity(CardRarity.Uncommon).ToList();
        var common = _system.GetCardsByRarity(CardRarity.Common).ToList();

        Assert.Single(uncommon);
        Assert.Equal(2, common.Count);
    }

    [Fact]
    public void GetStarterDeck_ReturnsOnlyCommonUpToTen()
    {
        for (int i = 0; i < 15; i++)
        {
            _system.RegisterCard(CreateCard($"card{i}", 1, CardRarity.Common, new CardEffect(EffectType.Damage, i)));
        }

        var deck = _system.GetStarterDeck().ToList();

        Assert.Equal(10, deck.Count);
        Assert.All(deck, c => Assert.Equal(CardRarity.Common, c.Rarity));
    }

    [Fact]
    public void CalculateDamage_WithDamageEffect_AddsBase()
    {
        var card = CreateCard("fireball", 2, CardRarity.Uncommon, new CardEffect(EffectType.Damage, 10));
        _system.RegisterCard(card);

        var damage = _system.CalculateDamage(card, 3);

        Assert.Equal(13, damage);
    }

    [Fact]
    public void CalculateDamage_WithoutDamageEffect_ReturnsBase()
    {
        var card = new CardDefinition("block", "name", "desc", 1, CardType.Skill, CardRarity.Common,
            new List<CardEffect> { new(EffectType.Block, 5) });

        var damage = _system.CalculateDamage(card, 4);

        Assert.Equal(4, damage);
    }

    [Fact]
    public void GetAllCards_ReturnsAllRegistered()
    {
        _system.RegisterCard(CreateCard("a", 1, CardRarity.Common, new CardEffect(EffectType.Damage, 1)));
        _system.RegisterCard(CreateCard("b", 1, CardRarity.Common, new CardEffect(EffectType.Damage, 2)));

        var all = _system.GetAllCards().ToList();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void RegisterCard_OverwritesExisting()
    {
        _system.RegisterCard(CreateCard("same", 1, CardRarity.Common, new CardEffect(EffectType.Damage, 1)));
        _system.RegisterCard(CreateCard("same", 2, CardRarity.Uncommon, new CardEffect(EffectType.Damage, 5)));

        var result = _system.GetCard("same");

        Assert.NotNull(result);
        Assert.Equal(2, result.Cost);
        Assert.Equal(CardRarity.Uncommon, result.Rarity);
    }

    [Fact]
    public void CalculateDamage_OnlyFirstDamageEffectCounts()
    {
        var card = CreateCard("double", 1, CardRarity.Common,
            new CardEffect(EffectType.Damage, 3),
            new CardEffect(EffectType.Damage, 7));

        var damage = _system.CalculateDamage(card, 2);

        Assert.Equal(5, damage); // 3 + 2, ignores second damage effect
    }
}
