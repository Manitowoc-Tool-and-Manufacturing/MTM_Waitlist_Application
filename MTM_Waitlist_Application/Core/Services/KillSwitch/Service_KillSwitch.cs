using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Interfaces.KillSwitch;
using Core.Models.KillSwitch;
using Microsoft.Extensions.Logging;

namespace Services.KillSwitch;

/// <summary>
/// Client-side kill-switch service.
/// Maintains a background timer that fires every <see cref="HeartbeatInterval"/> seconds.
/// On each tick it:
/// <list type="number">
///   <item>POSTs a heartbeat to <c>/api/admin/heartbeat</c> so the admin console
///         knows this client is alive and can display it in the Connected Users and
///         KillSwitch module views.</item>
///   <item>GETs <c>/api/admin/shutdown-signal</c> and raises
///         <see cref="ShutdownSignalReceived"/> on the main thread if the server
///         has issued a signal targeting this machine or user.</item>
/// </list>
/// The service is a singleton — <see cref="StartHeartbeat"/> must be called once
/// after authentication and <see cref="StopHeartbeat"/> must be called on logout.
/// </summary>
public sealed class Service_KillSwitch : IService_KillSwitch, IDisposable
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    private readonly IApiClient _apiClient;
    private readonly ILogger<Service_KillSwitch> _logger;

    // Guard against double-start.
    private int _started; // 0 = not started, 1 = started
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private string _username = string.Empty;
    private string _fullName = string.Empty;
    private string? _workstationName;

    /// <summary>Captured once at first start so heartbeats always use the same machine name.</summary>
    private static readonly string MachineName = DeviceInfo.Name;

    /// <inheritdoc/>
    public event EventHandler<Model_KillSwitch_Signal>? ShutdownSignalReceived;

    /// <summary>
    /// Initialises a new instance with the supplied API client.
    /// </summary>
    public Service_KillSwitch(IApiClient apiClient, ILogger<Service_KillSwitch> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void StartHeartbeat(string username, string fullName, string? workstationName)
    {
        // Prevent a second start from a re-login without an intervening logout.
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            _logger.LogInformation("[KillSwitch] StartHeartbeat ignored because the heartbeat loop is already running");
            return;
        }

        _username = username;
        _fullName = fullName;
        _workstationName = workstationName;

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation(
            "[KillSwitch] Heartbeat loop started — machine={MachineName}, username={Username}, fullName={FullName}, workstation={Workstation}",
            MachineName, _username, _fullName, _workstationName ?? "(none)");
    }

    /// <inheritdoc/>
    public void StopHeartbeat()
    {
        if (Interlocked.CompareExchange(ref _started, 0, 1) != 1)
        {
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _logger.LogInformation("[KillSwitch] Heartbeat loop stopped");
    }

    /// <inheritdoc/>
    public async Task StopHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        var username = _username;
        StopHeartbeat();

        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        await NotifyDisconnectAsync(username, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopHeartbeat();
    }

    // ── Private ────────────────────────────────────────────────────────────

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            // Send the first heartbeat immediately so the admin console sees this client
            // right away rather than waiting up to 15 seconds.
            await TickAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(HeartbeatInterval, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await TickAsync(ct);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "[KillSwitch] Heartbeat loop stopped unexpectedly");
            Interlocked.Exchange(ref _started, 0);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }

        await PostHeartbeatAsync(ct);

        // Poll the shutdown signal — failures are swallowed so a transient network
        // hiccup does not prevent the next poll from running.
        await PollShutdownSignalAsync(ct);
    }

    private async Task PostHeartbeatAsync(CancellationToken ct)
    {
        try
        {
            var result = await _apiClient.PostAsync<object>(
                Constants_Api.HeartbeatEndpoint,
                new
                {
                    machineName = MachineName,
                    username = _username,
                    fullName = _fullName,
                    workstationName = _workstationName,
                },
                ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[KillSwitch] Heartbeat POST succeeded — endpoint={Endpoint}, machine={MachineName}, username={Username}",
                    Constants_Api.HeartbeatEndpoint, MachineName, _username);
                return;
            }

            _logger.LogWarning(
                "[KillSwitch] Heartbeat POST failed — endpoint={Endpoint}, machine={MachineName}, username={Username}, error={Error}",
                Constants_Api.HeartbeatEndpoint, MachineName, _username, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[KillSwitch] Heartbeat POST threw — endpoint={Endpoint}, machine={MachineName}, username={Username}",
                Constants_Api.HeartbeatEndpoint, MachineName, _username);
        }
    }

    private async Task PollShutdownSignalAsync(CancellationToken ct)
    {
        try
        {
            var endpoint =
                $"{Constants_Api.ShutdownSignalEndpoint}?machineName={Uri.EscapeDataString(MachineName)}&username={Uri.EscapeDataString(_username)}";

            var result = await _apiClient.GetAsync<Model_KillSwitch_Signal>(endpoint, ct);

            if (!result.IsSuccess && result.Data is null)
            {
                _logger.LogWarning(
                    "[KillSwitch] Shutdown-signal poll failed — endpoint={Endpoint}, machine={MachineName}, username={Username}, error={Error}",
                    Constants_Api.ShutdownSignalEndpoint, MachineName, _username, result.ErrorMessage);
                return;
            }

            // Server returns 204 (no content) when there is no active signal.
            // HttpApiClient's ReadResultAsync treats a null body as a Failure, so
            // both "no signal" (204) and network errors produce result.Data == null.
            // In either case we simply wait for the next tick — we never shut the
            // client down due to a transient network failure.
            if (result.Data is null)
            {
                return;
            }

            var signal = result.Data;

            await AcknowledgeShutdownSignalAsync(ct);

            ShutdownSignalReceived?.Invoke(this, signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[KillSwitch] Shutdown-signal poll threw — endpoint={Endpoint}, machine={MachineName}, username={Username}",
                Constants_Api.ShutdownSignalEndpoint, MachineName, _username);
        }
    }

    private async Task AcknowledgeShutdownSignalAsync(CancellationToken ct)
    {
        try
        {
            var result = await _apiClient.PostAsync<object>(
                Constants_Api.ShutdownSignalAcknowledgeEndpoint,
                new
                {
                    machineName = MachineName,
                    username = _username,
                },
                ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[KillSwitch] Shutdown signal acknowledged — endpoint={Endpoint}, machine={MachineName}, username={Username}",
                    Constants_Api.ShutdownSignalAcknowledgeEndpoint, MachineName, _username);
                return;
            }

            _logger.LogWarning(
                "[KillSwitch] Shutdown signal acknowledgement failed — endpoint={Endpoint}, machine={MachineName}, username={Username}, error={Error}",
                Constants_Api.ShutdownSignalAcknowledgeEndpoint, MachineName, _username, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[KillSwitch] Shutdown signal acknowledgement threw — endpoint={Endpoint}, machine={MachineName}, username={Username}",
                Constants_Api.ShutdownSignalAcknowledgeEndpoint, MachineName, _username);
        }
    }

    private async Task NotifyDisconnectAsync(string username, CancellationToken ct)
    {
        try
        {
            var result = await _apiClient.PostAsync<object>(
                Constants_Api.DisconnectEndpoint,
                new
                {
                    machineName = MachineName,
                    username,
                },
                ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[KillSwitch] Disconnect notified — endpoint={Endpoint}, machine={MachineName}, username={Username}",
                    Constants_Api.DisconnectEndpoint, MachineName, username);
                return;
            }

            _logger.LogWarning(
                "[KillSwitch] Disconnect notification failed — endpoint={Endpoint}, machine={MachineName}, username={Username}, error={Error}",
                Constants_Api.DisconnectEndpoint, MachineName, username, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[KillSwitch] Disconnect notification threw — endpoint={Endpoint}, machine={MachineName}, username={Username}",
                Constants_Api.DisconnectEndpoint, MachineName, username);
        }
    }
}
