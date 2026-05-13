using Core.Interfaces.Auth;
using Core.Models.Auth;

namespace MTM_Waitlist_Application.WinUI.Services.Auth;

/// <summary>
/// WinUI token store for the standalone Windows host.
/// Keeps the active session in process memory so background services can attach the bearer token without MAUI MainThread.
/// </summary>
public sealed class Service_AuthTokenStore_WinUI : IService_AuthTokenStore
{
    private readonly object _gate = new();
    private Model_AuthToken? _currentToken;

    /// <inheritdoc />
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_currentToken?.Token ?? string.Empty);
        }
    }

    /// <inheritdoc />
    public Task<string> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_currentToken?.RefreshToken ?? string.Empty);
        }
    }

    /// <inheritdoc />
    public Task StoreSessionAsync(Model_AuthToken token, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _currentToken = token;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearSessionAsync()
    {
        lock (_gate)
        {
            _currentToken = null;
        }

        return Task.CompletedTask;
    }
}
