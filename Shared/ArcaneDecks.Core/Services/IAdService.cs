#nullable enable
using System;

namespace ArcaneDecks.Core.Services;

public enum AdPosition
{
    Top,
    Bottom
}

public interface IAdService
{
    void Initialize();
    void ShowBanner(AdPosition position);
    void HideBanner();
    void LoadInterstitial();
    void ShowInterstitial(Action? onClosed = null);
    bool IsInterstitialReady { get; }
}
