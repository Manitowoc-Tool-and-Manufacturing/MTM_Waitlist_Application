using MTM_Waitlist_Server.Core.Interfaces.ConnectedUsers;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.ConnectedUsers;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Retrieves connected-user data from the MySQL database by joining the
/// <c>Users</c> and <c>SharedWorkstations</c> tables.
/// </summary>
internal sealed class Service_ConnectedUsers : IService_ConnectedUsers
{
    private readonly IService_SettingsStore _settingsStore;

    /// <summary>Initialises a new instance of <see cref="Service_ConnectedUsers"/>.</summary>
    public Service_ConnectedUsers(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Model_ConnectedUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<Model_ConnectedUser>();

        await using var conn = await OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();

        // Left-join SharedWorkstations so personal-workstation users still appear.
        // A shared workstation is identified by the WindowsUsername matching a row in SharedWorkstations.
        cmd.CommandText = """
            SELECT
                u.Id,
                u.Username,
                u.DisplayName,
                u.Role,
                u.WindowsUsername,
                sw.MachineName   AS WorkstationName,
                u.IsActive,
                u.LastLoginAt,
                (sw.Id IS NOT NULL) AS IsSharedWorkstation
            FROM `Users` u
            LEFT JOIN `SharedWorkstations` sw
                ON sw.WindowsUsername = u.WindowsUsername
               AND sw.IsActive = 1
            WHERE u.IsActive = 1
            ORDER BY u.DisplayName
            """;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<Model_ConnectedUser?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT
                u.Id,
                u.Username,
                u.DisplayName,
                u.Role,
                u.WindowsUsername,
                sw.MachineName   AS WorkstationName,
                u.IsActive,
                u.LastLoginAt,
                (sw.Id IS NOT NULL) AS IsSharedWorkstation
            FROM `Users` u
            LEFT JOIN `SharedWorkstations` sw
                ON sw.WindowsUsername = u.WindowsUsername
               AND sw.IsActive = 1
            WHERE u.Id = @id
              AND u.IsActive = 1
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@id", userId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRow(reader) : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var db = _settingsStore.Get().Database;
        var csb = new MySqlConnectionStringBuilder
        {
            Server                  = db.Host,
            Port                    = (uint)db.Port,
            Database                = db.DatabaseName,
            UserID                  = db.UpdaterUsername,
            Password                = db.UpdaterPassword,
            ConnectionTimeout       = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout   = (uint)db.CommandTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode                 = MySqlSslMode.Preferred,
        };

        var conn = new MySqlConnection(csb.ConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    private static Model_ConnectedUser MapRow(MySqlDataReader reader)
    {
        DateTime? lastLogin = reader.IsDBNull(reader.GetOrdinal("LastLoginAt"))
            ? null
            : reader.GetDateTime("LastLoginAt");

        return new Model_ConnectedUser(
            UserId:             reader.GetInt32("Id"),
            Username:           reader.GetString("Username"),
            DisplayName:        reader.GetString("DisplayName"),
            Role:               reader.GetString("Role"),
            WindowsUsername:    reader.IsDBNull(reader.GetOrdinal("WindowsUsername"))
                                    ? null
                                    : reader.GetString("WindowsUsername"),
            WorkstationName:    reader.IsDBNull(reader.GetOrdinal("WorkstationName"))
                                    ? null
                                    : reader.GetString("WorkstationName"),
            IsSharedWorkstation: reader.GetBoolean("IsSharedWorkstation"),
            LastLoginAt:        lastLogin,
            IsActive:           reader.GetBoolean("IsActive"));
    }
}
