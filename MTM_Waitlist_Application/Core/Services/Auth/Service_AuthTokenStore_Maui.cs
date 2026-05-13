using Core.Constants.Auth;
using Core.Interfaces.Auth;
using Core.Models.Auth;

namespace Services.Auth;

/// <summary>
/// MAUI token store backed by platform SecureStorage.
/// Used by Android and other MAUI-hosted targets.
/// </summary>
public sealed class Service_AuthTokenStore_Maui : IService_AuthTokenStore
{
    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return await ReadAsync(Constants_AuthStorage.AuthTokenKey) ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<string> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        return await ReadAsync(Constants_AuthStorage.RefreshTokenKey) ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task StoreSessionAsync(Model_AuthToken token, CancellationToken cancellationToken = default)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await SecureStorage.SetAsync(Constants_AuthStorage.AuthTokenKey, token.Token);
            await SecureStorage.SetAsync(Constants_AuthStorage.RefreshTokenKey, token.RefreshToken);
            await SecureStorage.SetAsync(Constants_AuthStorage.AuthTokenExpiresAtKey, token.ExpiresAt.ToString("O"));
            await SecureStorage.SetAsync(Constants_AuthStorage.AuthRoleKey, token.Role);
            await SecureStorage.SetAsync(Constants_AuthStorage.AuthUsernameKey, token.Username);
            await SecureStorage.SetAsync(Constants_AuthStorage.AuthDisplayNameKey, token.DisplayName);
        });
    }

    /// <inheritdoc />
    public Task ClearSessionAsync()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SecureStorage.Remove(Constants_AuthStorage.AuthTokenKey);
            SecureStorage.Remove(Constants_AuthStorage.RefreshTokenKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthTokenExpiresAtKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthRoleKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthUsernameKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthDisplayNameKey);
        });

        return Task.CompletedTask;
    }

    private static async Task<string?> ReadAsync(string key)
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(() => SecureStorage.GetAsync(key));
        }
        catch
        {
            return string.Empty;
        }
    }
}
