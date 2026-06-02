using MTM_Waitlist_Server.Admin.Logging;
using MTM_Waitlist_Server.Core.Interfaces.FirstRun;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Core.Models.Migration;
using MTM_Waitlist_Server.Core.Models.Settings;
using MySqlConnector;
using System;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Probes MySQL on launch to determine if first-run setup is required.
/// All queries use the updater credentials from <see cref="IService_SettingsStore"/>.
/// </summary>
internal sealed class Service_FirstRun : IService_FirstRun
{
    private const string AutoSeedPasswordHash = "password";

    private readonly IService_SettingsStore _settingsStore;
    private readonly IService_Migration _migration;

    /// <summary>Initialises a new instance of <see cref="Service_FirstRun"/>.</summary>
    public Service_FirstRun(IService_SettingsStore settingsStore, IService_Migration migration)
    {
        _settingsStore = settingsStore;
        _migration = migration;
    }

    /// <inheritdoc/>
    public async Task<Model_FirstRunProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var db = _settingsStore.Get().Database;

        StartupLogger.Info($"Probe step 1: attempting TCP connection to {db.Host}:{db.Port} " +
            $"as '{db.UpdaterUsername}' (timeout={db.ConnectionTimeout}s).");

        var csb = new MySqlConnectionStringBuilder
        {
            Server = db.Host,
            Port = (uint)db.Port,
            UserID = db.UpdaterUsername,
            Password = db.UpdaterPassword,
            ConnectionTimeout = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout = (uint)db.CommandTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode = MySqlSslMode.Preferred,
            // Do not specify a database yet — we are checking whether it exists.
        };

        // Step 1 — can we connect at all?
        MySqlConnection conn;
        try
        {
            conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync(cancellationToken);
            StartupLogger.Info($"Probe step 1: TCP connection established (ServerVersion={conn.ServerVersion}).");
        }
        catch (MySqlException ex) when (IsNetworkError(ex))
        {
            // Genuinely unreachable — wrong host, port closed, firewall, etc.
            StartupLogger.Warn($"Probe step 1: NETWORK ERROR — {ex.GetType().Name} (Number={ex.Number}): {ex.Message}");
            StartupLogger.Info("Probe result: MySqlUnreachable (network/firewall/host failure).");
            return Model_FirstRunProbeResult.Unreachable(ex.Message);
        }
        catch (Exception ex)
        {
            // Server is reachable but the updater credentials are wrong or the
            // database/user hasn't been created yet.  Treat this as SchemaMissing
            // so the wizard opens at Step 1 to let the user configure credentials.
            StartupLogger.Warn($"Probe step 1: AUTH/CREDENTIAL ERROR — {ex.GetType().Name}: {ex.Message}. Treating as SchemaMissing so wizard can re-collect credentials.");
            return Model_FirstRunProbeResult.SchemaMissing(ex.Message);
        }

        await using (conn)
        {
            // Step 2 — does the schema + Users table exist?
            StartupLogger.Info($"Probe step 2: checking for database '{db.DatabaseName}' and 'Users' table in information_schema.");
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

            StartupLogger.Info($"Probe step 2: information_schema row count for '{db.DatabaseName}'.Users = {schemaExists}.");

            if (schemaExists == 0)
            {
                StartupLogger.Info("Probe result: SchemaMissing (Users table not found).");
                return Model_FirstRunProbeResult.SchemaMissing();
            }

            // Step 3 — does at least one active Admin or Developer user exist?
            StartupLogger.Info($"Probe step 3: counting active Admin/Developer users in '{db.DatabaseName}'.Users.");
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

            StartupLogger.Info($"Probe step 3: active Admin/Developer count = {adminCount}.");

            if (adminCount == 0)
            {
                StartupLogger.Warn("Probe step 3: no active Admin/Developer user found. Attempting to auto-seed the current Windows user as Admin.");

                try
                {
                    await AutoSeedCurrentWindowsAdminAsync(conn, db.DatabaseName, cancellationToken);
                    adminCount = await ScalarAsync<long>(
                        conn,
                        $"""
                        SELECT COUNT(*)
                        FROM `{db.DatabaseName}`.`Users`
                        WHERE `IsActive` = 1
                          AND `Role` IN ('Admin', 'Developer')
                        """,
                        null,
                        cancellationToken);

                    StartupLogger.Info($"Probe step 3: active Admin/Developer count after auto-seed = {adminCount}.");
                }
                catch (Exception ex)
                {
                    StartupLogger.Warn($"Probe step 3: auto-seeding current Windows admin failed — {ex.GetType().Name}: {ex.Message}");
                }

                if (adminCount == 0)
                {
                    StartupLogger.Info("Probe result: NoAdminUser (schema exists but no active admin found). ");
                    return Model_FirstRunProbeResult.NoAdminUser();
                }
            }
        }

