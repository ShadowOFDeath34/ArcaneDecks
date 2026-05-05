using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneDecks.Core.Systems;
using Xunit;

namespace ArcaneDecks.Core.Tests;

public class CombatSystemTests
{
    private readonly CombatSystem _combat = new();

    private static CombatEntity CreatePlayer(int health = 50)
    {
        return new CombatEntity { NameKey = "player", MaxHealth = health, CurrentHealth = health, IsPlayer = true };
    }

    private static CombatEntity CreateEnemy(int health = 30)
    {
        return new CombatEntity { NameKey = "goblin", MaxHealth = health, CurrentHealth = health };
    }

    private static CardDefinition CreateCard(int cost, params CardEffect[] effects)
    {
        return new CardDefinition("test", "name", "desc", cost, CardType.Attack, CardRarity.Common, effects.ToList());
    }

    [Fact]
    public void StartCombat_InitializesState()
    {
        var player = CreatePlayer();
        var enemy = CreateEnemy();

        _combat.StartCombat(player, new[] { enemy });

        Assert.True(_combat.CombatActive);
        Assert.True(_combat.IsPlayerTurn);
        Assert.Equal(1, _combat.CurrentTurn);
        Assert.Equal(player, _combat.Player);
        Assert.Single(_combat.Enemies);
    }

