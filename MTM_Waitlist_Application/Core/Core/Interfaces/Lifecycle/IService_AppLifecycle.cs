namespace Core.Interfaces.Lifecycle;

/// <summary>
/// Coordinates platform-specific application lifecycle work that begins after an authenticated shell is active.
/// Implementations are responsible for starting and stopping authenticated background services such as kill-switch heartbeats.
/// </summary>
public interface IService_AppLifecycle
{
    /// <summary>
    /// Starts the authenticated application lifecycle after a valid session exists and the main shell is active.
    /// Calling this method more than once must be safe.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel startup work.</param>
    Task StartAuthenticatedSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the authenticated application lifecycle with identity that was just returned by sign-in.
    /// Calling this method more than once must be safe.
    /// </summary>
    /// <param name="username">Authenticated username.</param>
    /// <param name="displayName">Authenticated user's display name.</param>
    /// <param name="workstationName">Optional friendly workstation name.</param>
    /// <param name="cancellationToken">Token used to cancel startup work.</param>
    Task StartAuthenticatedSessionAsync(
        string username,
        string displayName,
        string? workstationName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops authenticated application lifecycle work such as background heartbeats.
    /// Calling this method more than once must be safe.
    /// </summary>
    Task StopAuthenticatedSessionAsync();
}
