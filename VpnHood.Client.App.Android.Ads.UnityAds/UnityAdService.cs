using Com.Unity3d.Ads;
using VpnHood.Client.App.Abstractions;
using VpnHood.Client.Device;
using VpnHood.Client.Device.Droid;
using VpnHood.Common.Utils;

namespace VpnHood.Client.App.Droid.Ads.VhUnityAds;

public class UnityAdService(string adGameId, string adPlacementId, bool testMode = false) : IAppAdService
{
    private MyAdInitializationListener? _adInitializationListener;
    private MyAdLoadListener? _adLoadListener;
    private MyAdShowListener? _adShowListener;
    private DateTime _lastLoadAdTime = DateTime.MinValue;
    private static bool _isUnityAdInitialized;
    private static bool _isUnityAdLoaded;

    public static UnityAdService Create(string adGameId, string adPlacementId, bool testMode = false)
    {
        var ret = new UnityAdService(adGameId, adPlacementId, testMode);
        return ret;
    }

    public string NetworkName => "UnityAds";
    public AppAdType AdType => AppAdType.InterstitialAd;

    public async Task LoadAd(IUiContext uiContext, CancellationToken cancellationToken)
    {
        var appUiContext = (AndroidUiContext)uiContext;
        var activity = appUiContext.Activity;
        if (activity.IsDestroyed)
            throw new AdException("MainActivity has been destroyed before loading the ad.");

        // Initialize ad
        if (!_isUnityAdInitialized)
        {
            try
            {
                _adInitializationListener = new MyAdInitializationListener();
                activity.RunOnUiThread(() => UnityAds.Initialize(
                    activity, adGameId, testMode, _adInitializationListener));

                var cancellationTask = new TaskCompletionSource();
                cancellationToken.Register(cancellationTask.SetResult);
                await Task.WhenAny(_adInitializationListener.Task, cancellationTask.Task).VhConfigureAwait();
                cancellationToken.ThrowIfCancellationRequested();

                _isUnityAdInitialized = await _adInitializationListener.Task.VhConfigureAwait();
                
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _isUnityAdInitialized = false;
                if (ex is AdLoadException) throw;
                throw new AdLoadException($"Failed to load {AdType}.", ex);
            }
        }

        // Ad already loaded
        if (_adLoadListener != null && _lastLoadAdTime.AddHours(1) < DateTime.Now)
            _isUnityAdLoaded = await _adLoadListener.Task.VhConfigureAwait();


        // Load a new Ad
        try
        {
            _adLoadListener = new MyAdLoadListener();
            activity.RunOnUiThread(() => UnityAds.Load(adPlacementId, _adLoadListener));

            var cancellationTask = new TaskCompletionSource();
            cancellationToken.Register(cancellationTask.SetResult);
            await Task.WhenAny(_adLoadListener.Task, cancellationTask.Task).VhConfigureAwait();
            cancellationToken.ThrowIfCancellationRequested();

            _isUnityAdLoaded = await _adLoadListener.Task.VhConfigureAwait();
            _lastLoadAdTime = DateTime.Now;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _isUnityAdLoaded = false;
            _lastLoadAdTime = DateTime.MinValue;
            if (ex is AdLoadException) throw;
            throw new AdLoadException($"Failed to load {AdType}.", ex);
        }
    }

    public async Task ShowAd(IUiContext uiContext, string? customData, CancellationToken cancellationToken)
    {
        var appUiContext = (AndroidUiContext)uiContext;
        var activity = appUiContext.Activity;
        if (activity.IsDestroyed)
            throw new AdException("MainActivity has been destroyed before showing the ad.");

        if (!_isUnityAdLoaded)
            throw new AdException($"The {AdType} has not been loaded.");

        _adShowListener = new MyAdShowListener();
        activity.RunOnUiThread(() => UnityAds.Show(activity, adPlacementId, _adShowListener));

        // wait for show or dismiss
        var cancellationTask = new TaskCompletionSource();
        cancellationToken.Register(cancellationTask.SetResult);
        await Task.WhenAny(_adShowListener.Task, cancellationTask.Task).VhConfigureAwait();
        cancellationToken.ThrowIfCancellationRequested();

        await _adShowListener.Task.VhConfigureAwait();
        _isUnityAdLoaded = false;
    }

    private class MyAdInitializationListener : Java.Lang.Object, IUnityAdsInitializationListener
    {
        private readonly TaskCompletionSource<bool> _loadedCompletionSource = new();
        public Task<bool> Task => _loadedCompletionSource.Task;

        public void OnInitializationComplete()
        {
            _loadedCompletionSource.TrySetResult(true);
        }

        public void OnInitializationFailed(UnityAds.UnityAdsInitializationError? error, string? message)
        {
            _loadedCompletionSource.TrySetException(new AdLoadException(
                $"Unity Ads initialization failed with Error: {error}, Message: {message}"));
        }
    }

    private class MyAdLoadListener : Java.Lang.Object, IUnityAdsLoadListener
    {
        private readonly TaskCompletionSource<bool> _loadedCompletionSource = new();
        public Task<bool> Task => _loadedCompletionSource.Task;

        public void OnUnityAdsAdLoaded(string? adPlacementId)
        {
            _loadedCompletionSource.TrySetResult(true);
        }

        public void OnUnityAdsFailedToLoad(string? adUnitId, UnityAds.UnityAdsLoadError? error, string? message)
        {
            _loadedCompletionSource.TrySetException(new AdLoadException(
                $"Unity Ads failed to load ad for AdUnitId: {adUnitId} with Error: {error}, Message: {message}"));
        }
    }

    private class MyAdShowListener : Java.Lang.Object, IUnityAdsShowListener
    {
        private readonly TaskCompletionSource<bool> _loadedCompletionSource = new();
        public Task<bool> Task => _loadedCompletionSource.Task;

        public void OnUnityAdsShowStart(string? adPlacementId)
        {
            _loadedCompletionSource.TrySetResult(true);
        }
        public void OnUnityAdsShowFailure(string? adPlacementId, UnityAds.UnityAdsShowError? error, string? message)
        {
            _loadedCompletionSource.TrySetException(new AdLoadException(
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
    }
}