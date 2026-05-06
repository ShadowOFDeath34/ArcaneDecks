#nullable enable
using System;
using ArcaneDecks.Core.Services;
using Steamworks;

namespace ArcaneDecks.DesktopGL;

public class SteamworksService : ISteamService
{
    public bool IsInitialized { get; private set; }

    private CallResult<LeaderboardFindResult_t> _leaderboardFindResult;
    private CallResult<LeaderboardScoreUploaded_t> _scoreUploadedResult;

    public SteamworksService()
    {
        _leaderboardFindResult = new CallResult<LeaderboardFindResult_t>();
        _scoreUploadedResult = new CallResult<LeaderboardScoreUploaded_t>();
    }

    public void Initialize()
    {
        try
        {
            IsInitialized = SteamAPI.Init();
            if (IsInitialized)
            {
                SteamUserStats.RequestCurrentStats();
            }
        }
        catch
        {
            IsInitialized = false;
        }
    }

    public void Shutdown()
    {
        if (IsInitialized)
        {
            SteamAPI.Shutdown();
            IsInitialized = false;
        }
    }

    public void SetAchievement(string achievementId)
    {
        if (!IsInitialized) return;
        SteamUserStats.SetAchievement(achievementId);
    }

    public void StoreStats()
    {
        if (!IsInitialized) return;
        SteamUserStats.StoreStats();
    }

    public void ResetAllStats(bool achievementsToo)
    {
        if (!IsInitialized) return;
        SteamUserStats.ResetAllStats(achievementsToo);
    }

    public void SetStat(string statName, int value)
    {
        if (!IsInitialized) return;
        SteamUserStats.SetStat(statName, value);
    }

    public int GetStat(string statName)
    {
        if (!IsInitialized) return 0;
        SteamUserStats.GetStat(statName, out int value);
        return value;
    }

    public void UploadLeaderboardScore(string leaderboardId, int score)
    {
        if (!IsInitialized) return;

        SteamAPICall_t hSteamAPICall = SteamUserStats.FindLeaderboard(leaderboardId);
        _leaderboardFindResult.Set(hSteamAPICall, (result, bIOFailure) =>
        {
            if (bIOFailure || result.m_bLeaderboardFound == 0) return;
            SteamAPICall_t uploadCall = SteamUserStats.UploadLeaderboardScore(
                result.m_hSteamLeaderboard,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score,
                null,
                0
            );
            _scoreUploadedResult.Set(uploadCall, (_, uploadFailure) =>
            {
                if (uploadFailure) return;
            });
        });
    }
}
