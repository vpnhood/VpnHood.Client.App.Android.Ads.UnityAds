using Android.Content;
using Com.Unity3d.Ads;
using VpnHood.Client.App.Abstractions;
using VpnHood.Client.Device;
using VpnHood.Client.Device.Droid;
using VpnHood.Common.Exceptions;
using VpnHood.Common.Utils;

namespace VpnHood.Client.App.Droid.Ads.VhUnityAds;

public class UnityAdService(string adGameId, string adPlacementId, bool testMode = false) : IAppAdService
{
    private static bool _isUnityAdInitialized;
    public string NetworkName => "UnityAds";
    public AppAdType AdType => AppAdType.InterstitialAd;
    public DateTime? AdLoadedTime { get; private set; }

    public static UnityAdService Create(string adGameId, string adPlacementId, bool testMode = false)
    {
        var ret = new UnityAdService(adGameId, adPlacementId, testMode);
        return ret;
    }

    public bool IsCountrySupported(string countryCode)
    {
        return countryCode != "IR";
    }

    private async Task EnsureUnityAdInitialized(Context context, CancellationToken cancellationToken)
    {
        if (_isUnityAdInitialized)
            return;

        var adInitializationListener = new MyAdInitializationListener();
        UnityAds.Initialize(context, adGameId, testMode, adInitializationListener);

        var cancellationTask = new TaskCompletionSource();
        cancellationToken.Register(cancellationTask.SetResult);
        await Task.WhenAny(adInitializationListener.Task, cancellationTask.Task).VhConfigureAwait();
        cancellationToken.ThrowIfCancellationRequested();

        await adInitializationListener.Task.VhConfigureAwait();
        _isUnityAdInitialized = true;
    }

    public async Task LoadAd(IUiContext uiContext, CancellationToken cancellationToken)
    {
        var appUiContext = (AndroidUiContext)uiContext;
        var activity = appUiContext.Activity;
        if (activity.IsDestroyed)
            throw new AdException("MainActivity has been destroyed before loading the ad.");

        // Initialize unity
        await EnsureUnityAdInitialized(activity, cancellationToken);

        // reset the last loaded ad
        AdLoadedTime = null;

        // Load a new Ad
        var adLoadListener = new MyAdLoadListener();
        activity.RunOnUiThread(() => UnityAds.Load(adPlacementId, adLoadListener));

        var cancellationTask = new TaskCompletionSource();
        cancellationToken.Register(cancellationTask.SetResult);
        await Task.WhenAny(adLoadListener.Task, cancellationTask.Task).VhConfigureAwait();
        cancellationToken.ThrowIfCancellationRequested();

        await adLoadListener.Task.VhConfigureAwait();
        AdLoadedTime = DateTime.Now;
    }

    public async Task ShowAd(IUiContext uiContext, string? customData, CancellationToken cancellationToken)
    {
        var appUiContext = (AndroidUiContext)uiContext;
        var activity = appUiContext.Activity;
        if (activity.IsDestroyed)
            throw new AdException("MainActivity has been destroyed before showing the ad.");

        if (AdLoadedTime == null)
            throw new AdException($"The {AdType} has not been loaded.");

        try
        {

            var adShowListener = new MyAdShowListener();
            activity.RunOnUiThread(() => UnityAds.Show(activity, adPlacementId, adShowListener));

            // wait for show or dismiss
            var cancellationTask = new TaskCompletionSource();
            cancellationToken.Register(cancellationTask.SetResult);
            await Task.WhenAny(adShowListener.Task, cancellationTask.Task).VhConfigureAwait();
            cancellationToken.ThrowIfCancellationRequested();

            await adShowListener.Task.VhConfigureAwait();
        }
        finally
        {
            AdLoadedTime = null;
        }
    }

    private class MyAdInitializationListener : Java.Lang.Object, IUnityAdsInitializationListener
    {
        private readonly TaskCompletionSource _loadedCompletionSource = new();
        public Task Task => _loadedCompletionSource.Task;

        public void OnInitializationComplete()
        {
            _loadedCompletionSource.TrySetResult();
        }

        public void OnInitializationFailed(UnityAds.UnityAdsInitializationError? error, string? message)
        {
            _loadedCompletionSource.TrySetException(new AdLoadException(
                $"Unity Ads initialization failed with Error: {error}, Message: {message}"));
        }
    }

    private class MyAdLoadListener : Java.Lang.Object, IUnityAdsLoadListener
    {
        private readonly TaskCompletionSource _loadedCompletionSource = new();
        public Task Task => _loadedCompletionSource.Task;

        public void OnUnityAdsAdLoaded(string? adPlacementId)
        {
            _loadedCompletionSource.TrySetResult();
        }

        public void OnUnityAdsFailedToLoad(string? adUnitId, UnityAds.UnityAdsLoadError? error, string? message)
        {
            _loadedCompletionSource.TrySetException(new AdLoadException(
                $"Unity Ads failed to load ad for AdUnitId: {adUnitId} with Error: {error}, Message: {message}"));
        }
    }

    private class MyAdShowListener : Java.Lang.Object, IUnityAdsShowListener
    {
        private readonly TaskCompletionSource _loadedCompletionSource = new();
        public Task Task => _loadedCompletionSource.Task;

        public void OnUnityAdsShowStart(string? adPlacementId)
        {
            _loadedCompletionSource.TrySetResult();
        }
        public void OnUnityAdsShowFailure(string? adPlacementId, UnityAds.UnityAdsShowError? error, string? message)
        {
            _loadedCompletionSource.TrySetException(new AdException(
                $"Unity Ads failed to show ad for AdPlacementId: {adPlacementId} with Error: {error}, Message: {message}"));
        }

        public void OnUnityAdsShowClick(string? adPlacementId)
        {
        }
        public void OnUnityAdsShowComplete(string? adPlacementId, UnityAds.UnityAdsShowCompletionState? state)
        {
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}