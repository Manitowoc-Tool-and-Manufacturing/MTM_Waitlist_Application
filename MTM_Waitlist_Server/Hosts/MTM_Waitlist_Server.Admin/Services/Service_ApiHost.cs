using Microsoft.AspNetCore.Builder;
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
    private WebApplication? _webApp;
    private Task? _hostTask;

    public Service_ApiHost(IService_SettingsStore settingsStore, ServiceCollection sharedServices)
    {
        _settingsStore = settingsStore;
        _sharedServices = sharedServices;
    }

    /// <inheritdoc/>
    public bool IsRunning => _hostTask is { IsCompleted: false };

    /// <summary>
    /// Called once during normal launch to start the host for the first time.
    /// </summary>
    public void Start()
    {
        var listenUrl = _settingsStore.Get().Api.ListenAddress;
        _webApp = ApiStartup.BuildApp(listenUrl, _sharedServices);
        _hostTask = _webApp.RunAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> EnsureRunningAsync(CancellationToken ct = default)
    {
        // Already healthy — nothing to do.
        if (IsRunning)
        {
            return true;
        }

        // Stop stale host cleanly before rebuilding.
        if (_webApp is not null)
        {
            try { await _webApp.StopAsync(CancellationToken.None); } catch { /* ignore */ }
        }

        try
        {
            var listenUrl = _settingsStore.Get().Api.ListenAddress;
            _webApp = ApiStartup.BuildApp(listenUrl, _sharedServices);
            _hostTask = _webApp.RunAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
