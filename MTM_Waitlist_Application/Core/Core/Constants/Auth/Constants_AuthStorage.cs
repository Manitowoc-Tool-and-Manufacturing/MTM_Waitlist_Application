namespace Core.Constants.Auth;

/// <summary>
/// Secure storage keys used for persisted authentication session state.
/// </summary>
public static class Constants_AuthStorage
{
    /// <summary>Key for the current access token.</summary>
    public const string AuthTokenKey = "auth_token";

    /// <summary>Key for the current refresh token.</summary>
    public const string RefreshTokenKey = "refresh_token";

    /// <summary>Key for the current access-token expiry instant.</summary>
    public const string AuthTokenExpiresAtKey = "auth_token_expires_at";

    /// <summary>Key for the current authenticated role.</summary>
    public const string AuthRoleKey = "auth_role";

    /// <summary>Key for the authenticated user's application username.</summary>
    public const string AuthUsernameKey = "auth_username";

    /// <summary>Key for the authenticated user's display name (full name).</summary>
    public const string AuthDisplayNameKey = "auth_display_name";
}