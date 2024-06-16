using VpnHood.Client.App.Droid.Ads.VhUnityAds;
using VpnHood.Client.Device.Droid;
using VpnHood.Client.Device.Droid.ActivityEvents;

namespace Sample;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : ActivityEvent
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Set our view from the "main" layout resource
        SetContentView(Resource.Layout.activity_main);
        _ = Foo();
    }

    private async Task Foo()
    {
        try
        {
            var adService = UnityAdService.Create("5633286", "Before_Connect_Interstitial", true);
            await adService.LoadAd(new AndroidUiContext(this), new CancellationToken());
            await adService.ShowAd(new AndroidUiContext(this), "", new CancellationToken());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}