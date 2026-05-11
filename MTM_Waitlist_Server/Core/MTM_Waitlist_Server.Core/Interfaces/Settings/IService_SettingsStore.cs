using MTM_Waitlist_Server.Core.Models.Settings;

namespace MTM_Waitlist_Server.Core.Interfaces.Settings;

/// <summary>Reads and persists the server settings JSON file.</summary>
public interface IService_SettingsStore
{
    /// <summary>Returns the current settings, loading from disk on first access.</summary>
    ServerSettings Get();

    /// <summary>Persists the supplied settings to disk.</summary>
    Task SaveAsync(ServerSettings settings);

    /// <summary>Reloads settings from disk, discarding any in-memory state.</summary>
    Task ReloadAsync();
}
