using Core.Models.Auth;
using Core.Models.Shared;

namespace Core.Interfaces.Auth;

/// <summary>
/// Contract for user authentication operations.
/// Tokens are stored in the platform secure vault — implementations must never
/// write credentials to plain files or shared preferences.
/// </summary>
public interface IService_Auth
{
    /// <summary>Authenticates the user against the API and stores the returned JWT in secure storage.</summary>
    Task<Model_Dao_Result<Model_AuthToken>> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>Attempts silent Windows-based login for a personal workstation.</summary>
    Task<Model_Dao_Result<Model_AuthToken>> AutoLoginAsync(string windowsUsername, CancellationToken cancellationToken = default);

    /// <summary>Determines whether the current workstation requires manual credential login.</summary>
    Task<Model_Dao_Result<Model_Auth_LoginMode>> CheckWorkstationAsync(string windowsUsername, CancellationToken cancellationToken = default);

    /// <summary>Removes the stored JWT from secure storage and invalidates the current session.</summary>
    Task<Model_Dao_Result> LogoutAsync();

    /// <summary>Requests a new JWT from the API using the current token before it expires.</summary>
    Task<Model_Dao_Result<Model_AuthToken>> RefreshTokenAsync(CancellationToken cancellationToken = default);
}
