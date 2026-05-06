#nullable enable
using System;
using Google.Android.Gms.Ads;
using Google.Android.Gms.Ads.Interstitial;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using ArcaneDecks.Core.Services;
using Microsoft.Xna.Framework;

namespace ArcaneDecks.Android;

public class AndroidAdService : IAdService
{
    private AdView? _bannerView;
    private InterstitialAd? _interstitialAd;
    private bool _isInterstitialLoading;

    private const string TestBannerId = "ca-app-pub-3940256099942544/6300978111";
    private const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";

    public void Initialize()
    {
        var activity = Game.Activity;
        activity?.RunOnUiThread(() =>
        {
            MobileAds.Initialize(activity);
        });
    }

    public void ShowBanner(AdPosition position)
    {
        var activity = Game.Activity;
        if (activity == null) return;

        activity.RunOnUiThread(() =>
        {
            HideBannerInternal();

            _bannerView = new AdView(activity)
            {
                AdUnitId = TestBannerId,
                AdSize = AdSize.Banner
            };

            var layoutParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent)
            {
                Gravity = position == AdPosition.Top
                    ? GravityFlags.Top | GravityFlags.CenterHorizontal
                    : GravityFlags.Bottom | GravityFlags.CenterHorizontal
            };

            var contentView = activity.Window?.DecorView?.FindViewById<FrameLayout>(global::Android.Resource.Id.Content);
            contentView?.AddView(_bannerView, layoutParams);

            var adRequest = new AdRequest.Builder().Build();
            _bannerView.LoadAd(adRequest);
        });
    }

    public void HideBanner()
    {
        var activity = Game.Activity;
        activity?.RunOnUiThread(HideBannerInternal);
    }

    private void HideBannerInternal()
    {
        if (_bannerView == null) return;

        var parent = _bannerView.Parent as ViewGroup;
        parent?.RemoveView(_bannerView);
        _bannerView.Destroy();
        _bannerView = null;
    }

    public void LoadInterstitial()
    {
        if (_isInterstitialLoading) return;

        var activity = Game.Activity;
        if (activity == null) return;

        _isInterstitialLoading = true;
        var adRequest = new AdRequest.Builder().Build();

        InterstitialAd.Load(activity, TestInterstitialId, adRequest, new InterstitialLoadListener
        {
            OnLoaded = ad =>
            {
                _interstitialAd = ad;
                _isInterstitialLoading = false;
            },
            OnFailed = _ =>
            {
                _isInterstitialLoading = false;
            }
        });
    }

    public void ShowInterstitial(Action? onClosed = null)
    {
        var activity = Game.Activity;
        if (activity == null) return;

        activity.RunOnUiThread(() =>
        {
            if (_interstitialAd == null)
            {
                LoadInterstitial();
                onClosed?.Invoke();
                return;
            }

            _interstitialAd.FullScreenContentCallback = new ScreenCallback
            {
                OnDismissed = () =>
                {
                    onClosed?.Invoke();
                    _interstitialAd = null;
                }
            };

            _interstitialAd.Show(activity);
        });
    }

    public bool IsInterstitialReady => _interstitialAd != null && !_isInterstitialLoading;

    private abstract class InterstitialLoadCallbackBase : InterstitialAdLoadCallback
    {
        [Register("onAdLoaded", "(Lcom/google/android/gms/ads/interstitial/InterstitialAd;)V", "GetOnAdLoadedHandler")]
        public virtual void OnAdLoaded(InterstitialAd ad) { }

        private static Delegate? cb_onAdLoaded;

        private static Delegate GetOnAdLoadedHandler()
        {
            if (cb_onAdLoaded == null)
                cb_onAdLoaded = JNINativeWrapper.CreateDelegate((Action<IntPtr, IntPtr, IntPtr>)n_onAdLoaded);
            return cb_onAdLoaded;
        }

        private static void n_onAdLoaded(IntPtr jnienv, IntPtr native__this, IntPtr native_p0)
        {
            var thisobject = Java.Lang.Object.GetObject<InterstitialLoadCallbackBase>(native__this, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;
            var resultobject = Java.Lang.Object.GetObject<InterstitialAd>(native_p0, global::Android.Runtime.JniHandleOwnership.DoNotTransfer);
            thisobject.OnAdLoaded(resultobject!);
        }
    }

    private class InterstitialLoadListener : InterstitialLoadCallbackBase
    {
        public Action<InterstitialAd>? OnLoaded;
        public Action<LoadAdError>? OnFailed;

        public override void OnAdLoaded(InterstitialAd ad)
        {
            OnLoaded?.Invoke(ad);
        }

        public override void OnAdFailedToLoad(LoadAdError loadAdError)
        {
            OnFailed?.Invoke(loadAdError);
        }
    }

    private class ScreenCallback : FullScreenContentCallback
    {
        public Action? OnDismissed;

        public override void OnAdDismissedFullScreenContent()
        {
            OnDismissed?.Invoke();
        }
    }
}
