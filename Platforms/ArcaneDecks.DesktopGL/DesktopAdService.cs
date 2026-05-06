#nullable enable
using System;
using ArcaneDecks.Core.Services;

namespace ArcaneDecks.DesktopGL;

public class DesktopAdService : IAdService
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
