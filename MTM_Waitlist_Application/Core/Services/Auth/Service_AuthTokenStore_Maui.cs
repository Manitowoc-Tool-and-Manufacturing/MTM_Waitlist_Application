using Core.Constants.Auth;
using Core.Interfaces.Auth;
using Core.Models.Auth;
using System.Globalization;

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
    public async Task<Model_AuthToken?> GetStoredSessionAsync(CancellationToken cancellationToken = default)
    {
        var token = await ReadAsync(Constants_AuthStorage.AuthTokenKey);
        var refreshToken = await ReadAsync(Constants_AuthStorage.RefreshTokenKey);
        var expiresAtText = await ReadAsync(Constants_AuthStorage.AuthTokenExpiresAtKey);

        if (string.IsNullOrWhiteSpace(token)
            || string.IsNullOrWhiteSpace(refreshToken)
            || string.IsNullOrWhiteSpace(expiresAtText)
            || !DateTimeOffset.TryParse(expiresAtText, out var expiresAt))
        {
            return null;
        }

        var userIdText = await ReadAsync(Constants_AuthStorage.AuthUserIdKey);
        _ = int.TryParse(userIdText, out var userId);

        return new Model_AuthToken
        {
            UserId = userId,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Role = await ReadAsync(Constants_AuthStorage.AuthRoleKey) ?? string.Empty,
            Username = await ReadAsync(Constants_AuthStorage.AuthUsernameKey) ?? string.Empty,
            DisplayName = await ReadAsync(Constants_AuthStorage.AuthDisplayNameKey) ?? string.Empty,
        };
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
            await SecureStorage.SetAsync(Constants_AuthStorage.AuthUserIdKey, token.UserId.ToString(CultureInfo.InvariantCulture));
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
            SecureStorage.Remove(Constants_AuthStorage.AuthUserIdKey);
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
