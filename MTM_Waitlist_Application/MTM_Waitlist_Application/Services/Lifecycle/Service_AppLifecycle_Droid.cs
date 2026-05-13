#if ANDROID
using Core.Constants.Auth;
using Core.Interfaces.KillSwitch;
using Core.Interfaces.Lifecycle;
using Core.Models.KillSwitch;
using Microsoft.Extensions.Logging;

namespace MTM_Waitlist_Application.Services.Lifecycle;

/// <summary>
/// Android implementation of the authenticated application lifecycle.
/// Starts background services that require a valid stored authentication session after the MAUI shell is visible.
/// </summary>
public sealed class Service_AppLifecycle_Droid : IService_AppLifecycle
{
    private readonly IService_KillSwitch _killSwitch;
    private readonly ILogger<Service_AppLifecycle_Droid> _logger;
    private int _started;

    /// <summary>
    /// Initializes a new Android lifecycle service instance.
    /// </summary>
    /// <param name="killSwitch">Kill-switch heartbeat service.</param>
    /// <param name="logger">Lifecycle logger.</param>
    public Service_AppLifecycle_Droid(
        IService_KillSwitch killSwitch,
        ILogger<Service_AppLifecycle_Droid> logger)
    {
        _killSwitch = killSwitch;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAuthenticatedSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[STARTUP][Droid] Authenticated lifecycle starting");

            var username = await ReadSecureStorageAsync(Constants_AuthStorage.AuthUsernameKey);
            var displayName = await ReadSecureStorageAsync(Constants_AuthStorage.AuthDisplayNameKey);
            await StartAuthenticatedSessionAsync(username, displayName, workstationName: null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STARTUP][Droid] Authenticated lifecycle failed to start");
        }
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
            _logger.LogInformation("[STARTUP][Droid] Authenticated lifecycle already started");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            Interlocked.Exchange(ref _started, 0);
            _logger.LogWarning("[STARTUP][Droid] Kill-switch heartbeat not started because stored username is missing");
            return Task.CompletedTask;
        }

        _killSwitch.ShutdownSignalReceived += OnShutdownSignalReceived;
        _killSwitch.StartHeartbeat(username, displayName, workstationName);

        _logger.LogInformation(
            "[STARTUP][Droid] Authenticated lifecycle started — username={Username}, displayName={DisplayName}",
            username,
            string.IsNullOrWhiteSpace(displayName) ? "(missing)" : displayName);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAuthenticatedSessionAsync()
    {
        if (Interlocked.CompareExchange(ref _started, 0, 1) != 1)
        {
            _logger.LogInformation("[STARTUP][Droid] Authenticated lifecycle already stopped");
            return Task.CompletedTask;
        }

        _killSwitch.ShutdownSignalReceived -= OnShutdownSignalReceived;
        _ = _killSwitch.StopHeartbeatAsync();
        _logger.LogInformation("[STARTUP][Droid] Authenticated lifecycle stopped");
        return Task.CompletedTask;
    }

    private static async Task<string> ReadSecureStorageAsync(string key)
    {
        return await MainThread.InvokeOnMainThreadAsync(
            () => SecureStorage.GetAsync(key)) ?? string.Empty;
    }

    private async void OnShutdownSignalReceived(object? sender, Model_KillSwitch_Signal signal)
    {
        try
        {
            _logger.LogWarning(
                "[KillSwitch][Droid] Shutdown signal received — target={Target}, warningSeconds={WarningSeconds}, message={Message}",
                signal.Target,
                signal.WarningSeconds,
                signal.Message);

            await StopAuthenticatedSessionAsync();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (signal.WarningSeconds > 0 && Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
                {
                    await page.DisplayAlertAsync(
                        "Application Closing",
                        $"{signal.Message}\n\nTap OK to close the application.",
                        "OK");
                }

                _logger.LogInformation("[KillSwitch][Droid] Closing application per admin signal");
                Application.Current?.Quit();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KillSwitch][Droid] Failed to show shutdown warning; closing application per admin signal");
            Application.Current?.Quit();
        }
    }
}
#endif
