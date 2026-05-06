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

        Assert.Equal(32, gold); // 2 enemies * 12 + turn 1 * 3 + 5
    }

    [Fact]
    public void PlayCard_WhenCombatNotActive_ReturnsFalse()
    {
        var player = CreatePlayer();
        var enemy = CreateEnemy(1);
        _combat.StartCombat(player, new[] { enemy });
        var kill = CreateCard(1, new CardEffect(EffectType.Damage, 5));
        _combat.PlayCard(kill, enemy); // kills enemy
        _combat.EndPlayerTurn(); // ProcessEnemyTurn detects combat over and sets CombatActive=false

        var result = _combat.PlayCard(CreateCard(1, new CardEffect(EffectType.Damage, 1)));

        Assert.False(result);
    }

    [Fact]
    public void PlayCard_WithoutTarget_HitsFirstAliveEnemy()
    {
        var player = CreatePlayer();
        var enemy1 = CreateEnemy(10);
        var enemy2 = CreateEnemy(10);
        _combat.StartCombat(player, new[] { enemy1, enemy2 });
        var card = CreateCard(1, new CardEffect(EffectType.Damage, 3));

        _combat.PlayCard(card); // no target

        Assert.Equal(7, enemy1.CurrentHealth);
        Assert.Equal(10, enemy2.CurrentHealth);
    }

    [Fact]
    public void PlayCard_MultipleEffects_ExecutesAll()
    {
        var player = CreatePlayer();
        player.CurrentHealth = 40;
        var enemy = CreateEnemy();
        _combat.StartCombat(player, new[] { enemy });
        var card = CreateCard(1,
            new CardEffect(EffectType.Damage, 4),
            new CardEffect(EffectType.Block, 3),
            new CardEffect(EffectType.Heal, 5));

        _combat.PlayCard(card, enemy);

        Assert.Equal(26, enemy.CurrentHealth);
        Assert.Equal(3, player.Armor);
        Assert.Equal(45, player.CurrentHealth);
    }

    [Fact]
    public void PlayCard_Block_Stacks()
    {
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });
        var card = CreateCard(1, new CardEffect(EffectType.Block, 3));

        _combat.PlayCard(card);
        _combat.PlayCard(card);

        Assert.Equal(6, player.Armor);
    }

    [Fact]
    public void PlayCard_DestroyArmor_AlreadyZero_StaysZero()
    {
        var enemy = CreateEnemy();
        _combat.StartCombat(CreatePlayer(), new[] { enemy });
        var card = CreateCard(1, new CardEffect(EffectType.DestroyArmor, 5));

        _combat.PlayCard(card, enemy);

        Assert.Equal(0, enemy.Armor);
    }

    [Fact]
    public void PlayCard_DestroyArmor_NullTarget_DoesNothing()
    {
        var enemy = CreateEnemy();
        enemy.AddArmor(5);
        _combat.StartCombat(CreatePlayer(), new[] { enemy });
        var card = CreateCard(1, new CardEffect(EffectType.DestroyArmor, 3));

        _combat.PlayCard(card); // no target

        Assert.Equal(5, enemy.Armor);
    }

    [Fact]
    public void ProcessEnemyTurn_DamageScalesWithTurn()
    {
        var player = CreatePlayer(100);
        var enemy = CreateEnemy();
        enemy.Damage = 5;
        _combat.StartCombat(player, new[] { enemy });

        // Turn 1: damage = 5 + (1/3) = 5
        _combat.EndPlayerTurn();
        var healthAfterTurn1 = player.CurrentHealth;

        // Turn 2: damage = 5 + (2/3) = 5
        _combat.EndPlayerTurn();
        var healthAfterTurn2 = player.CurrentHealth;

        // Turn 3: damage = 5 + (3/3) = 6
        _combat.EndPlayerTurn();
        var healthAfterTurn3 = player.CurrentHealth;

        Assert.Equal(95, healthAfterTurn1);
        Assert.Equal(90, healthAfterTurn2);
        Assert.Equal(84, healthAfterTurn3);
    }

    [Fact]
    public void Event_OnHealed_Fires()
    {
        int healedAmount = 0;
        var player = CreatePlayer(50);
        player.CurrentHealth = 40;
        _combat.StartCombat(player, new[] { CreateEnemy() });
        _combat.OnHealed += (_, amt) => healedAmount = amt;

        _combat.PlayCard(CreateCard(1, new CardEffect(EffectType.Heal, 6)));

        Assert.Equal(6, healedAmount);
    }

    [Fact]
    public void Event_OnArmorGained_Fires()
    {
        int armorAmount = 0;
        var player = CreatePlayer();
        _combat.StartCombat(player, new[] { CreateEnemy() });
        _combat.OnArmorGained += (_, amt) => armorAmount = amt;

        _combat.PlayCard(CreateCard(1, new CardEffect(EffectType.Block, 4)));

        Assert.Equal(4, armorAmount);
    }

    [Fact]
    public void Event_OnEnemyTurnStarted_Fires()
    {
        bool fired = false;
        _combat.StartCombat(CreatePlayer(), new[] { CreateEnemy() });
        _combat.OnEnemyTurnStarted += () => fired = true;

        _combat.EndPlayerTurn();

        Assert.True(fired);
    }

    [Fact]
    public void Event_OnTurnEnded_FiresTwicePerCycle()
    {
        int count = 0;
        _combat.StartCombat(CreatePlayer(), new[] { CreateEnemy() });
        _combat.OnTurnEnded += () => count++;

        _combat.EndPlayerTurn();

        Assert.Equal(2, count); // once in EndPlayerTurn, once in ProcessEnemyTurn
    }

    [Fact]
    public void IsCombatOver_PlayerNull_ReturnsTrue()
    {
        var freshCombat = new CombatSystem();
        Assert.True(freshCombat.IsCombatOver());
    }

    [Fact]
    public void GetRewardGold_ScalesWithTurn()
    {
        _combat.StartCombat(CreatePlayer(), new[] { CreateEnemy() });

        var goldTurn1 = _combat.GetRewardGold(); // 1*12 + 1*3 + 5 = 20
        _combat.EndPlayerTurn(); // turn becomes 2
        var goldTurn2 = _combat.GetRewardGold(); // 1*12 + 2*3 + 5 = 23

        Assert.Equal(20, goldTurn1);
        Assert.Equal(23, goldTurn2);
    }

    [Fact]
    public void ApplyDamage_Overkill_ClampedToZero()
    {
        var entity = new CombatEntity { MaxHealth = 10, CurrentHealth = 10, Armor = 2 };

        entity.ApplyDamage(20);

        Assert.Equal(0, entity.CurrentHealth);
        Assert.Equal(0, entity.Armor);
    }

    [Fact]
    public void ApplyDamage_ArmorExceedsDamage_NoHealthLost()
    {
        var entity = new CombatEntity { MaxHealth = 10, CurrentHealth = 10, Armor = 5 };

        entity.ApplyDamage(3);

        Assert.Equal(10, entity.CurrentHealth);
        Assert.Equal(2, entity.Armor);
    }

    [Fact]
    public void ApplyHeal_RespectsMaxHealth()
    {
        var entity = new CombatEntity { MaxHealth = 20, CurrentHealth = 18 };
        entity.ApplyHeal(5);
        Assert.Equal(20, entity.CurrentHealth);
    }

    [Fact]
    public void AddArmor_NegativeInput_ClampedToZero()
    {
        var entity = new CombatEntity { Armor = 5 };
        entity.AddArmor(-10);

        Assert.Equal(0, entity.Armor);
    }

    [Fact]
    public void DestroyArmor_ExcessAmount_ClampedToZero()
    {
        var entity = new CombatEntity { Armor = 3 };
        entity.DestroyArmor(10);

        Assert.Equal(0, entity.Armor);
    }

    [Fact]
    public void CombatEntity_TickStatusEffects_RemovesExpired()
    {
        var entity = new CombatEntity { MaxHealth = 20, CurrentHealth = 20 };
        var poison = new TestPoisonEffect { Duration = 2, DamagePerTick = 2 };
        entity.StatusEffects.Add(poison);

        entity.TickStatusEffects(); // tick 1
        Assert.Equal(18, entity.CurrentHealth);
        Assert.Single(entity.StatusEffects);

        entity.TickStatusEffects(); // tick 2
        Assert.Equal(16, entity.CurrentHealth);
        Assert.Empty(entity.StatusEffects);
    }

    private class TestPoisonEffect : StatusEffect
    {
        public int DamagePerTick { get; init; } = 2;
        public override void Tick(CombatEntity target)
        {
            target.ApplyDamage(DamagePerTick);
            Duration--;
        }
    }
}