        StartupLogger.Info("Probe result: Ready (all checks passed).");
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
            Server = host,
            Port = (uint)port,
            UserID = adminUsername,
            Password = adminPassword,
            ConnectionTimeout = 10,
        };

        try
        {
            await using var conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync(cancellationToken);

            var settings = _settingsStore.Get();

            // If no password provided, use the reversed username as default.
            var effectiveAppPassword = string.IsNullOrWhiteSpace(appDbPassword)
                ? DatabaseSettings.ComputeReversedPassword(appDbUsername)
                : appDbPassword;

            // 1. Create the database if it doesn't exist.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    $"CREATE DATABASE IF NOT EXISTS `{databaseName}` " +
                    "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            var appUserExists = await MySqlUserExistsAsync(conn, appDbUsername, cancellationToken);

            // 2. Create or reset the application MySQL user, then grant privileges.
            if (appUserExists)
            {
                await using var alterUserCommand = conn.CreateCommand();
                alterUserCommand.CommandText =
                    $"ALTER USER {FormatMySqlUserAccount(appDbUsername)} IDENTIFIED BY @appPwd";
                alterUserCommand.Parameters.AddWithValue("@appPwd", effectiveAppPassword);
                await alterUserCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                await using var createUserCommand = conn.CreateCommand();
                createUserCommand.CommandText =
                    $"CREATE USER {FormatMySqlUserAccount(appDbUsername)} IDENTIFIED BY @appPwd";
                createUserCommand.Parameters.AddWithValue("@appPwd", effectiveAppPassword);
                await createUserCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    $"GRANT ALL PRIVILEGES ON `{databaseName}`.* TO {FormatMySqlUserAccount(appDbUsername)}";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "FLUSH PRIVILEGES";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 3. Persist the verified connection settings so the rest of the app can use them.
            settings.Database.Host = host;
            settings.Database.Port = port;
            settings.Database.DatabaseName = databaseName;
            settings.Database.UpdaterUsername = appDbUsername;
            settings.Database.UpdaterPassword = effectiveAppPassword;
            await _settingsStore.SaveAsync(settings);

            return null; // null = success
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <inheritdoc/>
    public async Task<MigrationResult> BootstrapSchemaAsync(
        IProgress<string> progress,
        CancellationToken cancellationToken = default)
    {
        var migrationProgress = new Progress<MigrationProgress>(step =>
            progress.Report($"[{step.Version}] {step.Message}"));

        return await _migration.ApplyPendingMigrationsAsync(migrationProgress, cancellationToken);
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
            Server = db.Host,
            Port = (uint)db.Port,
            Database = db.DatabaseName,
            UserID = db.UpdaterUsername,
            Password = db.UpdaterPassword,
            ConnectionTimeout = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout = (uint)db.CommandTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode = MySqlSslMode.Preferred,
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

        cmd.Parameters.AddWithValue("@win", windowsUsername);
        cmd.Parameters.AddWithValue("@app", appUsername);
        cmd.Parameters.AddWithValue("@hash", passwordHash);
        cmd.Parameters.AddWithValue("@display", displayName);
        cmd.Parameters.AddWithValue("@role", role);

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

    private static async Task AutoSeedCurrentWindowsAdminAsync(
        MySqlConnection conn,
        string databaseName,
        CancellationToken ct)
    {
        var windowsUsername = WindowsIdentity.GetCurrent().Name;
        if (string.IsNullOrWhiteSpace(windowsUsername))
        {
            throw new InvalidOperationException("Current Windows username could not be resolved.");
        }

        var accountName = ExtractAccountName(windowsUsername);
        var displayName = ResolveWindowsDisplayName(windowsUsername, accountName);
        var appUsername = BuildAppUsername(accountName);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $"""
            INSERT INTO `{databaseName}`.`Users`
                (`WindowsUsername`, `Username`, `PasswordHash`, `DisplayName`, `Role`, `IsActive`)
            VALUES
                (@win, @app, @hash, @display, 'Admin', 1)
            ON DUPLICATE KEY UPDATE
                `WindowsUsername` = VALUES(`WindowsUsername`),
                `DisplayName` = VALUES(`DisplayName`),
                `Role` = 'Admin',
                `IsActive` = 1
            """;
        cmd.Parameters.AddWithValue("@win", windowsUsername);
        cmd.Parameters.AddWithValue("@app", appUsername);
        cmd.Parameters.AddWithValue("@hash", AutoSeedPasswordHash);
        cmd.Parameters.AddWithValue("@display", displayName);

        await cmd.ExecuteNonQueryAsync(ct);
        StartupLogger.Info($"Auto-seeded current Windows user '{windowsUsername}' as Admin in '{databaseName}'.Users.");
    }

    private static string ExtractAccountName(string windowsUsername)
    {
        var separatorIndex = windowsUsername.LastIndexOf('\\');
        return separatorIndex >= 0 && separatorIndex < windowsUsername.Length - 1
            ? windowsUsername[(separatorIndex + 1)..]
            : windowsUsername;
    }

    private static string BuildAppUsername(string accountName)
    {
        var sanitized = Regex.Replace(accountName.Trim(), "[^A-Za-z0-9._-]", "_", RegexOptions.CultureInvariant);
        return string.IsNullOrWhiteSpace(sanitized)
            ? Environment.UserName
            : sanitized;
    }

    private static string ResolveWindowsDisplayName(string windowsUsername, string fallbackAccountName)
    {
        var displayName = Environment.GetEnvironmentVariable("USERNAME");
        if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(displayName, fallbackAccountName, StringComparison.OrdinalIgnoreCase))
        {
            return displayName;
        }

        return string.IsNullOrWhiteSpace(fallbackAccountName)
            ? windowsUsername
            : fallbackAccountName;
    }

    private static async Task<bool> MySqlUserExistsAsync(
        MySqlConnection conn,
        string username,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM mysql.user WHERE User = @user";
        cmd.Parameters.AddWithValue("@user", username);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }

    private static string FormatMySqlUserAccount(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Application MySQL username cannot be empty.");
        }

        return $"'{username.Replace("'", "''", StringComparison.Ordinal)}'@'%'";
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
        const int erBadHostError = 1042;
        const int crConnHostError = 2003;
        const int crUnknownHost = 2005;
        const int crServerLost = 2013;
        const int connectorTimeout = 9000;

        return ex.Number is erBadHostError or crConnHostError
                         or crUnknownHost or crServerLost or connectorTimeout
            || ex.InnerException is System.Net.Sockets.SocketException
            || ex.InnerException is TimeoutException;
    }
}
