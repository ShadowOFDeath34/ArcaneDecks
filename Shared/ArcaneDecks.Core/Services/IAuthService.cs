using System.Threading.Tasks;

namespace ArcaneDecks.Core.Services;

public interface IAuthService
{
    string? Token { get; }
    string PlayerId { get; }
    string DeviceId { get; }
    bool IsAuthenticated { get; }
    Task<bool> AuthenticateAsync();
    void LoadLocalCredentials(string storagePath);
    void SaveLocalCredentials(string storagePath);
    void EnsureDeviceId(string storagePath);
}