    [Fact]
    public void StartCombat_ResetsEnergy()
    {
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });

        Assert.Equal(_combat.MaxEnergy, _combat.CurrentEnergy);
    }

    [Fact]
    public void PlayCard_DamagesTarget()
    {
        var player = CreatePlayer();
        var enemy = CreateEnemy();
        _combat.StartCombat(player, new[] { enemy });
        var card = CreateCard(1, new CardEffect(EffectType.Damage, 6));

        _combat.PlayCard(card, enemy);

        Assert.Equal(24, enemy.CurrentHealth);
    }

    [Fact]
    public void PlayCard_WithoutEnergy_ReturnsFalse()
    {
        var player = CreatePlayer();
        var enemy = CreateEnemy();
        _combat.StartCombat(player, new[] { enemy });
        var card = CreateCard(5, new CardEffect(EffectType.Damage, 10)); // cost > max energy

        var result = _combat.PlayCard(card, enemy);

        Assert.False(result);
        Assert.Equal(30, enemy.CurrentHealth);
    }

    [Fact]
    public void PlayCard_ConsumesEnergy()
    {
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });
        var card = CreateCard(2, new CardEffect(EffectType.Damage, 5));

        _combat.PlayCard(card);

        Assert.Equal(1, _combat.CurrentEnergy);
    }

    [Fact]
    public void PlayCard_Block_AddsArmor()
    {
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });
        var card = CreateCard(1, new CardEffect(EffectType.Block, 5));

        _combat.PlayCard(card);

        Assert.Equal(5, player.Armor);
    }

    [Fact]
    public void PlayCard_Heal_RespectsMaxHealth()
    {
        var player = CreatePlayer(50);
        player.CurrentHealth = 45;
        _combat.StartCombat(player, new[] { CreateEnemy() });
        var card = CreateCard(1, new CardEffect(EffectType.Heal, 10));

        _combat.PlayCard(card);

        Assert.Equal(50, player.CurrentHealth);
    }

    [Fact]
    public void PlayCard_ArmorAbsorbsDamage()
    {
        var player = CreatePlayer();
        player.AddArmor(5);
        _combat.StartCombat(player, new[] { CreateEnemy() });
        var card = CreateCard(1, new CardEffect(EffectType.Damage, 3));

        _combat.PlayCard(card, player);

        Assert.Equal(2, player.Armor);
        Assert.Equal(50, player.CurrentHealth);
    }

    [Fact]
    public void PlayCard_DestroyArmor_RemovesArmor()
    {
        var enemy = CreateEnemy();
        enemy.AddArmor(8);
        _combat.StartCombat(CreatePlayer(), new[] { enemy });
        var card = CreateCard(1, new CardEffect(EffectType.DestroyArmor, 5));

        _combat.PlayCard(card, enemy);

        Assert.Equal(3, enemy.Armor);
    }

    [Fact]
    public void EndPlayerTurn_SwitchesToEnemyTurn()
    {
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });

        _combat.EndPlayerTurn();

        Assert.True(_combat.IsPlayerTurn); // enemy turn processes immediately and switches back
        Assert.Equal(2, _combat.CurrentTurn);
    }

    [Fact]
    public void ProcessEnemyTurn_EnemyDamagesPlayer()
    {
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });

        _combat.EndPlayerTurn();

        Assert.True(player.CurrentHealth < 50);
    }

    [Fact]
    public void ProcessEnemyTurn_EnemyDeathEndsCombat()
    {
        var player = CreatePlayer();
        var enemy = CreateEnemy(5);
        _combat.StartCombat(player, new[] { enemy });
        var card = CreateCard(1, new CardEffect(EffectType.Damage, 10));
        _combat.PlayCard(card, enemy);
        _combat.EndPlayerTurn(); // triggers ProcessEnemyTurn which ends combat

        Assert.True(_combat.IsCombatOver());
        Assert.False(_combat.CombatActive);
    }

    [Fact]
    public void ProcessEnemyTurn_PlayerDeathEndsCombat()
    {
        var player = CreatePlayer(1);
        _combat.StartCombat(player, new[] { CreateEnemy() });

        _combat.EndPlayerTurn();

        Assert.True(player.IsDead);
        Assert.True(_combat.IsCombatOver());
    }

    [Fact]
    public void GetAliveEnemies_ExcludesDead()
    {
        var enemy1 = CreateEnemy(10);
        var enemy2 = CreateEnemy(10);
        _combat.StartCombat(CreatePlayer(), new[] { enemy1, enemy2 });
        var card = CreateCard(1, new CardEffect(EffectType.Damage, 15));
        _combat.PlayCard(card, enemy1);

        var alive = _combat.GetAliveEnemies();

        Assert.Single(alive);
        Assert.Equal(enemy2, alive[0]);
    }

    [Fact]
    public void Energy_ResetsOnNewTurn()
    {
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });
        var card = CreateCard(2, new CardEffect(EffectType.Damage, 5));
        _combat.PlayCard(card);
        Assert.Equal(1, _combat.CurrentEnergy);

        _combat.EndPlayerTurn();

        Assert.Equal(_combat.MaxEnergy, _combat.CurrentEnergy);
    }

    [Fact]
    public void Event_OnCombatStarted_Fires()
    {
        bool fired = false;
        _combat.OnCombatStarted += () => fired = true;

        _combat.StartCombat(CreatePlayer(), new[] { CreateEnemy() });

        Assert.True(fired);
    }

    [Fact]
    public void Event_OnCombatEnded_Fires()
    {
        bool fired = false;
        var player = CreatePlayer();
        var enemy = CreateEnemy(1);
        _combat.StartCombat(player, new[] { enemy });
        _combat.OnCombatEnded += () => fired = true;

        var card = CreateCard(1, new CardEffect(EffectType.Damage, 5));
        _combat.PlayCard(card, enemy);
        _combat.EndPlayerTurn(); // triggers ProcessEnemyTurn which checks IsCombatOver

        Assert.True(fired);
    }

    [Fact]
    public void Event_OnDamageTaken_Fires()
    {
        int damageReceived = 0;
        var enemy = CreateEnemy();
        _combat.StartCombat(CreatePlayer(), new[] { enemy });
        _combat.OnDamageTaken += (_, dmg) => damageReceived = dmg;

        var card = CreateCard(1, new CardEffect(EffectType.Damage, 7));
        _combat.PlayCard(card, enemy);

        Assert.Equal(7, damageReceived);
    }

    [Fact]
    public void Event_OnEntityDied_Fires()
    {
        CombatEntity? diedEntity = null;
        var enemy = CreateEnemy(1);
        _combat.StartCombat(CreatePlayer(), new[] { enemy });
        _combat.OnEntityDied += e => diedEntity = e;

        var card = CreateCard(1, new CardEffect(EffectType.Damage, 5));
        _combat.PlayCard(card, enemy);

        Assert.NotNull(diedEntity);
        Assert.Equal(enemy, diedEntity);
    }

    [Fact]
    public void Event_OnPlayerTurnStarted_Fires()
    {
        int count = 0;
        _combat.StartCombat(CreatePlayer(), new[] { CreateEnemy() });
        _combat.OnPlayerTurnStarted += () => count++;

        _combat.EndPlayerTurn();

        Assert.Equal(1, count); // only from ProcessEnemyTurn; StartCombat event missed because subscribed after
    }

    [Fact]
    public void GetRewardGold_ReturnsExpected()
    {
        _combat.StartCombat(CreatePlayer(), new[] { CreateEnemy(), CreateEnemy() });

        var gold = _combat.GetRewardGold();

        Assert.Equal(22, gold); // 2 enemies * 10 + turn 1 * 2
    }
}
