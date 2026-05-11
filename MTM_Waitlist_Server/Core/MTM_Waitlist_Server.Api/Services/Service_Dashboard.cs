using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Dashboard;
using MySqlConnector;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Api.Services;

/// <summary>
/// Implements <see cref="IService_Dashboard"/> using MySqlConnector.
/// All queries use the <c>waitlist_admin_dbupdater</c> credentials from settings,
/// as that user holds the PROCESS and KILL privileges required by the dashboard.
/// </summary>
public sealed class Service_Dashboard : IService_Dashboard
{
    private readonly IService_SettingsStore _settings;
    private readonly IActivityLogBuffer _logBuffer;

    public Service_Dashboard(IService_SettingsStore settings, IActivityLogBuffer logBuffer)
    {
        _settings = settings;
        _logBuffer = logBuffer;
    }

    // ── Connection helper ─────────────────────────────────────────────────────

    private MySqlConnection OpenUpdaterConnection()
    {
        var db = _settings.Get().Database;
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
        return new MySqlConnection(csb.ConnectionString);
    }

    // ── IService_Dashboard ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<long> GetDatabaseSizeBytesAsync(CancellationToken ct = default)
    {
        await using var conn = OpenUpdaterConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COALESCE(SUM(data_length + index_length), 0)
            FROM information_schema.TABLES
            WHERE table_schema = DATABASE();
            """;

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    /// <inheritdoc/>
    public async Task<(int Active, int Max)> GetConnectionCountsAsync(CancellationToken ct = default)
    {
        await using var conn = OpenUpdaterConnection();
        await conn.OpenAsync(ct);

        int active = 0;
        int max = 0;

        await using var cmdActive = conn.CreateCommand();
        cmdActive.CommandText = "SHOW GLOBAL STATUS LIKE 'Threads_connected';";
        await using (var r = await cmdActive.ExecuteReaderAsync(ct))
        {
            if (await r.ReadAsync(ct))
            {
                active = Convert.ToInt32(r.GetString(1));
            }
        }

        await using var cmdMax = conn.CreateCommand();
        cmdMax.CommandText = "SHOW VARIABLES LIKE 'max_connections';";
        await using (var r = await cmdMax.ExecuteReaderAsync(ct))
        {
            if (await r.ReadAsync(ct))
            {
                max = Convert.ToInt32(r.GetString(1));
            }
        }

        return (active, max);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Model_TableStat>> GetTableStatsAsync(CancellationToken ct = default)
    {
        await using var conn = OpenUpdaterConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT table_name, table_rows, data_length, update_time
            FROM information_schema.TABLES
            WHERE table_schema = DATABASE()
            ORDER BY table_name;
            """;

        var results = new List<Model_TableStat>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new Model_TableStat(
                TableName: reader.GetString(0),
                EstimatedRows: reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1)),
                DataBytes: reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2)),
                LastUpdated: reader.IsDBNull(3) ? null : reader.GetDateTime(3)));
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Model_TableStat>> GetExactTableStatsAsync(CancellationToken ct = default)
    {
        // Get table names first, then COUNT(*) each individually.
        var stats = await GetTableStatsAsync(ct);
        var results = new List<Model_TableStat>(stats.Count);

        await using var conn = OpenUpdaterConnection();
        await conn.OpenAsync(ct);

        foreach (var stat in stats)
        {
            await using var cmd = conn.CreateCommand();
            // Table name is from information_schema — safe to embed directly.
            cmd.CommandText = $"SELECT COUNT(*) FROM `{stat.TableName}`;";
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            results.Add(stat with { EstimatedRows = count });
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Model_ActiveConnection>> GetActiveConnectionsAsync(CancellationToken ct = default)
    {
        await using var conn = OpenUpdaterConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SHOW FULL PROCESSLIST;";

        var results = new List<Model_ActiveConnection>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var user = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

            // Skip the replication and system threads.
            if (user is "system user" or "event_scheduler")
            {
                continue;
            }

            var host = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

            // Strip port from host (e.g. "10.0.1.15:52341" → "10.0.1.15").
            var colonIdx = host.LastIndexOf(':');
            if (colonIdx > 0)
            {
                host = host[..colonIdx];
            }

            var state = reader.IsDBNull(6) ? null : reader.GetString(6);
            if (state is { Length: > 40 })
            {
                state = state[..40];
            }

            results.Add(new Model_ActiveConnection(
                ThreadId: Convert.ToInt64(reader.GetValue(0)),
                User: user,
                Host: host,
                Command: reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                TimeSeconds: reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5)),
                State: state,
                IsCritical: Model_ActiveConnection.DetectCritical(user, host)));
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task KillConnectionAsync(long threadId, CancellationToken ct = default)
    {
        // Verify the thread is not a critical internal connection before issuing KILL.
        // Re-query the process list so the guard cannot be bypassed by a stale client snapshot.
        var current = await GetActiveConnectionsAsync(ct);
        var thread = current.FirstOrDefault(c => c.ThreadId == threadId);

        if (thread is null)
        {
            // Thread already gone — nothing to do.
            return;
        }

        if (thread.IsCritical)
        {
            throw new InvalidOperationException(
                $"Thread {threadId} belongs to critical user '{thread.User}' and cannot be killed. " +
                "Killing this connection would crash the admin application.");
        }

        await using var conn = OpenUpdaterConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        // threadId comes from SHOW FULL PROCESSLIST — numeric, not user input.
        cmd.CommandText = $"KILL {threadId};";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<(int Total, int Open, int Overdue)> GetWaitlistCountsAsync(CancellationToken ct = default)
    {
        await using var conn = OpenUpdaterConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                COUNT(*),
                SUM(CASE WHEN Status NOT IN ('Completed','Cancelled') THEN 1 ELSE 0 END),
                SUM(CASE WHEN Status NOT IN ('Completed','Cancelled')
                          AND RequestedAt < NOW() - INTERVAL 1 HOUR THEN 1 ELSE 0 END)
            FROM WaitlistEntries
            WHERE DATE(RequestedAt) = CURDATE();
            """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return (
                Total: Convert.ToInt32(reader.GetValue(0)),
                Open: reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1)),
                Overdue: reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)));
        }

        return (0, 0, 0);
    }

    // ── Log buffer passthrough ─────────────────────────────────────────────────

    /// <summary>Returns the recent activity snapshot from the in-process ring buffer.</summary>
    public IReadOnlyList<LogEntry> GetRecentActivity() => _logBuffer.GetSnapshot();
}
