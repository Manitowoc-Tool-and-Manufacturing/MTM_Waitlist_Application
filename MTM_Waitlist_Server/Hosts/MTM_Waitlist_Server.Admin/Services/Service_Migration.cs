using MTM_Waitlist_Server.Admin.Helpers;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Migration;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Implements the incremental migration runner described in DATABASE-06.
/// <para>
/// Migration files are read from the folder configured in
/// <see cref="Core.Models.Settings.MigrationsSettings.MigrationFolder"/> (relative to the
/// executable directory when the path is not rooted).  Procedures, triggers, and indexes are
/// always re-run because they are idempotent (<c>DROP IF EXISTS / CREATE</c>).
/// </para>
/// </summary>
internal sealed class Service_Migration : IService_Migration
{
    private static readonly Regex MigrationFileRegex =
        new(@"^V(\d{3})__(.+)\.sql$", RegexOptions.IgnoreCase);

    private readonly IService_SettingsStore _settingsStore;

    public Service_Migration(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <inheritdoc />
    public async Task<(MigrationSchemaState State, string? ErrorMessage)> DetectSchemaStateAsync(
        CancellationToken ct = default)
    {
        try
        {
            await using var conn = OpenConnection();
            await conn.OpenAsync(ct);

            // --- State 1: SchemaVersions already exists → Ready ---
            if (await TableExistsAsync(conn, "SchemaVersions", ct))
            {
                return (MigrationSchemaState.Ready, null);
            }

            // SchemaVersions is missing. Check whether core application tables exist.
            // If they do, the database was bootstrapped before the tracking system was added.
            bool coreTablesExist = await TableExistsAsync(conn, "Users", ct);

            if (coreTablesExist)
            {
                // --- State 2: Pre-existing schema — create tracking table and backfill ---
                await EnsureSchemaVersionsTableAsync(conn, ct);
                return (MigrationSchemaState.PreExistingSchema, null);
            }

            // --- State 3: Empty database — no tables at all ---
            return (MigrationSchemaState.FreshDatabase, null);
        }
        catch (Exception ex)
        {
            return (MigrationSchemaState.Error, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SchemaVersionsTableExistsAsync(CancellationToken ct = default)
    {
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await TableExistsAsync(conn, "SchemaVersions", ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken ct = default)
    {
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Version, Description, Script, AppliedAt, AppliedBy, " +
            "ExecutionMs, Success, ErrorMessage " +
            "FROM SchemaVersions ORDER BY Id";

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<AppliedMigration>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new AppliedMigration(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetBoolean(6),
                reader.IsDBNull(7) ? null : reader.GetString(7)));
        }
        return list;
    }

    /// <inheritdoc />
    public IReadOnlyList<PendingMigration> GetPendingMigrations()
    {
        var folder = ResolvePath(_settingsStore.Get().Migrations.MigrationFolder);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        // Applied versions to exclude — fire-and-forget; if DB is unavailable we still list files.
        HashSet<string> applied = [];
        try
        {
            applied = GetAppliedVersionsSync();
        }
        catch { /* offline — show all files as pending */ }

        return Directory.EnumerateFiles(folder, "V*.sql")
            .Select(path => (path, match: MigrationFileRegex.Match(Path.GetFileName(path))))
            .Where(t => t.match.Success)
            .Select(t => new PendingMigration(
                Version: $"V{t.match.Groups[1].Value}",
                VersionNumber: int.Parse(t.match.Groups[1].Value),
                Description: t.match.Groups[2].Value.Replace('_', ' '),
                Script: Path.GetFileName(t.path),
                FilePath: t.path))
            .Where(m => !applied.Contains(m.Version))
            .OrderBy(m => m.VersionNumber)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MigrationResult> ApplyPendingMigrationsAsync(
        IProgress<MigrationProgress> progress,
        CancellationToken ct = default)
    {
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        // Ensure the tracking table exists and backfill before computing pending list.
        // Order matters: GetPendingMigrations() filters by applied versions, so the
        // tracking table must be populated first or already-applied migrations re-run.
        await EnsureSchemaVersionsTableAsync(conn, ct);

        var pending = GetPendingMigrations();
        int applied = 0;

        foreach (var migration in pending)
        {
            ct.ThrowIfCancellationRequested();
            progress.Report(new MigrationProgress(migration.Version,
                $"Applying {migration.Script}…", false));

            var sql = await File.ReadAllTextAsync(migration.FilePath, ct);
            var checksum = ComputeSha256(sql);
            var sw = Stopwatch.StartNew();
            string? error = null;

            try
            {
                await SqlScriptRunner.RunFileScriptAsync(conn, sql, ct);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                await RecordMigrationAsync(conn, migration, checksum, (int)sw.ElapsedMilliseconds,
                    success: false, error, ct);

                return new MigrationResult(false, applied, migration.Version, error);
            }

            sw.Stop();
            await RecordMigrationAsync(conn, migration, checksum, (int)sw.ElapsedMilliseconds,
                success: true, null, ct);

            applied++;
            progress.Report(new MigrationProgress(migration.Version,
                $"✅ {migration.Script} applied in {sw.ElapsedMilliseconds} ms", true));
        }

        return new MigrationResult(true, applied, null, null);
    }

    /// <inheritdoc />
    public async Task<RerunResult> RerunIdempotentObjectsAsync(
        IProgress<MigrationProgress> progress,
        CancellationToken ct = default)
    {
        var settings = _settingsStore.Get().Migrations;
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        int procs = await RunFolderAsync(conn,
            ResolvePath(settings.ProceduresFolder), "procedure", progress, ct);
        int triggers = await RunFolderAsync(conn,
            ResolvePath(settings.TriggersFolder), "trigger", progress, ct);
        int indexes = await RunFolderAsync(conn,
            ResolvePath(settings.IndexesFolder), "index", progress, ct);

        return new RerunResult(procs, triggers, indexes, []);
    }

    /// <inheritdoc />
    public string PreviewMigrationSql(string version)
    {
        var folder = ResolvePath(_settingsStore.Get().Migrations.MigrationFolder);
        if (!Directory.Exists(folder))
        {
            return string.Empty;
        }

        var file = Directory.EnumerateFiles(folder, $"{version}__*.sql").FirstOrDefault();
        return file is not null ? File.ReadAllText(file) : string.Empty;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates the SchemaVersions tracking table if it does not already exist,
    /// then backfills any migration files whose SQL has already been applied to the
    /// live schema (determined by checking whether the objects they create exist).
    /// This prevents "table already exists" errors on the very first migration run
    /// against a database that was bootstrapped before the migration system existed.
    /// </summary>
    private async Task EnsureSchemaVersionsTableAsync(MySqlConnection conn, CancellationToken ct)
    {
        // 1. Create the table — IF NOT EXISTS makes this safe to call every time.
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS SchemaVersions (
                    Id            INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    Version       VARCHAR(20)  NOT NULL,
                    Description   VARCHAR(255) NOT NULL,
                    Script        VARCHAR(255) NOT NULL,
                    Checksum      VARCHAR(64)  NOT NULL,
                    AppliedAt     DATETIME     NOT NULL,
                    AppliedBy     VARCHAR(100) NOT NULL,
                    ExecutionMs   INT          NOT NULL,
                    Success       TINYINT(1)   NOT NULL DEFAULT 1,
                    ErrorMessage  TEXT         NULL,
                    UNIQUE KEY uq_schema_version (Version)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """;
            await cmd.ExecuteNonQueryAsync(ct);
        }

        // 2. Backfill migrations whose effects are already present in the live schema.
        //    We check each known bootstrap migration individually so we never re-run SQL
        //    that would fail with "table/column already exists".
        var db = _settingsStore.Get().Database;
        var folder = ResolvePath(_settingsStore.Get().Migrations.MigrationFolder);

        await BackfillIfAppliedAsync(conn, folder,
            version:     "V001",
            description: "Initial_Schema",
            script:      "V001__Initial_Schema.sql",
            // V001 creates the Users table — if it exists, V001 is already applied.
            probeQuery:  "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Users'",
            ct);

        await BackfillIfAppliedAsync(conn, folder,
            version:     "V002",
            description: "Add_SchemaVersions_Table",
            script:      "V002__Add_SchemaVersions_Table.sql",
            // V002 creates SchemaVersions — if it already had rows OR we just created it
            // above (empty), treat it as applied only when the Users table also exists
            // (meaning V001 ran), which is always true here because we checked V001 first.
            probeQuery:  "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SchemaVersions'",
            ct);
    }

    /// <summary>
    /// Inserts an <c>INSERT IGNORE</c> backfill row for <paramref name="version"/> when
    /// <paramref name="probeQuery"/> returns a non-zero count, indicating the migration's
    /// effects are already present in the live schema.
    /// </summary>
    private static async Task BackfillIfAppliedAsync(
        MySqlConnection conn,
        string folder,
        string version,
        string description,
        string script,
        string probeQuery,
        CancellationToken ct)
    {
        // Check whether the target object already exists.
        await using var probeCmd = conn.CreateCommand();
        probeCmd.CommandText = probeQuery;
        var count = Convert.ToInt32(await probeCmd.ExecuteScalarAsync(ct));
        if (count == 0)
        {
            return; // Object not present — migration hasn't run; leave it as pending.
        }

        // Compute checksum from the file if it exists, otherwise use a sentinel.
        var filePath = Path.Combine(folder, script);
        var checksum = File.Exists(filePath)
            ? ComputeSha256(await File.ReadAllTextAsync(filePath, ct))
            : "bootstrapped-pre-tracking";

        await using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText =
            """
            INSERT IGNORE INTO SchemaVersions
                (Version, Description, Script, Checksum, AppliedAt, AppliedBy, ExecutionMs, Success)
            VALUES
                (@v, @d, @s, @c, UTC_TIMESTAMP(), 'system', 0, 1)
            """;
        insertCmd.Parameters.AddWithValue("@v", version);
        insertCmd.Parameters.AddWithValue("@d", description);
        insertCmd.Parameters.AddWithValue("@s", script);
        insertCmd.Parameters.AddWithValue("@c", checksum);
        await insertCmd.ExecuteNonQueryAsync(ct);
    }

    private MySqlConnection OpenConnection()
    {
        var db = _settingsStore.Get().Database;

        if (string.IsNullOrWhiteSpace(db.Host))
            throw new InvalidOperationException("Database host is not configured. Open Settings and save a valid connection.");
        if (string.IsNullOrWhiteSpace(db.UpdaterUsername))
            throw new InvalidOperationException("Database username is not configured. Open Settings and save a valid connection.");
        if (string.IsNullOrWhiteSpace(db.DatabaseName))
            throw new InvalidOperationException("Database name is not configured. Open Settings and save a valid connection.");

        var csb = new MySqlConnectionStringBuilder
        {
            Server                  = db.Host,
            Port                    = (uint)db.Port,
            Database                = db.DatabaseName,
            UserID                  = db.UpdaterUsername,
            Password                = db.UpdaterPassword ?? string.Empty,
            ConnectionTimeout       = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout   = (uint)db.CommandTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode                 = MySqlSslMode.Preferred,
        };
        return new MySqlConnection(csb.ConnectionString);
    }

    private static async Task<bool> TableExistsAsync(MySqlConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.TABLES " +
            "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t";
        cmd.Parameters.AddWithValue("@t", tableName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }

    private HashSet<string> GetAppliedVersionsSync()
    {
        using var conn = OpenConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Version FROM SchemaVersions";
        using var reader = cmd.ExecuteReader();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            set.Add(reader.GetString(0));
        }
        return set;
    }

    private static async Task RecordMigrationAsync(
        MySqlConnection conn,
        PendingMigration migration,
        string checksum,
        int executionMs,
        bool success,
        string? error,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO SchemaVersions " +
            "(Version, Description, Script, Checksum, AppliedAt, AppliedBy, ExecutionMs, Success, ErrorMessage) " +
            "VALUES (@v, @d, @s, @c, @at, @by, @ms, @ok, @err)";
        cmd.Parameters.AddWithValue("@v", migration.Version);
        cmd.Parameters.AddWithValue("@d", migration.Description);
        cmd.Parameters.AddWithValue("@s", migration.Script);
        cmd.Parameters.AddWithValue("@c", checksum);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@by", Environment.UserName);
        cmd.Parameters.AddWithValue("@ms", executionMs);
        cmd.Parameters.AddWithValue("@ok", success ? 1 : 0);
        cmd.Parameters.AddWithValue("@err", (object?)error ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> RunFolderAsync(
        MySqlConnection conn,
        string folder,
        string kind,
        IProgress<MigrationProgress> progress,
        CancellationToken ct)
    {
        if (!Directory.Exists(folder))
        {
            return 0;
        }

        int count = 0;
        foreach (var file in Directory.EnumerateFiles(folder, "*.sql", SearchOption.AllDirectories)
                     .OrderBy(f => f))
        {
            ct.ThrowIfCancellationRequested();
            var sql = await File.ReadAllTextAsync(file, ct);
            await SqlScriptRunner.RunFileScriptAsync(conn, sql, ct);
            count++;
            progress.Report(new MigrationProgress(kind,
                $"✅ {kind}: {Path.GetFileName(file)}", true));
        }

        return count;
    }

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
