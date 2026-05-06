using System.Collections.Generic;

namespace ArcaneDecks.Core.Services;

public interface IAnalyticsService
{
    void TrackEvent(string eventName, Dictionary<string, object>? properties = null);
    void Identify(string userId, Dictionary<string, object>? properties = null);
}
