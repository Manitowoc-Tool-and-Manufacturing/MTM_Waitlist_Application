namespace MTM_Waitlist_Server.Core.Interfaces.Window;

/// <summary>
/// Manages application window sizing for different scenarios.
/// </summary>
public interface IService_WindowSizer
{
    /// <summary>
    /// Applies the window size appropriate for the first-run setup wizard.
    /// </summary>
    void ApplyFirstRunSize();

    /// <summary>
    /// Applies the default window size for the normal admin shell.
    /// </summary>
    void ApplyNormalSize();

    /// <summary>
    /// Moves the window to the centre of the monitor it currently occupies.
    /// </summary>
    void CenterOnMonitor();
}
