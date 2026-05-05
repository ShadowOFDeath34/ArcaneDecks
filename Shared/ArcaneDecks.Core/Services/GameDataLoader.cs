using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Services;

public class GameDataLoader
{
    private readonly string _basePath;

    public GameDataLoader(string basePath)
    {
        _basePath = basePath;
    }

    public List<CardDefinition> LoadCards()
    {
        var path = Path.Combine(_basePath, "cards.json");
        if (!File.Exists(path)) return new List<CardDefinition>();

        var json = File.ReadAllText(path);
        var root = JsonSerializer.Deserialize<CardDataRoot>(json);
        if (root?.Cards == null) return new List<CardDefinition>();

        var cards = new List<CardDefinition>();
        foreach (var c in root.Cards)
        {
            var effects = new List<CardEffect>();
            foreach (var e in c.Effects)
            {
                if (Enum.TryParse<EffectType>(e.Type, out var effectType))
                {
                    effects.Add(new CardEffect(effectType, e.Value, e.TargetKey));
                }
            }

            if (Enum.TryParse<CardType>(c.Type, out var cardType) &
                Enum.TryParse<CardRarity>(c.Rarity, out var cardRarity))
            {
                cards.Add(new CardDefinition(c.Id, c.NameKey, c.DescriptionKey, c.Cost, cardType, cardRarity, effects));
            }
        }
        return cards;
    }

    public List<EnemyTemplate> LoadEnemies()
    {
        var path = Path.Combine(_basePath, "enemies.json");
        if (!File.Exists(path)) return new List<EnemyTemplate>();

        var json = File.ReadAllText(path);
        var root = JsonSerializer.Deserialize<EnemyDataRoot>(json);
        var enemies = new List<EnemyTemplate>();
        if (root?.Enemies != null)
        {
            foreach (var e in root.Enemies) { e.IsBoss = false; enemies.Add(e); }
        }
        if (root?.Bosses != null)
        {
            foreach (var b in root.Bosses) { b.IsBoss = true; enemies.Add(b); }
        }
        return enemies;
    }

    private class CardDataRoot
    {
        [JsonPropertyName("cards")]
        public List<CardDto> Cards { get; set; } = new();
    }

    private class CardDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("nameKey")]
        public string NameKey { get; set; } = "";
        [JsonPropertyName("descriptionKey")]
        public string DescriptionKey { get; set; } = "";
        [JsonPropertyName("cost")]
        public int Cost { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        [JsonPropertyName("rarity")]
        public string Rarity { get; set; } = "";
        [JsonPropertyName("effects")]
        public List<EffectDto> Effects { get; set; } = new();
    }

    private class EffectDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        [JsonPropertyName("value")]
        public int Value { get; set; }
        [JsonPropertyName("targetKey")]
        public string? TargetKey { get; set; }
    }

    private class EnemyDataRoot
    {
        [JsonPropertyName("enemies")]
        public List<EnemyTemplate> Enemies { get; set; } = new();
        [JsonPropertyName("bosses")]
        public List<EnemyTemplate> Bosses { get; set; } = new();
    }
}

public class EnemyTemplate
{
    public string Id { get; set; } = "";
    public string NameKey { get; set; } = "";
    public int MaxHealth { get; set; }
    public int Damage { get; set; }
    public int Armor { get; set; }
    public bool IsBoss { get; set; }
}
