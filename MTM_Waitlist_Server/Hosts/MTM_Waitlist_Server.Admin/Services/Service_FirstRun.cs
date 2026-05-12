using MTM_Waitlist_Server.Core.Interfaces.FirstRun;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MySqlConnector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Probes MySQL on launch to determine if first-run setup is required.
/// All queries use the updater credentials from <see cref="IService_SettingsStore"/>.
/// </summary>
internal sealed class Service_FirstRun : IService_FirstRun
{
    private readonly IService_SettingsStore _settingsStore;

    /// <summary>Initialises a new instance of <see cref="Service_FirstRun"/>.</summary>
    public Service_FirstRun(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <inheritdoc/>
    public async Task<Model_FirstRunProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var db = _settingsStore.Get().Database;
        var csb = new MySqlConnectionStringBuilder
        {
            Server                   = db.Host,
            Port                     = (uint)db.Port,
            UserID                   = db.UpdaterUsername,
            Password                 = db.UpdaterPassword,
            ConnectionTimeout        = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout    = (uint)db.CommandTimeout,
            AllowPublicKeyRetrieval  = true,
            SslMode                  = MySqlSslMode.Preferred,
            // Do not specify a database yet — we are checking whether it exists.
        };

        // Step 1 — can we connect at all?
        MySqlConnection conn;
        try
        {
            conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync(cancellationToken);
        }
        catch (MySqlException ex) when (IsNetworkError(ex))
        {
            // Genuinely unreachable — wrong host, port closed, firewall, etc.
            return Model_FirstRunProbeResult.Unreachable(ex.Message);
        }
        catch (Exception ex)
        {
            // Server is reachable but the updater credentials are wrong or the
            // database/user hasn't been created yet.  Treat this as SchemaMissing
            // so the wizard opens at Step 1 to let the user configure credentials.
            return Model_FirstRunProbeResult.SchemaMissing(ex.Message);
        }

        await using (conn)
        {
            // Step 2 — does the schema + Users table exist?
            var schemaExists = await ScalarAsync<long>(
                conn,
                """
                SELECT COUNT(*)
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = @schema
                  AND TABLE_NAME   = 'Users'
                """,
                new MySqlParameter("@schema", db.DatabaseName),
                cancellationToken);

            if (schemaExists == 0)
            {
                return Model_FirstRunProbeResult.SchemaMissing();
            }

            // Step 3 — does at least one active Admin or Developer user exist?
            var adminCount = await ScalarAsync<long>(
                conn,
                $"""
                SELECT COUNT(*)
                FROM `{db.DatabaseName}`.`Users`
                WHERE `IsActive` = 1
                  AND `Role` IN ('Admin', 'Developer')
                """,
                null,
                cancellationToken);

            if (adminCount == 0)
            {
                return Model_FirstRunProbeResult.NoAdminUser();
            }
        }

        return Model_FirstRunProbeResult.Ready();
    }

    /// <inheritdoc/>
    public async Task<bool> IsFirstRunRequiredAsync(CancellationToken cancellationToken = default)
    {
        var settings = _settingsStore.Get();
        if (settings.FirstRunComplete)
        {
            // Even if FirstRunComplete is set, still run probe so we can detect degraded mode.
            var probe = await ProbeAsync(cancellationToken);
            return probe.Status != FirstRunStatus.Ready;
        }

        var result = await ProbeAsync(cancellationToken);
        return result.Status != FirstRunStatus.Ready;
    }

    /// <inheritdoc/>
    public async Task MarkCompleteAsync(CancellationToken cancellationToken = default)
    {
        var settings = _settingsStore.Get();
        settings.FirstRunComplete = true;
        await _settingsStore.SaveAsync(settings);
    }

