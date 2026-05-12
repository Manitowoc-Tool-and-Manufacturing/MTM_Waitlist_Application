using Core.Interfaces.Api;
using Core.Interfaces.Auth;
using Core.Constants.Auth;
using Core.Models.Auth;
using Core.Models.Shared;

namespace Services.Auth;

/// <summary>
/// Handles user authentication against the backend API and manages JWT storage.
/// The token is stored in the platform's secure vault via MAUI SecureStorage
/// (Android Keystore on Android, Windows DPAPI on Windows) — never in plaintext.
/// </summary>
public sealed class Service_Auth : IService_Auth
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initialises a new instance with the supplied API client.
    /// </summary>
    public Service_Auth(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_AuthToken>> LoginAsync(
        string username, string password, CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<Model_AuthToken>(
            "/api/auth/login",
            new { username, password },
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            return Model_Dao_Result<Model_AuthToken>.Failure(result.ErrorMessage);
        }

        await StoreSessionAsync(result.Data);
        return result;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_AuthToken>> AutoLoginAsync(
        string windowsUsername, CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<Model_AuthToken>(
            "/api/auth/auto-login",
            new { windowsUsername },
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            return Model_Dao_Result<Model_AuthToken>.Failure(result.ErrorMessage);
        }

        await StoreSessionAsync(result.Data);
        return result;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_Auth_LoginMode>> CheckWorkstationAsync(
        string windowsUsername, CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<Model_Auth_LoginMode>(
            "/api/auth/check-workstation",
            new { windowsUsername },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Model_Dao_Result> LogoutAsync()
    {
        try
        {
            ClearStoredSession();
            return Task.FromResult(Model_Dao_Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Model_Dao_Result.Failure($"Logout failed: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_AuthToken>> RefreshTokenAsync(
        CancellationToken cancellationToken = default)
    {
        string refreshToken = string.Empty;

        try
        {
            // SecureStorage on WinUI requires the UI thread (WinRT PasswordVault).
            refreshToken = await MainThread.InvokeOnMainThreadAsync(
                () => SecureStorage.GetAsync(Constants_AuthStorage.RefreshTokenKey)) ?? string.Empty;
        }
        catch
        {
            refreshToken = string.Empty;
        }

        var result = await _apiClient.PostAsync<Model_AuthToken>(
            "/api/auth/refresh",
            new { refreshToken },
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            return Model_Dao_Result<Model_AuthToken>.Failure(result.ErrorMessage);
        }

        await StoreSessionAsync(result.Data);
        return result;
    }

    private static async Task StoreSessionAsync(Model_AuthToken token)
    {
        // SecureStorage on WinUI requires the UI thread (WinRT PasswordVault).
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

    private static void ClearStoredSession()
    {
        // SecureStorage.Remove is synchronous but still requires the UI thread on WinUI.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SecureStorage.Remove(Constants_AuthStorage.AuthTokenKey);
            SecureStorage.Remove(Constants_AuthStorage.RefreshTokenKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthTokenExpiresAtKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthRoleKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthUsernameKey);
            SecureStorage.Remove(Constants_AuthStorage.AuthDisplayNameKey);
        });
    }
}
