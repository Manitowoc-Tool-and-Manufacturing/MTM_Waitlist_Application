using Android.App;
using Android.Content.PM;
using Android.OS;
using Core.Interfaces.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace MTM_Waitlist_Application.Droid
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnDestroy()
        {
            NotifyLifecycleStoppedAsync().GetAwaiter().GetResult();
            base.OnDestroy();
        }

        private static async Task NotifyLifecycleStoppedAsync()
        {
            if (IPlatformApplication.Current?.Services.GetService<IService_AppLifecycle>() is IService_AppLifecycle appLifecycle)
            {
                await appLifecycle.StopAuthenticatedSessionAsync();
            }
        }
    }
}
