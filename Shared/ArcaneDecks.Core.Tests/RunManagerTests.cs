using System.Linq;
using ArcaneDecks.Core.Systems;
using Xunit;

namespace ArcaneDecks.Core.Tests;

public class RunManagerTests
{
    private static CardSystem CreateCardSystem()
    {
        var cs = new CardSystem();
        cs.RegisterCard(new CardDefinition(
            Id: "strike",
            NameKey: "card.strike.name",
            DescriptionKey: "card.strike.desc",
            Cost: 1,
            Type: CardType.Attack,
            Rarity: CardRarity.Common,
            Effects: new() { new CardEffect(Type: EffectType.Damage, Value: 6) }
        ));
        cs.RegisterCard(new CardDefinition(
            Id: "defend",
            NameKey: "card.defend.name",
            DescriptionKey: "card.defend.desc",
            Cost: 1,
            Type: CardType.Skill,
            Rarity: CardRarity.Common,
            Effects: new() { new CardEffect(Type: EffectType.Block, Value: 5) }
        ));
        return cs;
    }

    [Fact]
    public void StartRun_CreatesActiveRun_WithDefaultHealth()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();

        Assert.True(rm.State.IsActive);
        Assert.Equal(1, rm.State.CurrentFloor);
        Assert.Equal(50, rm.State.PlayerMaxHealth);
        Assert.Equal(50, rm.State.PlayerCurrentHealth);
        Assert.NotEmpty(rm.State.FloorPlan);
    }

    [Fact]
    public void StartRun_PopulatesDeck_WithStarterCards()
    {
        var cs = CreateCardSystem();
        var rm = new RunManager(cs);
        rm.StartRun();

        Assert.NotEmpty(rm.State.DeckCardIds);
        Assert.Equal(rm.State.DeckCardIds.Count, rm.State.DrawPile.Count);
    }

    [Fact]
    public void DrawCards_ReturnsRequestedCount()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();

        var drawn = rm.DrawCards(2);
        Assert.Equal(2, drawn.Count);
        Assert.Equal(0, rm.State.DrawPile.Count);
    }

    [Fact]
    public void DrawCards_ReturnsFewer_WhenNotEnoughCards()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();

        var drawn = rm.DrawCards(5);
        Assert.Equal(2, drawn.Count);
    }

    [Fact]
    public void DrawCards_Reshuffles_WhenDrawPileEmpty()
    {
        var cs = CreateCardSystem();
        var rm = new RunManager(cs);
        rm.StartRun();

        var total = rm.State.DrawPile.Count;
        rm.DrawCards(total);
        Assert.Empty(rm.State.DrawPile);

        rm.DiscardHand(rm.State.DeckCardIds.ToList());
        var drawn = rm.DrawCards(2);
        Assert.Equal(2, drawn.Count);
    }

    [Fact]
    public void DiscardHand_AddsToDiscardPile()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();

        var ids = rm.State.DeckCardIds.Take(2).ToList();
        rm.DiscardHand(ids);

        Assert.Equal(2, rm.State.DiscardPile.Count);
    }

    [Fact]
    public void AddCardToDeck_AddsCard_WhenExists()
    {
        var cs = CreateCardSystem();
        var rm = new RunManager(cs);
        rm.StartRun();

        rm.AddCardToDeck("strike");
        Assert.Contains("strike", rm.State.DeckCardIds);
        Assert.Contains("strike", rm.State.DrawPile);
    }

    [Fact]
    public void RemoveCardFromDeck_RemovesCard()
    {
        var cs = CreateCardSystem();
        cs.RegisterCard(new CardDefinition(
            Id: "quick_strike",
            NameKey: "card.quick_strike.name",
            DescriptionKey: "card.quick_strike.desc",
            Cost: 0,
            Type: CardType.Attack,
            Rarity: CardRarity.Common,
            Effects: new() { new CardEffect(Type: EffectType.Damage, Value: 3) }
        ));
        var rm = new RunManager(cs);
        rm.StartRun();

        rm.RemoveCardFromDeck("quick_strike");
        Assert.DoesNotContain("quick_strike", rm.State.DeckCardIds);
        Assert.DoesNotContain("quick_strike", rm.State.DrawPile);
    }

    [Fact]
    public void HealPlayer_RespectsMaxHealth()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        rm.DamagePlayer(20);
        rm.HealPlayer(5);
        Assert.Equal(35, rm.State.PlayerCurrentHealth);

        rm.HealPlayer(100);
        Assert.Equal(50, rm.State.PlayerCurrentHealth);
    }

    [Fact]
    public void DamagePlayer_ReducesHealth()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        rm.DamagePlayer(15);
        Assert.Equal(35, rm.State.PlayerCurrentHealth);
    }

    [Fact]
    public void DamagePlayer_DoesNotGoBelowZero()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        rm.DamagePlayer(200);
        Assert.Equal(0, rm.State.PlayerCurrentHealth);
    }

    [Fact]
    public void AdvanceFloor_IncrementsFloor()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        rm.AdvanceFloor();
        Assert.Equal(2, rm.State.CurrentFloor);
    }

    [Fact]
    public void GetCurrentFloorType_ReturnsCorrectType()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        var type = rm.GetCurrentFloorType();
        Assert.Equal(rm.State.FloorPlan[0], type);
    }

    [Fact]
    public void IsFinalFloor_TrueOnLastFloor()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        var last = rm.State.FloorPlan.Count;
        rm.State.CurrentFloor = last;
        Assert.True(rm.IsFinalFloor());
    }

    [Fact]
    public void IsFinalFloor_FalseBeforeLastFloor()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        Assert.False(rm.IsFinalFloor());
    }

    [Fact]
    public void EndRun_Victory_SetsScore()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        rm.State.Gold = 10;
        rm.State.CurrentFloor = 5;
        rm.EndRun(true);

        Assert.False(rm.State.IsActive);
        Assert.True(rm.State.IsVictory);
        Assert.True(rm.State.Score > 0);
    }

    [Fact]
    public void EndRun_Defeat_DoesNotAddScore()
    {
        var rm = new RunManager(CreateCardSystem());
        rm.StartRun();
        rm.State.Gold = 10;
        rm.State.CurrentFloor = 5;
        rm.EndRun(false);

        Assert.False(rm.State.IsActive);
        Assert.False(rm.State.IsVictory);
        Assert.Equal(0, rm.State.Score);
    }
}
