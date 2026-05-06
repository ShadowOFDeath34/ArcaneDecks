#nullable enable
using System;
using ArcaneDecks.Core.Services;

namespace ArcaneDecks.iOS;

public class iOSAdService : IAdService
{
    public void Initialize() { }
    public void ShowBanner(AdPosition position) { }
    public void HideBanner() { }
    public void LoadInterstitial() { }
    public void ShowInterstitial(Action? onClosed = null)
    {
        onClosed?.Invoke();
    }
    public bool IsInterstitialReady => false;
}
