namespace Core.Models.Auth;

/// <summary>
/// Holds the JWT bearer token and its expiry timestamp.
/// Stored in the platform's secure vault via SecureStorage — never in plaintext.
/// </summary>
public sealed record Model_AuthToken
{
    /// <summary>The authenticated user's numeric identifier.</summary>
    public int UserId { get; init; }

    /// <summary>The raw JWT bearer token string.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>The raw refresh token used to obtain a new access token.</summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>UTC timestamp at which the token expires and must be refreshed.</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>The authenticated application username.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>The authenticated user's display name.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>The authenticated user's role.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Returns <see langword="true"/> when the token has passed its expiry time.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
