namespace MTM_Waitlist_Server.Core.Models.ConnectedUsers;

/// <summary>
/// Represents a user that has logged into the MAUI client, sourced from the
/// MySQL Users and SharedWorkstations tables.
/// </summary>
public record Model_ConnectedUser(
    int UserId,
    string Username,
    string DisplayName,
    string Role,
    string? WindowsUsername,
    string? WorkstationName,
    bool IsSharedWorkstation,
    DateTime? LastLoginAt,
    bool IsActive)
{
    /// <summary>First letter of the display name, used for avatar initials.</summary>
    public string Initial => DisplayName is { Length: > 0 } ? DisplayName[0].ToString() : "?";
}
