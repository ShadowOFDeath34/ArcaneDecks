using System.Collections.Generic;
using System.Threading.Tasks;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Services;

public interface IBackendService
{
    Task<bool> SaveProgressAsync(RunState state);
    Task<RunState?> LoadProgressAsync();
    Task<bool> SubmitScoreAsync(string leaderboardId, string playerName, int score, int floor);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string leaderboardId);
}
