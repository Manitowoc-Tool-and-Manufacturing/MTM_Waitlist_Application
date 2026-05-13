using Core.Interfaces.KillSwitch;
using Core.Interfaces.Lifecycle;
using Core.Models.KillSwitch;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace MTM_Waitlist_Application.WinUI.Services.Lifecycle;

/// <summary>
/// WinUI implementation of the authenticated application lifecycle.
/// Starts background services that require a valid stored authentication session after the WinUI shell is visible.
/// </summary>
public sealed class Service_AppLifecycle_WinUI : IService_AppLifecycle
{
    private readonly IService_KillSwitch _killSwitch;
    private readonly ILogger<Service_AppLifecycle_WinUI> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private int _started;

    /// <summary>
    /// Initializes a new WinUI lifecycle service instance.
    /// </summary>
    /// <param name="killSwitch">Kill-switch heartbeat service.</param>
    /// <param name="logger">Lifecycle logger.</param>
    public Service_AppLifecycle_WinUI(
        IService_KillSwitch killSwitch,
        ILogger<Service_AppLifecycle_WinUI> logger)
    {
        _killSwitch = killSwitch;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    /// <inheritdoc />
    public async Task StartAuthenticatedSessionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("[STARTUP][WinUI] Authenticated lifecycle requires sign-in identity; secure-storage startup is not used by the standalone WinUI host");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAuthenticatedSessionAsync(
        string username,
        string displayName,
        string? workstationName = null,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            _logger.LogInformation("[STARTUP][WinUI] Authenticated lifecycle already started");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("[STARTUP][WinUI] Authenticated lifecycle starting");

            if (string.IsNullOrWhiteSpace(username))
            {
                Interlocked.Exchange(ref _started, 0);
                _logger.LogWarning("[STARTUP][WinUI] Kill-switch heartbeat not started because stored username is missing");
                return Task.CompletedTask;
            }

            _killSwitch.ShutdownSignalReceived += OnShutdownSignalReceived;
            _killSwitch.StartHeartbeat(username, displayName, workstationName);

            _logger.LogInformation(
                "[STARTUP][WinUI] Authenticated lifecycle started — username={Username}, displayName={DisplayName}",
                username,
                string.IsNullOrWhiteSpace(displayName) ? "(missing)" : displayName);
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _started, 0);
            _logger.LogError(ex, "[STARTUP][WinUI] Authenticated lifecycle failed to start");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAuthenticatedSessionAsync()
    {
        if (Interlocked.CompareExchange(ref _started, 0, 1) != 1)
        {
            _logger.LogInformation("[STARTUP][WinUI] Authenticated lifecycle already stopped");
            return;
        }

        _killSwitch.ShutdownSignalReceived -= OnShutdownSignalReceived;
        await _killSwitch.StopHeartbeatAsync();
        _logger.LogInformation("[STARTUP][WinUI] Authenticated lifecycle stopped");
    }

    private async void OnShutdownSignalReceived(object? sender, Model_KillSwitch_Signal signal)
    {
        _logger.LogWarning(
            "[KillSwitch][WinUI] Shutdown signal received — target={Target}, warningSeconds={WarningSeconds}, message={Message}",
            signal.Target,
            signal.WarningSeconds,
            signal.Message);

        await StopAuthenticatedSessionAsync();

        MainWindow.ShowShutdownWarning(signal.Message, signal.WarningSeconds);

        if (signal.WarningSeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(signal.WarningSeconds));
        }

        _logger.LogInformation("[KillSwitch][WinUI] Closing application per admin signal");
        _dispatcherQueue.TryEnqueue(() => Microsoft.UI.Xaml.Application.Current.Exit());
    }
}
