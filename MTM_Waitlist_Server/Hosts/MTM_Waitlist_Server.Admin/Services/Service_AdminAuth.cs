using MTM_Waitlist_Server.Core.Interfaces.Auth;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MySqlConnector;
using System;
using System.Security.Principal;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Authorises access to the admin application by looking up the current Windows username
/// in <c>mtm_waitlist.Users</c> and confirming the Role is <c>Admin</c> or <c>Developer</c>.
/// Uses the updater connection string (elevated credentials) because this check runs before
/// the API is started.
/// </summary>
internal sealed class Service_AdminAuth : IService_AdminAuth
{
    private static readonly string[] AllowedRoles = ["Admin", "Developer"];

    private readonly IService_SettingsStore _settingsStore;

    /// <summary>Initialises a new instance of <see cref="Service_AdminAuth"/>.</summary>
    public Service_AdminAuth(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthorisedAsync(string windowsUsername)
    {
        if (string.IsNullOrWhiteSpace(windowsUsername))
        {
            return false;
        }

        try
        {
            var db = _settingsStore.Get().Database;
            var csb = new MySqlConnectionStringBuilder
            {
                Server = db.Host,
                Port = (uint)db.Port,
                Database = db.DatabaseName,
                UserID = db.UpdaterUsername,
                Password = db.UpdaterPassword,
                ConnectionTimeout = (uint)db.ConnectionTimeout,
                DefaultCommandTimeout = (uint)db.CommandTimeout
            };

            await using var conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "CALL usp_Auth_GetUserByWindowsUsername(@p_WindowsUsername)";
            cmd.Parameters.AddWithValue("@p_WindowsUsername", windowsUsername);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                // No matching active user found.
                return false;
            }

            var role = reader["Role"]?.ToString() ?? string.Empty;
            return Array.IndexOf(AllowedRoles, role) >= 0;
        }
        catch
        {
            // If the database is unreachable at startup, deny access rather than crashing.
            return false;
        }
    }

    /// <summary>
    /// Returns the current Windows username in the format stored by the application
    /// (<c>DOMAIN\username</c> or plain <c>username</c>).
    /// </summary>
    public static string GetCurrentWindowsUsername()
    {
        return WindowsIdentity.GetCurrent().Name;
    }
}
