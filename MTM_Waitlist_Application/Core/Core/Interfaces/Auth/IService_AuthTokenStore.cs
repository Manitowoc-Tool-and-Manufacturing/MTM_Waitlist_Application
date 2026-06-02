using Core.Models.Auth;

namespace Core.Interfaces.Auth;

/// <summary>
/// Stores and retrieves authenticated session tokens for the current platform.
/// Implementations must avoid plaintext file storage and use the safest platform-specific option available.
/// </summary>
public interface IService_AuthTokenStore
{
    /// <summary>
    /// Gets the current access token used for authenticated API requests.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>The current access token, or an empty string when no session is stored.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current refresh token used to renew the access token.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>The current refresh token, or an empty string when no session is stored.</returns>
    Task<string> GetRefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full currently stored authenticated session.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>The stored session, or <see langword="null"/> when no session is available.</returns>
    Task<Model_AuthToken?> GetStoredSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the authenticated session returned by the server.
    /// </summary>
    /// <param name="token">Authenticated token response.</param>
    /// <param name="cancellationToken">Token used to cancel the write operation.</param>
    Task StoreSessionAsync(Model_AuthToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all stored authenticated session values.
    /// </summary>
    Task ClearSessionAsync();
}
