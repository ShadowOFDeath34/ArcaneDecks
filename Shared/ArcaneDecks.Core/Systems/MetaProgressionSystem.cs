using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ArcaneDecks.Core.Systems;

public class MetaUpgrade
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public string DescriptionKey { get; init; } = "";
    public int MaxLevel { get; init; }
    public int BaseCost { get; init; }
    public int CostIncreasePerLevel { get; init; }
    public int EffectPerLevel { get; init; }
}

public class MetaProgressionData
{
    public int GoblinTeeth { get; set; }
    public Dictionary<string, int> UpgradeLevels { get; set; } = new();
}

public class MetaProgressionSystem
{
    public MetaProgressionData Data { get; } = new();

    public static readonly List<MetaUpgrade> Upgrades = new()
    {
        new MetaUpgrade
        {
            Id = "max_health",
            NameKey = "ui.meta.max_health_name",
            DescriptionKey = "ui.meta.max_health_desc",
            MaxLevel = 5,
            BaseCost = 10,
            CostIncreasePerLevel = 10,
            EffectPerLevel = 5
        },
        new MetaUpgrade
        {
            Id = "start_gold",
            NameKey = "ui.meta.start_gold_name",
            DescriptionKey = "ui.meta.start_gold_desc",
            MaxLevel = 5,
            BaseCost = 10,
            CostIncreasePerLevel = 10,
            EffectPerLevel = 10
        }
    };

    public int GetCost(string upgradeId)
    {
        var level = Data.UpgradeLevels.GetValueOrDefault(upgradeId, 0);
        var upgrade = Upgrades.First(u => u.Id == upgradeId);
        return upgrade.BaseCost + level * upgrade.CostIncreasePerLevel;
    }

    public int GetCurrentLevel(string upgradeId)
    {
        return Data.UpgradeLevels.GetValueOrDefault(upgradeId, 0);
    }

    public bool CanUpgrade(string upgradeId)
    {
        var level = GetCurrentLevel(upgradeId);
        var upgrade = Upgrades.First(u => u.Id == upgradeId);
        if (level >= upgrade.MaxLevel) return false;
        return Data.GoblinTeeth >= GetCost(upgradeId);
    }

    public bool PurchaseUpgrade(string upgradeId)
    {
        if (!CanUpgrade(upgradeId)) return false;
        Data.GoblinTeeth -= GetCost(upgradeId);
        Data.UpgradeLevels[upgradeId] = GetCurrentLevel(upgradeId) + 1;
        return true;
    }

    public int GetStartingMaxHealthBonus()
    {
        var level = GetCurrentLevel("max_health");
        var upgrade = Upgrades.First(u => u.Id == "max_health");
        return level * upgrade.EffectPerLevel;
    }

    public int GetStartingGoldBonus()
    {
        var level = GetCurrentLevel("start_gold");
        var upgrade = Upgrades.First(u => u.Id == "start_gold");
        return level * upgrade.EffectPerLevel;
    }

    public int CalculateTeethEarned(RunState run)
    {
        return Math.Max(0, run.CurrentFloor + run.Score / 200);
    }

    public void ApplyToRun(RunState run)
    {
        run.PlayerMaxHealth = 55 + GetStartingMaxHealthBonus();
        run.PlayerCurrentHealth = run.PlayerMaxHealth;
        run.Gold = GetStartingGoldBonus();
    }

    public void LoadFromDto(Dictionary<string, object>? dto)
    {
        if (dto == null) return;

        if (dto.TryGetValue("goblinTeeth", out var gt))
        {
            Data.GoblinTeeth = gt switch
            {
                JsonElement je => je.GetInt32(),
                int i => i,
                _ => 0
            };
        }

        if (dto.TryGetValue("upgradeLevels", out var ul) && ul is JsonElement je2)
        {
            Data.UpgradeLevels = new Dictionary<string, int>();
            foreach (var prop in je2.EnumerateObject())
            {
                Data.UpgradeLevels[prop.Name] = prop.Value.GetInt32();
            }
        }
        else if (dto.TryGetValue("upgradeLevels", out var ul2) && ul2 is Dictionary<string, int> dict)
        {
            Data.UpgradeLevels = new Dictionary<string, int>(dict);
        }
    }

    public Dictionary<string, object> ToDto()
    {
        return new Dictionary<string, object>
        {
            ["goblinTeeth"] = Data.GoblinTeeth,
            ["upgradeLevels"] = Data.UpgradeLevels
        };
    }
}
