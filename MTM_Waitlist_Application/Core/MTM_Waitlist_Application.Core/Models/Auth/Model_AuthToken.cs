namespace MTM_Waitlist_Application.Core.Models.Auth;

/// <summary>
/// Holds the JWT bearer token and its expiry timestamp.
/// Stored in the platform's secure vault via SecureStorage — never in plaintext.
/// </summary>
public sealed class Model_AuthToken
{
    /// <summary>The raw JWT bearer token string.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>UTC timestamp at which the token expires and must be refreshed.</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Returns <see langword="true"/> when the token has passed its expiry time.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
