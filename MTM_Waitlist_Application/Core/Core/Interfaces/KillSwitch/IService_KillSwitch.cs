using Core.Models.KillSwitch;

namespace Core.Interfaces.KillSwitch;

/// <summary>
/// Contract for the client-side kill-switch service.
/// Implementations maintain a background heartbeat loop that:
/// <list type="bullet">
///   <item>Posts a heartbeat to the admin API every 15 seconds so the admin console
///         knows this client is alive.</item>
///   <item>Polls the shutdown-signal endpoint on each heartbeat interval and raises
///         <see cref="ShutdownSignalReceived"/> if the server has issued a signal
///         that targets this client.</item>
/// </list>
/// </summary>
public interface IService_KillSwitch
{
    /// <summary>
    /// Raised on the UI thread when the server returns a shutdown signal for this client.
    /// Subscribers should display a countdown dialog and close the application.
    /// </summary>
    event EventHandler<Model_KillSwitch_Signal> ShutdownSignalReceived;

    /// <summary>
    /// Starts the background heartbeat and polling loop.
    /// Should be called once, immediately after the user is authenticated, so the
    /// admin console begins receiving heartbeats as soon as the session is active.
    /// Calling this more than once is safe — subsequent calls are ignored.
    /// </summary>
    /// <param name="username">The authenticated application username.</param>
    /// <param name="fullName">The authenticated user's display name.</param>
    /// <param name="workstationName">
    /// Optional friendly label for the workstation, or <see langword="null"/> for personal machines.
    /// </param>
    void StartHeartbeat(string username, string fullName, string? workstationName);

    /// <summary>
    /// Stops the background heartbeat loop and releases its resources.
    /// Called when the user logs out.
    /// </summary>
    void StopHeartbeat();

    /// <summary>
    /// Stops the background heartbeat loop and notifies the server that this client disconnected normally.
    /// </summary>
    Task StopHeartbeatAsync(CancellationToken cancellationToken = default);
}
