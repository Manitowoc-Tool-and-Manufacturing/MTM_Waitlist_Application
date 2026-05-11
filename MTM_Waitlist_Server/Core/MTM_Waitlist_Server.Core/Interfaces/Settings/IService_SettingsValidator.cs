using MTM_Waitlist_Server.Core.Models.Settings;

namespace MTM_Waitlist_Server.Core.Interfaces.Settings;

/// <summary>Validates <see cref="ServerSettings"/> before they are persisted.</summary>
public interface IService_SettingsValidator
{
    /// <summary>
    /// Validates the supplied settings and returns a list of error messages.
    /// Returns an empty list when the settings are valid.
    /// </summary>
    IReadOnlyList<string> Validate(ServerSettings settings);
}
