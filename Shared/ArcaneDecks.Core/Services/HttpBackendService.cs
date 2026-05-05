using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ArcaneDecks.Core.Systems;

namespace ArcaneDecks.Core.Services;

public class HttpBackendService : IBackendService
{
    private readonly HttpClient _http;
    private readonly IAuthService _auth;
    private readonly string _apiBaseUrl;

    public HttpBackendService(HttpClient http, IAuthService auth, string apiBaseUrl)
    {
        _http = http;
        _auth = auth;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
    }

    public async Task<bool> SaveProgressAsync(RunState state)
    {
        if (!_auth.IsAuthenticated) return false;

        var dto = new ProgressDto
        {
            Gold = state.Gold,
            HighestFloor = state.CurrentFloor,
            CardsUnlocked = state.DeckCardIds,
        };

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/api/v1/progress")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);

        try
        {
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<RunState?> LoadProgressAsync()
    {
        if (!_auth.IsAuthenticated) return null;

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/v1/progress");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProgressResponseDto>(json);
            if (result?.Data == null) return null;

            var state = new RunState
            {
                IsActive = true,
                Gold = result.Data.Gold,
                CurrentFloor = result.Data.HighestFloor,
                DeckCardIds = new List<string>(result.Data.CardsUnlocked),
                DrawPile = new List<string>(result.Data.CardsUnlocked),
                DiscardPile = new List<string>(),
                PlayerCurrentHealth = 50,
                PlayerMaxHealth = 50,
            };
            return state;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SubmitScoreAsync(string leaderboardId, string playerName, int score, int floor)
    {
        if (!_auth.IsAuthenticated) return false;

        var dto = new LeaderboardSubmitDto
        {
            PlayerId = _auth.PlayerId,
            PlayerName = playerName,
            Score = score,
            Floor = floor,
        };

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/api/v1/leaderboards/{leaderboardId}")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);

        try
        {
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string leaderboardId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/v1/leaderboards/{leaderboardId}");
        if (_auth.IsAuthenticated)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<LeaderboardEntryDto>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LeaderboardResponseDto>(json);
            return result?.Data ?? new List<LeaderboardEntryDto>();
        }
        catch
        {
            return new List<LeaderboardEntryDto>();
        }
    }
}
