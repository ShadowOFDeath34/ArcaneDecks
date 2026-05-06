using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PostHog;

namespace ArcaneDecks.Core.Services;

public class PostHogAnalyticsService : IAnalyticsService
{
    private readonly PostHogClient? _client;
    private readonly string _distinctId;

    public PostHogAnalyticsService(string apiKey, string distinctId, string? hostUrl = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            _client = null;
        }
        else
        {
            _client = new PostHogClient(new PostHogOptions
            {
                ProjectApiKey = apiKey,
                HostUrl = new Uri(hostUrl ?? "https://us.i.posthog.com")
            });
        }

        _distinctId = distinctId;
    }

    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        if (_client == null) return;

        _ = Task.Run(() =>
        {
            try
            {
                _client.Capture(_distinctId, eventName, properties);
            }
            catch
            {
                // Analytics failures must not crash the game.
            }
        });
    }

    public void Identify(string userId, Dictionary<string, object>? properties = null)
    {
        if (_client == null) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await _client.IdentifyAsync(_distinctId, properties, null, default);
            }
            catch
            {
                // Analytics failures must not crash the game.
            }
        });
    }
}
