using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneDecks.Core.Systems;

public class CombatEntity
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string NameKey { get; init; } = "entity.unknown";
    public int MaxHealth { get; set; } = 50;
    public int CurrentHealth { get; set; } = 50;
    public int Armor { get; set; } = 0;
    public int Damage { get; set; } = 7;
    public List<StatusEffect> StatusEffects { get; } = new();
    public bool IsPlayer { get; init; } = false;
    public bool IsDead => CurrentHealth <= 0;

    public void AddArmor(int amount)
    {
        Armor += amount;
        if (Armor < 0) Armor = 0;
    }

    public void DestroyArmor(int amount)
    {
        Armor -= amount;
        if (Armor < 0) Armor = 0;
    }

    public void ApplyDamage(int amount)
    {
        int remaining = amount;
        if (Armor > 0)
        {
            int absorbed = Math.Min(Armor, remaining);
            Armor -= absorbed;
            remaining -= absorbed;
        }
        CurrentHealth -= remaining;
        if (CurrentHealth < 0) CurrentHealth = 0;
    }

    public void ApplyHeal(int amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
    }

    public void TickStatusEffects()
    {
        foreach (var se in StatusEffects.ToList())
        {
            se.Tick(this);
            if (se.Duration <= 0)
            {
                StatusEffects.Remove(se);
            }
        }
    }
}

public abstract class StatusEffect
{
    public string NameKey { get; init; } = "status.unknown";
    public int Duration { get; set; } = 1;
    public abstract void Tick(CombatEntity target);
}

public class CombatSystem
{
    public CombatEntity? Player { get; private set; }
    public List<CombatEntity> Enemies { get; } = new();
    public int CurrentTurn { get; private set; } = 0;
    public bool IsPlayerTurn { get; private set; } = true;
    public bool CombatActive { get; private set; } = false;
    public int MaxEnergy { get; set; } = 3;
    public int CurrentEnergy { get; private set; } = 3;

    public event Action? OnCombatStarted;
    public event Action? OnCombatEnded;
    public event Action<CombatEntity, int>? OnDamageTaken;
    public event Action<CombatEntity, int>? OnHealed;
    public event Action<CombatEntity, int>? OnArmorGained;
    public event Action<CombatEntity>? OnEntityDied;
    public event Action? OnPlayerTurnStarted;
    public event Action? OnEnemyTurnStarted;
    public event Action? OnTurnEnded;

    public void StartCombat(CombatEntity player, IEnumerable<CombatEntity> enemies)
    {
        Player = player;
        Enemies.Clear();
        Enemies.AddRange(enemies);
        CurrentTurn = 1;
        IsPlayerTurn = true;
        CombatActive = true;
        CurrentEnergy = MaxEnergy;
        OnCombatStarted?.Invoke();
        OnPlayerTurnStarted?.Invoke();
    }

    public bool PlayCard(CardDefinition card, CombatEntity? target = null)
    {
        if (!IsPlayerTurn || !CombatActive || Player == null) return false;
        if (card.Cost > CurrentEnergy) return false;

        CurrentEnergy -= card.Cost;

        foreach (var effect in card.Effects)
        {
            switch (effect.Type)
            {
                case EffectType.Damage:
                    if (target != null)
                    {
                        target.ApplyDamage(effect.Value);
                        OnDamageTaken?.Invoke(target, effect.Value);
                        if (target.IsDead) EntityDied(target);
                    }
                    else
                    {
                        var alive = GetAliveEnemies();
                        if (alive.Count > 0)
                        {
                            var first = alive[0];
                            first.ApplyDamage(effect.Value);
                            OnDamageTaken?.Invoke(first, effect.Value);
                            if (first.IsDead) EntityDied(first);
                        }
                    }
                    break;

                case EffectType.Block:
                    Player.AddArmor(effect.Value);
                    OnArmorGained?.Invoke(Player, effect.Value);
                    break;

                case EffectType.Heal:
                    Player.ApplyHeal(effect.Value);
                    OnHealed?.Invoke(Player, effect.Value);
                    break;

                case EffectType.DestroyArmor:
                    if (target != null)
                    {
                        target.DestroyArmor(effect.Value);
                        OnDamageTaken?.Invoke(target, 0);
                    }
                    break;
            }
        }

        return true;
    }

    public void EndPlayerTurn()
    {
        if (!IsPlayerTurn || !CombatActive) return;
        IsPlayerTurn = false;
        OnTurnEnded?.Invoke();
        ProcessEnemyTurn();
    }

    public void ProcessEnemyTurn()
    {
        if (!CombatActive) return;
        OnEnemyTurnStarted?.Invoke();

        foreach (var enemy in GetAliveEnemies())
        {
            enemy.TickStatusEffects();
            if (enemy.IsDead)
            {
                EntityDied(enemy);
                continue;
            }

            if (Player != null && !Player.IsDead)
            {
                int damage = enemy.Damage + (CurrentTurn / 3);
                Player.ApplyDamage(damage);
                OnDamageTaken?.Invoke(Player, damage);
                if (Player.IsDead) EntityDied(Player);
            }
        }

        if (Player != null)
        {
            Player.TickStatusEffects();
            Player.Armor = 0;
        }

        CurrentTurn++;
        IsPlayerTurn = true;
        CurrentEnergy = MaxEnergy;
        OnTurnEnded?.Invoke();
        OnPlayerTurnStarted?.Invoke();

        if (IsCombatOver())
        {
            CombatActive = false;
            OnCombatEnded?.Invoke();
        }
    }

    public bool IsCombatOver()
    {
        if (Player == null || Player.IsDead) return true;
        return GetAliveEnemies().Count == 0;
    }

    public List<CombatEntity> GetAliveEnemies()
    {
        return Enemies.Where(e => !e.IsDead).ToList();
    }

    public int GetRewardGold()
    {
        return Enemies.Count * 12 + CurrentTurn * 3 + 5;
    }

    private void EntityDied(CombatEntity entity)
    {
        OnEntityDied?.Invoke(entity);
    }
}
