#nullable enable
using System;
using ArcaneDecks.Core.Services;

namespace ArcaneDecks.iOS;

public class iOSSteamService : ISteamService
{
    public bool IsInitialized => false;

    public void Initialize() { }
    public void Shutdown() { }
    public void SetAchievement(string achievementId) { }
    public void StoreStats() { }
    public void ResetAllStats(bool achievementsToo) { }
    public void UploadLeaderboardScore(string leaderboardId, int score) { }
    public void SetStat(string statName, int value) { }
    public int GetStat(string statName) => 0;
}
