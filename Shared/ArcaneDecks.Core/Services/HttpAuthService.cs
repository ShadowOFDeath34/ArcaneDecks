using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArcaneDecks.Core.Services;

public class HttpAuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly string _apiBaseUrl;
    private string? _token;
    private string _playerId = string.Empty;
    private string _deviceId = string.Empty;

    public string? Token => _token;
    public string PlayerId => _playerId;
    public string DeviceId => _deviceId;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public HttpAuthService(HttpClient http, string apiBaseUrl)
    {
        _http = http;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
    }

    public async Task<bool> AuthenticateAsync()
    {
        if (string.IsNullOrEmpty(_deviceId))
        {
            return false;
        }

        var payload = new { deviceId = _deviceId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync($"{_apiBaseUrl}/api/v1/auth/anonymous", content);
            if (!response.IsSuccessStatusCode) return false;

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthResponseDto>(responseJson);
            if (result == null || string.IsNullOrEmpty(result.Token)) return false;

            _token = result.Token;
            _playerId = result.PlayerId;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void LoadLocalCredentials(string storagePath)
    {
        var tokenPath = Path.Combine(storagePath, "auth_token.dat");
        var playerIdPath = Path.Combine(storagePath, "player_id.dat");
        var deviceIdPath = Path.Combine(storagePath, "device_id.dat");

        if (File.Exists(tokenPath))
            _token = File.ReadAllText(tokenPath);

        if (File.Exists(playerIdPath))
            _playerId = File.ReadAllText(playerIdPath);

        if (File.Exists(deviceIdPath))
        {
            _deviceId = File.ReadAllText(deviceIdPath);
        }
    }

    public void SaveLocalCredentials(string storagePath)
    {
        Directory.CreateDirectory(storagePath);
        var tokenPath = Path.Combine(storagePath, "auth_token.dat");
        var playerIdPath = Path.Combine(storagePath, "player_id.dat");
        var deviceIdPath = Path.Combine(storagePath, "device_id.dat");

        if (!string.IsNullOrEmpty(_token))
            File.WriteAllText(tokenPath, _token);

        if (!string.IsNullOrEmpty(_playerId))
            File.WriteAllText(playerIdPath, _playerId);

        if (!string.IsNullOrEmpty(_deviceId))
            File.WriteAllText(deviceIdPath, _deviceId);
    }

    public void EnsureDeviceId(string storagePath)
    {
        var deviceIdPath = Path.Combine(storagePath, "device_id.dat");
        if (File.Exists(deviceIdPath))
        {
            _deviceId = File.ReadAllText(deviceIdPath);
        }
        else
        {
            _deviceId = Guid.NewGuid().ToString();
            Directory.CreateDirectory(storagePath);
            File.WriteAllText(deviceIdPath, _deviceId);
        }
    }
}
