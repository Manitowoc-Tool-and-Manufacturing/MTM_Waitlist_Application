using Core.Interfaces.Api;
using Core.Interfaces.Auth;
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
    private const string ServiceUnavailableMessage =
        "The MTM Waitlist service is not available right now. Please try again in a moment or contact support.";

    private readonly IApiClient _apiClient;
    private readonly IService_AuthTokenStore _tokenStore;

    /// <summary>
    /// Initialises a new instance with the supplied API client.
    /// </summary>
    public Service_Auth(IApiClient apiClient, IService_AuthTokenStore tokenStore)
    {
        _apiClient = apiClient;
        _tokenStore = tokenStore;
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
            return Model_Dao_Result<Model_AuthToken>.Failure(GetUserFriendlyErrorMessage(result.ErrorMessage));
        }

        await _tokenStore.StoreSessionAsync(result.Data, cancellationToken);
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
            return Model_Dao_Result<Model_AuthToken>.Failure(GetUserFriendlyErrorMessage(result.ErrorMessage));
        }

        await _tokenStore.StoreSessionAsync(result.Data, cancellationToken);
        return result;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_Auth_LoginMode>> CheckWorkstationAsync(
        string windowsUsername, CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<Model_Auth_LoginMode>(
            "/api/auth/check-workstation",
            new { windowsUsername },
            cancellationToken);

        return result.IsSuccess
            ? result
            : Model_Dao_Result<Model_Auth_LoginMode>.Failure(GetUserFriendlyErrorMessage(result.ErrorMessage));
    }

    /// <inheritdoc/>
    public Task<Model_Dao_Result> LogoutAsync()
    {
        try
        {
            _ = _tokenStore.ClearSessionAsync();
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
            refreshToken = await _tokenStore.GetRefreshTokenAsync(cancellationToken);
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
            return Model_Dao_Result<Model_AuthToken>.Failure(GetUserFriendlyErrorMessage(result.ErrorMessage));
        }

        await _tokenStore.StoreSessionAsync(result.Data, cancellationToken);
        return result;
    }

    private static string GetUserFriendlyErrorMessage(string errorMessage)
    {
        if (IsConnectionFailure(errorMessage))
        {
            return ServiceUnavailableMessage;
        }

        return string.IsNullOrWhiteSpace(errorMessage)
            ? "Sign in could not be completed. Please try again."
            : errorMessage;
    }

    private static bool IsConnectionFailure(string errorMessage)
    {
        return errorMessage.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
            || errorMessage.Contains("Network error", StringComparison.OrdinalIgnoreCase);
    }
}