    /// <inheritdoc/>
    public async Task<string?> SetupDatabaseAsync(
        string host, int port, string databaseName,
        string adminUsername, string adminPassword,
        string appDbUsername, string appDbPassword,
        CancellationToken cancellationToken = default)
    {
        // Connect with the privileged (root / admin) credentials — no database selected yet.
        var csb = new MySqlConnectionStringBuilder
        {
            Server            = host,
            Port              = (uint)port,
            UserID            = adminUsername,
            Password          = adminPassword,
            ConnectionTimeout = 10,
        };

        try
        {
            await using var conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            // 1. Create the database if it doesn't exist.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    $"CREATE DATABASE IF NOT EXISTS `{databaseName}` " +
                    "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 2. Create the application MySQL user if it doesn't exist and grant privileges.
            //    Separate CREATE USER from GRANT so partial state doesn't leave the server broken.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    $"CREATE USER IF NOT EXISTS @appUser@'%' IDENTIFIED BY @appPwd";
                cmd.Parameters.AddWithValue("@appUser", appDbUsername);
                cmd.Parameters.AddWithValue("@appPwd",  appDbPassword);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    $"GRANT ALL PRIVILEGES ON `{databaseName}`.* TO @appUser@'%'";
                cmd.Parameters.AddWithValue("@appUser", appDbUsername);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "FLUSH PRIVILEGES";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 3. Persist the verified connection settings so the rest of the app can use them.
            var settings = _settingsStore.Get();
            settings.Database.Host            = host;
            settings.Database.Port            = port;
            settings.Database.DatabaseName    = databaseName;
            settings.Database.UpdaterUsername = appDbUsername;
            settings.Database.UpdaterPassword = appDbPassword;
            await _settingsStore.SaveAsync(settings);

            return null; // null = success
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <inheritdoc/>
    public async Task CreateFirstUserAsync(
        string windowsUsername,
        string appUsername,
        string displayName,
        string passwordHash,
        string role,
        CancellationToken cancellationToken = default)
    {
        var db = _settingsStore.Get().Database;
        var csb = new MySqlConnectionStringBuilder
        {
            Server   = db.Host,
            Port     = (uint)db.Port,
            Database = db.DatabaseName,
            UserID   = db.UpdaterUsername,
            Password = db.UpdaterPassword,
            ConnectionTimeout     = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout = (uint)db.CommandTimeout,
        };

        await using var conn = new MySqlConnection(csb.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO `Users`
                (`WindowsUsername`, `Username`, `PasswordHash`, `DisplayName`, `Role`,
                 `IsActive`, `CreatedAt`, `UpdatedAt`)
            VALUES
                (@win, @app, @hash, @display, @role,
                 1, UTC_TIMESTAMP(), UTC_TIMESTAMP())
            """;

        cmd.Parameters.AddWithValue("@win",     windowsUsername);
        cmd.Parameters.AddWithValue("@app",     appUsername);
        cmd.Parameters.AddWithValue("@hash",    passwordHash);
        cmd.Parameters.AddWithValue("@display", displayName);
        cmd.Parameters.AddWithValue("@role",    role);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<T> ScalarAsync<T>(
        MySqlConnection conn,
        string sql,
        MySqlParameter? parameter,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (parameter is not null)
        {
            cmd.Parameters.Add(parameter);
        }

        var result = await cmd.ExecuteScalarAsync(ct);
        return (T)Convert.ChangeType(result!, typeof(T));
    }

    /// <summary>
    /// Returns <c>true</c> when the exception represents a genuine network-level failure
    /// (host unreachable, port closed, DNS failure, connection timeout) rather than an
    /// authentication or schema error where the server <em>is</em> reachable.
    /// </summary>
    private static bool IsNetworkError(MySqlException ex)
    {
        // MySqlConnector error numbers that indicate the server is genuinely unreachable.
        // 1042 = ER_BAD_HOST_ERROR, 2003 = CR_CONN_HOST_ERROR, 2005 = CR_UNKNOWN_HOST,
        // 2013 = CR_SERVER_LOST, 9000 = connection timeout used by MySqlConnector.
        const int erBadHostError      = 1042;
        const int crConnHostError     = 2003;
        const int crUnknownHost       = 2005;
        const int crServerLost        = 2013;
        const int connectorTimeout    = 9000;

        return ex.Number is erBadHostError or crConnHostError
                         or crUnknownHost or crServerLost or connectorTimeout
            || ex.InnerException is System.Net.Sockets.SocketException
            || ex.InnerException is TimeoutException;
    }
}
