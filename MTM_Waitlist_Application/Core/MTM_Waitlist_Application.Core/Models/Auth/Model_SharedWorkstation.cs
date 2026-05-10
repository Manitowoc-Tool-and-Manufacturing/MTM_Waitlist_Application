namespace MTM_Waitlist_Application.Core.Models.Auth;

/// <summary>
/// Represents a shared workstation (kiosk or floor terminal) whose Windows username
/// is registered in the system as requiring manual credential login.
/// Personal workstations whose Windows username is <em>not</em> in this list will
/// trigger automatic login via the matched <c>Users.WindowsUsername</c>.
/// </summary>
public sealed class Model_SharedWorkstation
{
    /// <summary>Unique identifier for the shared workstation record.</summary>
    public int Id { get; set; }

    /// <summary>
    /// The Windows login name of the shared PC or kiosk
    /// (e.g., <c>MTMDOM\PRESS3-PC</c> or <c>PRESS3-PC</c>).
    /// </summary>
    public string WindowsUsername { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable label for the workstation displayed in the admin UI
    /// (e.g., "Press 3 Floor Terminal").
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>Admin notes about this workstation (location, supervisor, etc.).</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When <see langword="true"/>, this workstation forces credential login.
    /// When <see langword="false"/>, the workstation is treated as a personal machine.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when this record was last modified.</summary>
    public DateTime UpdatedAt { get; set; }
}
