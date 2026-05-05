using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneDecks.Core.Systems;

public record CardDefinition(
    string Id,
    string NameKey,
    string DescriptionKey,
    int Cost,
    CardType Type,
    CardRarity Rarity,
    List<CardEffect> Effects
);

public enum CardType { Attack, Skill, Power }
public enum CardRarity { Common, Uncommon, Rare, Legendary }

public record CardEffect(
    EffectType Type,
    int Value,
    string? TargetKey = null
);

public enum EffectType { Damage, Block, Heal, Draw, ApplyStatus, DestroyArmor }

public class CardSystem
{
    private readonly Dictionary<string, CardDefinition> _cards = new();

    public void RegisterCard(CardDefinition card)
    {
        _cards[card.Id] = card;
    }

    public CardDefinition? GetCard(string id)
    {
        return _cards.TryGetValue(id, out var card) ? card : null;
    }

    public IEnumerable<CardDefinition> GetCardsByRarity(CardRarity rarity)
    {
        return _cards.Values.Where(c => c.Rarity == rarity);
    }

    public IEnumerable<CardDefinition> GetStarterDeck()
    {
        return _cards.Values.Where(c => c.Rarity == CardRarity.Common).Take(10);
    }

    public IEnumerable<CardDefinition> GetAllCards()
    {
        return _cards.Values;
    }

    public int CalculateDamage(CardDefinition card, int baseDamage)
    {
        var damageEffect = card.Effects.FirstOrDefault(e => e.Type == EffectType.Damage);
        return damageEffect != null ? damageEffect.Value + baseDamage : baseDamage;
    }
}
