using MTM_Waitlist_Application.Core.Interfaces.Api;
using MTM_Waitlist_Application.Core.Interfaces.Auth;
using MTM_Waitlist_Application.Core.Models.Auth;
using MTM_Waitlist_Application.Core.Models.Shared;

namespace MTM_Waitlist_Application.Services.Auth;

/// <summary>
/// Handles user authentication against the backend API and manages JWT storage.
/// The token is stored in the platform's secure vault via MAUI SecureStorage
/// (Android Keystore on Android, Windows DPAPI on Windows) — never in plaintext.
/// </summary>
public sealed class Service_Auth : IService_Auth
{
    private readonly IApiClient _apiClient;

    /// <summary>Key used when reading/writing the JWT in SecureStorage.</summary>
    private const string AuthTokenKey = "auth_token";

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

        // Store JWT in platform secure vault — never write to Preferences or a plain file.
        await SecureStorage.SetAsync(AuthTokenKey, result.Data.Token);
        return result;
    }

    /// <inheritdoc/>
    public Task<Model_Dao_Result> LogoutAsync()
    {
        try
        {
            // Remove clears the key from the platform keystore.
            SecureStorage.Remove(AuthTokenKey);
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
        var result = await _apiClient.PostAsync<Model_AuthToken>(
            "/api/auth/refresh",
            new { },
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            return Model_Dao_Result<Model_AuthToken>.Failure(result.ErrorMessage);
        }

        await SecureStorage.SetAsync(AuthTokenKey, result.Data.Token);
        return result;
    }
}
