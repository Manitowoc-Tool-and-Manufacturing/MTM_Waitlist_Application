using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Settings;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Reads and persists server settings from
/// <c>%ProgramData%\MTM\WaitlistServer\server-settings.json</c>.
/// Falls back to defaults when the file does not yet exist.
/// </summary>
internal sealed class Service_SettingsStore : IService_SettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "MTM", "WaitlistServer", "server-settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private ServerSettings _cached = new();
    private bool _loaded;

    /// <inheritdoc/>
    public ServerSettings Get()
    {
        if (!_loaded)
        {
            LoadFromDisk();
        }

        return _cached;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(ServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(SettingsPath, json);
        _cached = settings;
        _loaded = true;
    }

    /// <inheritdoc/>
    public async Task ReloadAsync()
    {
        _loaded = false;
        await Task.Run(LoadFromDisk);
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(SettingsPath))
        {
            // First run — use defaults; the admin can save after first launch.
            _cached = new ServerSettings();
            _loaded = true;
            return;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            _cached = JsonSerializer.Deserialize<ServerSettings>(json, JsonOptions) ?? new ServerSettings();
        }
        catch
        {
            // Corrupt file — fall back to defaults rather than crashing on launch.
            _cached = new ServerSettings();
        }

        _loaded = true;
    }
}
