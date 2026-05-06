using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneDecks.Core.Systems;

public enum FloorType { Combat, Shop, Boss, Event }

public class RunState
{
    public bool IsActive { get; set; }
    public int CurrentFloor { get; set; }
    public int PlayerMaxHealth { get; set; } = 55;
    public int PlayerCurrentHealth { get; set; } = 55;
    public List<string> DeckCardIds { get; set; } = new();
    public List<string> DrawPile { get; set; } = new();
    public List<string> DiscardPile { get; set; } = new();
    public int Gold { get; set; }
    public int Score { get; set; }
    public bool IsVictory { get; set; }
    public List<FloorType> FloorPlan { get; set; } = new();
    public string? SeasonalEventId { get; set; }
    public string? SeasonalEventKey { get; set; }
    public Dictionary<string, object> SeasonalRules { get; set; } = new();
}

public class RunManager
{
    public RunState State { get; } = new();
    private readonly CardSystem _cardSystem;
    private readonly Random _random = new();
    public MetaProgressionSystem? MetaProgressionSystem { get; set; }

    public RunManager(CardSystem cardSystem)
    {
        _cardSystem = cardSystem;
    }

    public void StartRun()
    {
        var starterCards = _cardSystem.GetStarterDeck().Select(c => c.Id).ToList();
        if (starterCards.Count == 0)
        {
            starterCards = _cardSystem.GetAllCards()
                .Where(c => c.Rarity == CardRarity.Common)
                .Select(c => c.Id)
                .Take(10)
                .ToList();
        }

        State.IsActive = true;
        State.IsVictory = false;
        State.CurrentFloor = 1;
        State.PlayerMaxHealth = 55;
        State.PlayerCurrentHealth = 55;
        State.DeckCardIds = new List<string>(starterCards);
        State.DrawPile = new List<string>(State.DeckCardIds);
        State.DiscardPile = new List<string>();
        State.Gold = 0;
        State.Score = 0;
        State.FloorPlan = GenerateFloorPlan();
        State.SeasonalEventId = null;
        State.SeasonalEventKey = null;
        State.SeasonalRules = new Dictionary<string, object>();
        MetaProgressionSystem?.ApplyToRun(State);
        Shuffle(State.DrawPile);
    }

    public void StartSeasonalRun(string eventId, string eventKey, Dictionary<string, object> rules)
    {
        StartRun();
        State.SeasonalEventId = eventId;
        State.SeasonalEventKey = eventKey;
        State.SeasonalRules = new Dictionary<string, object>(rules);

        // Apply event modifiers
        if (rules.TryGetValue("startingHealthModifier", out var healthMod) && healthMod is double healthModVal)
        {
            State.PlayerMaxHealth += (int)healthModVal;
            State.PlayerCurrentHealth = State.PlayerMaxHealth;
        }
        if (rules.TryGetValue("startingGold", out var startGold) && startGold is double goldVal)
        {
            State.Gold = (int)goldVal;
        }
        if (rules.TryGetValue("forcedCards", out var forced) && forced is List<object> forcedList)
        {
            foreach (var cardIdObj in forcedList)
            {
                var cardId = cardIdObj?.ToString();
                if (!string.IsNullOrEmpty(cardId) && !State.DeckCardIds.Contains(cardId))
                {
                    State.DeckCardIds.Add(cardId);
                    State.DrawPile.Add(cardId);
                }
            }
            Shuffle(State.DrawPile);
        }
    }

    public void RestoreState(RunState state)
    {
        if (state == null) return;
        State.IsActive = state.IsActive;
        State.IsVictory = state.IsVictory;
        State.CurrentFloor = state.CurrentFloor;
        State.PlayerMaxHealth = state.PlayerMaxHealth;
        State.PlayerCurrentHealth = state.PlayerCurrentHealth;
        State.DeckCardIds = new List<string>(state.DeckCardIds);
        State.DrawPile = new List<string>(state.DrawPile);
        State.DiscardPile = new List<string>(state.DiscardPile);
        State.Gold = state.Gold;
        State.Score = state.Score;
        State.FloorPlan = new List<FloorType>(state.FloorPlan);
        State.SeasonalEventId = state.SeasonalEventId;
        State.SeasonalEventKey = state.SeasonalEventKey;
        State.SeasonalRules = new Dictionary<string, object>(state.SeasonalRules);
    }

    public void EndRun(bool victory)
    {
        State.IsActive = false;
        State.IsVictory = victory;
        if (victory)
        {
            State.Score += State.Gold * 10 + State.CurrentFloor * 100;
        }

        // Apply seasonal score multiplier if present
        if (State.SeasonalRules.TryGetValue("scoreMultiplier", out var multObj) && multObj is double mult)
        {
            State.Score = (int)(State.Score * mult);
        }
    }

    public List<CardDefinition> DrawCards(int count)
    {
        var drawn = new List<CardDefinition>();
        for (int i = 0; i < count; i++)
        {
            if (State.DrawPile.Count == 0)
            {
                if (State.DiscardPile.Count == 0) break;
                State.DrawPile.AddRange(State.DiscardPile);
                State.DiscardPile.Clear();
                Shuffle(State.DrawPile);
            }
            if (State.DrawPile.Count == 0) break;
            var id = State.DrawPile[0];
            State.DrawPile.RemoveAt(0);
            var card = _cardSystem.GetCard(id);
            if (card != null)
            {
                drawn.Add(card);
            }
        }
        return drawn;
    }

    public void DiscardHand(List<string> cardIds)
    {
        foreach (var id in cardIds)
        {
            if (!State.DiscardPile.Contains(id))
                State.DiscardPile.Add(id);
        }
    }

    public void AddCardToDeck(string cardId)
    {
        if (_cardSystem.GetCard(cardId) != null && !State.DeckCardIds.Contains(cardId))
        {
            State.DeckCardIds.Add(cardId);
            State.DrawPile.Add(cardId);
        }
    }

    public void RemoveCardFromDeck(string cardId)
    {
        State.DeckCardIds.Remove(cardId);
        State.DrawPile.Remove(cardId);
        State.DiscardPile.Remove(cardId);
    }

    public void HealPlayer(int amount)
    {
        State.PlayerCurrentHealth = Math.Min(State.PlayerMaxHealth, State.PlayerCurrentHealth + amount);
    }

    public void DamagePlayer(int amount)
    {
        State.PlayerCurrentHealth = Math.Max(0, State.PlayerCurrentHealth - amount);
    }

    public void AdvanceFloor()
    {
        if (State.CurrentFloor < State.FloorPlan.Count)
        {
            State.CurrentFloor++;
        }
    }

    public FloorType GetCurrentFloorType()
    {
        if (State.CurrentFloor <= 0 || State.CurrentFloor > State.FloorPlan.Count)
            return FloorType.Combat;
        return State.FloorPlan[State.CurrentFloor - 1];
    }

    public bool IsFinalFloor()
    {
        return State.CurrentFloor >= State.FloorPlan.Count;
    }

    private List<FloorType> GenerateFloorPlan()
    {
        return new List<FloorType>
        {
            FloorType.Combat,
            FloorType.Combat,
            FloorType.Shop,
            FloorType.Combat,
            FloorType.Combat,
            FloorType.Shop,
            FloorType.Combat,
            FloorType.Combat,
            FloorType.Combat,
            FloorType.Boss,
        };
    }

    private void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
