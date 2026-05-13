using Microsoft.AspNetCore.Builder;
using MTM_Waitlist_Server.Admin.Logging;
using MTM_Waitlist_Server.Api;
using MTM_Waitlist_Server.Core.Interfaces.Api;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Manages the in-process Kestrel API host lifecycle.
/// Stores the running <see cref="WebApplication"/> task and restarts it on demand
/// if it has faulted or completed unexpectedly.
/// </summary>
internal sealed class Service_ApiHost : IService_ApiHost
{
    private readonly IService_SettingsStore _settingsStore;
    private readonly ServiceCollection _sharedServices;
    private readonly IServiceProvider _sharedProvider;
    private WebApplication? _webApp;
    private Task? _hostTask;

    public Service_ApiHost(
        IService_SettingsStore settingsStore,
        ServiceCollection sharedServices,
        IServiceProvider sharedProvider)
    {
        _settingsStore = settingsStore;
        _sharedServices = sharedServices;
        _sharedProvider = sharedProvider;
    }

    /// <inheritdoc/>
    public bool IsRunning => _hostTask is { IsCompleted: false };

    /// <summary>
    /// Called once during normal launch to start the host for the first time.
    /// </summary>
    public void Start()
    {
        var listenUrl = _settingsStore.Get().Api.ListenAddress;
        StartupLogger.Info($"ApiHost.Start: building Kestrel app bound to '{listenUrl}'.");
        _webApp = ApiStartup.BuildApp(listenUrl, _sharedServices, _sharedProvider);
        StartupLogger.Info("ApiHost.Start: WebApplication built. Starting RunAsync on background task.");
        _hostTask = _webApp.RunAsync();
        StartupLogger.Info("ApiHost.Start: RunAsync task launched.");
    }

    /// <inheritdoc/>
    public async Task<bool> EnsureRunningAsync(CancellationToken ct = default)
    {
        // Already healthy — nothing to do.
        if (IsRunning)
        {
            StartupLogger.Info("ApiHost.EnsureRunningAsync: host is already running — no action needed.");
            return true;
        }

        StartupLogger.Warn("ApiHost.EnsureRunningAsync: host task is not running (completed or faulted). Attempting restart.");

        // Stop stale host cleanly before rebuilding.
        if (_webApp is not null)
        {
            try
            {
                StartupLogger.Info("ApiHost.EnsureRunningAsync: stopping stale WebApplication.");
                await _webApp.StopAsync(CancellationToken.None);
                StartupLogger.Info("ApiHost.EnsureRunningAsync: stale WebApplication stopped.");
            }
            catch (Exception ex)
            {
                StartupLogger.Warn($"ApiHost.EnsureRunningAsync: StopAsync threw during stale-host cleanup — {ex.GetType().Name}: {ex.Message}. Ignoring and proceeding.");
            }
        }

        try
        {
            var listenUrl = _settingsStore.Get().Api.ListenAddress;
            StartupLogger.Info($"ApiHost.EnsureRunningAsync: rebuilding WebApplication on '{listenUrl}'.");
            _webApp = ApiStartup.BuildApp(listenUrl, _sharedServices, _sharedProvider);
            _hostTask = _webApp.RunAsync();
            StartupLogger.Info("ApiHost.EnsureRunningAsync: restart succeeded — RunAsync task launched.");
            return true;
        }
        catch (Exception ex)
        {
            StartupLogger.Error("ApiHost.EnsureRunningAsync: restart FAILED.", ex);
            return false;
        }
    }
}
