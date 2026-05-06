#nullable enable
using System;

namespace ArcaneDecks.Core.Services;

public interface ISteamService
{
    bool IsInitialized { get; }

    void Initialize();
    void Shutdown();

    void SetAchievement(string achievementId);
    void StoreStats();
    void ResetAllStats(bool achievementsToo);

    void UploadLeaderboardScore(string leaderboardId, int score);

    void SetStat(string statName, int value);
    int GetStat(string statName);
}
