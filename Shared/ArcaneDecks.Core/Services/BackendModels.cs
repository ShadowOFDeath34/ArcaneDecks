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
