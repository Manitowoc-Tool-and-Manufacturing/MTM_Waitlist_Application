namespace Core.Models.Auth;

/// <summary>
/// Describes whether the current workstation should use manual credential login
/// or silent Windows auto-login.
/// </summary>
public sealed class Model_Auth_LoginMode
{
    /// <summary>True when the current machine is configured as a shared workstation.</summary>
    public bool IsSharedWorkstation { get; init; }

    /// <summary>The Windows identity that was evaluated by the server.</summary>
    public string WindowsUsername { get; init; } = string.Empty;

    /// <summary>Optional friendly machine label returned for shared workstations.</summary>
    public string? MachineName { get; init; }
}