using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ArcaneDecks.Core.Services;

public class AuthResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
}

public class ProgressDto
{
    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    [JsonPropertyName("highestFloor")]
    public int HighestFloor { get; set; }

    [JsonPropertyName("cardsUnlocked")]
    public List<string> CardsUnlocked { get; set; } = new();

    [JsonPropertyName("metaUpgrades")]
    public Dictionary<string, object> MetaUpgrades { get; set; } = new();
}

public class ProgressDataDto
{
    [JsonPropertyName("gold")]
    public int Gold { get; set; }

    [JsonPropertyName("highest_floor")]
    public int HighestFloor { get; set; }

    [JsonPropertyName("cards_unlocked")]
    public List<string> CardsUnlocked { get; set; } = new();

    [JsonPropertyName("meta_upgrades")]
    public Dictionary<string, object> MetaUpgrades { get; set; } = new();
}

public class ProgressResponseDto
{
    [JsonPropertyName("data")]
    public ProgressDataDto? Data { get; set; }
}

public class MetaProgressDto
{
    [JsonPropertyName("meta_progress")]
    public Dictionary<string, object> MetaProgress { get; set; } = new();
}

public class LeaderboardSubmitDto
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    [JsonPropertyName("playerName")]
    public string PlayerName { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("floor")]
    public int Floor { get; set; }
}

public class LeaderboardEntryDto
{
    [JsonPropertyName("player_name")]
    public string PlayerName { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("floor")]
    public int Floor { get; set; }
}

public class LeaderboardResponseDto
{
    [JsonPropertyName("data")]
    public List<LeaderboardEntryDto> Data { get; set; } = new();
}

public class SeasonalEventDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("event_key")]
    public string EventKey { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("start_at")]
    public string StartAt { get; set; } = string.Empty;

    [JsonPropertyName("end_at")]
    public string EndAt { get; set; } = string.Empty;

    [JsonPropertyName("rules_json")]
    public Dictionary<string, object> Rules { get; set; } = new();

    [JsonPropertyName("reward_teeth")]
    public int RewardTeeth { get; set; }

    [JsonPropertyName("reward_card_id")]
    public string? RewardCardId { get; set; }
}

public class SeasonalEventListDto
{
    [JsonPropertyName("events")]
    public List<SeasonalEventDto> Events { get; set; } = new();
}

public class SeasonalEventSubmitResultDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("bestScore")]
    public int BestScore { get; set; }

    [JsonPropertyName("bestFloor")]
    public int BestFloor { get; set; }

    [JsonPropertyName("runsCompleted")]
    public int RunsCompleted { get; set; }
}

public class SeasonalEventClaimResultDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("rewardTeeth")]
    public int RewardTeeth { get; set; }

    [JsonPropertyName("rewardCardId")]
    public string? RewardCardId { get; set; }
}
