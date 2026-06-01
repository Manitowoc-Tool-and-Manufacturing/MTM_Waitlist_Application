using MTM_Waitlist_Server.Admin.Helpers;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Migration;
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
/// Compares the checked-in SQL object definitions to the live database and applies
/// the objects that can be safely created, replaced, or repaired with a
/// deterministic backup-and-restore workflow.
/// </summary>
internal sealed class Service_Migration : IService_Migration
{
    private const string SchemaVersionsTableSqlPath = "database/schema/tables/System/SchemaVersions.sql";
    private const string SchemaVersionsPreExistingBootstrapSqlPath = "database/schema/data/System/SchemaVersions_PreExisting_Bootstrap.sql";

    private static readonly IReadOnlyDictionary<string, int> TableDependencyOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Users"] = 0,
        ["SchemaVersions"] = 1,
        ["SharedWorkstations"] = 2,
        ["RefreshTokens"] = 3,
        ["WaitlistEntries"] = 4,
        ["SetupTechDunnageTypeConfig"] = 5,
        ["WorkstationActiveJobs"] = 6,
        ["WorkstationJobHistory"] = 7,
        ["WorkOrderDunnageAssignments"] = 8,
        ["WorkOrderSubordinateParts"] = 9,
    };

    private static readonly Regex CreateIndexRegex =
        new(
            @"CREATE\s+(?:UNIQUE\s+)?INDEX\s+`?(?<name>[^`\s(]+)`?\s+ON\s+`?(?<table>[^`\s(]+)`?\s*\((?<columns>[^)]+)\)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CreateTriggerRegex =
        new(
            @"CREATE\s+(?:DEFINER\s*=\s*(?:`[^`]*`|'[^']*')\s*@\s*(?:`[^`]*`|'[^']*')\s+)?TRIGGER\s+`?(?<name>[^`\s]+)`?\s+(?<timing>BEFORE|AFTER)\s+(?<event>INSERT|UPDATE|DELETE)\s+ON\s+`?(?<table>[^`\s]+)`?\s+FOR\s+EACH\s+ROW\s+(?<body>BEGIN\b.*\bEND)\s*(?:\$\$|;)?",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CreateProcedureRegex =
        new(
            @"CREATE\s+(?:DEFINER\s*=\s*(?:`[^`]*`|'[^']*')\s*@\s*(?:`[^`]*`|'[^']*')\s+)?PROCEDURE\s+`?(?<name>[^`\s(]+)`?\s*(?<parameters>\(.*?\))\s*(?<body>BEGIN\b.*\bEND)\s*(?:\$\$|;)?",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

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

            bool schemaVersionsExists = await TableExistsAsync(conn, "SchemaVersions", ct);
            bool usersTableExists = await TableExistsAsync(conn, "Users", ct);

            if (schemaVersionsExists && usersTableExists)
            {
                return (MigrationSchemaState.Ready, null);
            }

            if (usersTableExists)
            {
                await EnsureSchemaVersionsTableAsync(conn, ct);
                return (MigrationSchemaState.PreExistingSchema, null);
            }

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

        if (!await TableExistsAsync(conn, "SchemaVersions", ct))
        {
            return [];
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Version, Description, Script, AppliedAt, AppliedBy, " +
            "ExecutionMs, Success, ErrorMessage " +
            "FROM SchemaVersions ORDER BY AppliedAt DESC, Id DESC";

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
    public async Task<IReadOnlyList<PendingMigration>> GetPendingMigrationsAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = OpenConnection();
            await conn.OpenAsync(ct);

            var pending = (await ScanDefinitionsAsync(conn, ct))
                .Where(item => item.Status != SchemaDefinitionStatus.Match)
                .OrderBy(item => item.Definition.Order)
                .Select(ToPendingMigration)
                .ToList();

            return pending;
        }
        catch
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<MigrationResult> ApplyPendingMigrationsAsync(
        IProgress<MigrationProgress> progress,
        CancellationToken ct = default)
    {
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var comparisons = await ScanDefinitionsAsync(conn, ct);
        var autoApplicable = comparisons
            .Where(item => item.Status != SchemaDefinitionStatus.Match && CanAutoApply(item))
            .OrderBy(item => item.Definition.Order)
            .ToList();
        var blocked = comparisons
            .Where(item => item.Status != SchemaDefinitionStatus.Match && !CanAutoApply(item))
            .OrderBy(item => item.Definition.Order)
            .ToList();

        int applied = 0;

        foreach (var item in autoApplicable)
        {
            ct.ThrowIfCancellationRequested();
            progress.Report(new MigrationProgress(
                GetKindLabel(item.Definition.Kind),
                $"Applying {item.Definition.RelativePath} ({item.StatusMessage})…",
                false));

            var sql = await File.ReadAllTextAsync(item.Definition.FilePath, ct);
            var checksum = ComputeSha256(sql);
            var sw = Stopwatch.StartNew();
            string? error = null;

            try
            {
                await ApplyDefinitionAsync(conn, item, progress, ct);
                await EnsureSchemaVersionsTableIfCoreSchemaExistsAsync(conn, ct);
                await RecordMigrationAsync(conn, item, checksum, (int)sw.ElapsedMilliseconds, true, null, ct);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (await TableExistsAsync(conn, "Users", ct))
                {
                    await EnsureSchemaVersionsTableAsync(conn, ct);
                    await RecordMigrationAsync(conn, item, checksum, (int)sw.ElapsedMilliseconds, false, error, ct);
                }

                return new MigrationResult(false, applied, item.Definition.RelativePath, error);
            }

            sw.Stop();

            applied++;
            progress.Report(new MigrationProgress(
                GetKindLabel(item.Definition.Kind),
                $"✅ {item.Definition.RelativePath} applied in {sw.ElapsedMilliseconds} ms",
                true));
        }

        if (blocked.Count > 0)
        {
            string blockedList = string.Join(
                "; ",
                blocked.Select(item => $"{item.Definition.RelativePath} ({item.StatusMessage})"));
            return new MigrationResult(
                false,
                applied,
                blocked[0].Definition.RelativePath,
                $"Automatic updates are blocked for: {blockedList}. Review the deterministic mismatch details and use a targeted data-preserving migration where safe repair is unavailable.");
        }

        return new MigrationResult(true, applied, null, null);
    }

    /// <inheritdoc />
    public async Task<RerunResult> RerunIdempotentObjectsAsync(
        IProgress<MigrationProgress> progress,
        CancellationToken ct = default)
    {
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var comparisons = await ScanDefinitionsAsync(conn, ct);
        var items = comparisons
            .Where(item => item.Status != SchemaDefinitionStatus.Match)
            .Where(item => item.Definition.Kind != SchemaDefinitionKind.Table)
            .Where(CanAutoApply)
            .OrderBy(item => item.Definition.Order)
            .ToList();

        int proceduresApplied = 0;
        int triggersApplied = 0;
        int indexesApplied = 0;
        List<string> errors = [];

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await ApplyDefinitionAsync(conn, item, progress, ct);
                progress.Report(new MigrationProgress(
                    GetKindLabel(item.Definition.Kind),
                    $"✅ {item.Definition.RelativePath} applied",
                    true));

                switch (item.Definition.Kind)
                {
                    case SchemaDefinitionKind.Procedure:
                        proceduresApplied++;
                        break;
                    case SchemaDefinitionKind.Trigger:
                        triggersApplied++;
                        break;
                    case SchemaDefinitionKind.Indexes:
                        indexesApplied++;
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{item.Definition.RelativePath}: {ex.Message}");
            }
        }

        return new RerunResult(proceduresApplied, triggersApplied, indexesApplied, errors);
    }

    /// <inheritdoc />
    public async Task ResetDatabaseAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default)
    {
        var db = _settingsStore.Get().Database;

        progress.Report(new MigrationProgress("RESET", $"Connecting to MySQL server {db.Host}:{db.Port}…", false));

        await using var conn = OpenServerConnection();
        await conn.OpenAsync(ct);

        progress.Report(new MigrationProgress("RESET", $"Dropping database `{db.DatabaseName}`…", false));
        await ExecuteNonQueryAsync(conn, $"DROP DATABASE IF EXISTS {EscapeIdentifier(db.DatabaseName)}", ct);

        progress.Report(new MigrationProgress("RESET", $"Recreating database `{db.DatabaseName}`…", false));
        await ExecuteNonQueryAsync(
            conn,
            $"CREATE DATABASE {EscapeIdentifier(db.DatabaseName)} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci",
            ct);

        var settings = _settingsStore.Get();
        settings.FirstRunComplete = false;
        await _settingsStore.SaveAsync(settings);

        progress.Report(new MigrationProgress("RESET", "Database reset completed. First-run setup has been re-enabled.", true));
    }

    /// <inheritdoc />
    public string PreviewMigrationSql(string version)
    {
        string? filePath = ResolveDefinitionPath(version);
        if (filePath is null || !File.Exists(filePath))
        {
            return string.Empty;
        }

        return File.ReadAllText(filePath);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task EnsureSchemaVersionsTableAsync(MySqlConnection conn, CancellationToken ct)
    {
        await RunRuntimeSqlFileAsync(conn, SchemaVersionsTableSqlPath, ct);
        await RunRuntimeSqlFileAsync(conn, SchemaVersionsPreExistingBootstrapSqlPath, ct);
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
            Server = db.Host,
            Port = (uint)db.Port,
            Database = db.DatabaseName,
            UserID = db.UpdaterUsername,
            Password = db.UpdaterPassword ?? string.Empty,
            ConnectionTimeout = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout = (uint)db.CommandTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode = MySqlSslMode.Preferred,
        };
        return new MySqlConnection(csb.ConnectionString);
    }

    private MySqlConnection OpenServerConnection()
    {
        var db = _settingsStore.Get().Database;

        if (string.IsNullOrWhiteSpace(db.Host))
            throw new InvalidOperationException("Database host is not configured. Open Settings and save a valid connection.");
        if (string.IsNullOrWhiteSpace(db.UpdaterUsername))
            throw new InvalidOperationException("Database username is not configured. Open Settings and save a valid connection.");

        var csb = new MySqlConnectionStringBuilder
        {
            Server = db.Host,
            Port = (uint)db.Port,
            UserID = db.UpdaterUsername,
            Password = db.UpdaterPassword ?? string.Empty,
            ConnectionTimeout = (uint)db.ConnectionTimeout,
            DefaultCommandTimeout = (uint)db.CommandTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode = MySqlSslMode.Preferred,
        };

        return new MySqlConnection(csb.ConnectionString);
    }

    private static async Task<bool> TableExistsAsync(MySqlConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.TABLES " +
            "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@t)";
        cmd.Parameters.AddWithValue("@t", tableName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task RecordMigrationAsync(
        MySqlConnection conn,
        SchemaDefinitionComparison item,
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
            "VALUES (@v, @d, @s, @c, @at, @by, @ms, @ok, @err) " +
            "ON DUPLICATE KEY UPDATE " +
            "Description = VALUES(Description), " +
            "Script = VALUES(Script), " +
            "Checksum = VALUES(Checksum), " +
            "AppliedAt = VALUES(AppliedAt), " +
            "AppliedBy = VALUES(AppliedBy), " +
            "ExecutionMs = VALUES(ExecutionMs), " +
            "Success = VALUES(Success), " +
            "ErrorMessage = VALUES(ErrorMessage)";
        cmd.Parameters.AddWithValue("@v", ComputeDefinitionVersion(item.Definition.RelativePath));
        cmd.Parameters.AddWithValue("@d", item.Definition.DisplayName);
        cmd.Parameters.AddWithValue("@s", item.Definition.RelativePath);
        cmd.Parameters.AddWithValue("@c", checksum);
        cmd.Parameters.AddWithValue("@at", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@by", Environment.UserName);
        cmd.Parameters.AddWithValue("@ms", executionMs);
        cmd.Parameters.AddWithValue("@ok", success ? 1 : 0);
        cmd.Parameters.AddWithValue("@err", (object?)error ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task ExecuteNonQueryAsync(MySqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException("Database identifier cannot be empty.");
        }

        return $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";
    }

    private async Task EnsureSchemaVersionsTableIfCoreSchemaExistsAsync(MySqlConnection conn, CancellationToken ct)
    {
        if (await TableExistsAsync(conn, "Users", ct))
        {
            await EnsureSchemaVersionsTableAsync(conn, ct);
        }
    }

    private static async Task RunRuntimeSqlFileAsync(MySqlConnection conn, string relativePath, CancellationToken ct)
    {
        string filePath = ResolvePath(relativePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Required SQL file was not found: {relativePath}", filePath);
        }

        string sql = await File.ReadAllTextAsync(filePath, ct);
        await SqlScriptRunner.RunFileScriptAsync(conn, sql, ct);
    }

    private async Task<List<SchemaDefinitionComparison>> ScanDefinitionsAsync(MySqlConnection conn, CancellationToken ct)
    {
        List<SchemaDefinitionComparison> results = [];

        foreach (var definition in LoadDefinitions())
        {
            results.Add(await CompareDefinitionAsync(conn, definition, ct));
        }

        return results;
    }

    private IReadOnlyList<SchemaDefinition> LoadDefinitions()
    {
        const string tablesFolder = "database/schema/tables";
        const string indexesFolder = "database/indexes";
        const string triggersFolder = "database/triggers";
        const string proceduresFolder = "database/procedures";

        int order = 0;
        List<SchemaDefinition> definitions = [];

        definitions.AddRange(LoadDefinitionsFromFolder(tablesFolder, SchemaDefinitionKind.Table, ref order));
        definitions.AddRange(LoadDefinitionsFromFolder(indexesFolder, SchemaDefinitionKind.Indexes, ref order));
        definitions.AddRange(LoadDefinitionsFromFolder(triggersFolder, SchemaDefinitionKind.Trigger, ref order));
        definitions.AddRange(LoadDefinitionsFromFolder(proceduresFolder, SchemaDefinitionKind.Procedure, ref order));

        return definitions;
    }

    private static List<SchemaDefinition> LoadDefinitionsFromFolder(
        string relativeFolder,
        SchemaDefinitionKind kind,
        ref int order)
    {
        List<SchemaDefinition> definitions = [];
        string root = ResolvePath(relativeFolder);
        if (!Directory.Exists(root))
        {
            return definitions;
        }

        foreach (var filePath in Directory.EnumerateFiles(root, "*.sql", SearchOption.AllDirectories).OrderBy(path => GetDefinitionSortKey(kind, path), StringComparer.OrdinalIgnoreCase))
        {
            string relativePath = Path.GetRelativePath(AppContext.BaseDirectory, filePath).Replace('\\', '/');
            string objectName = Path.GetFileNameWithoutExtension(filePath);
            string displayName = kind == SchemaDefinitionKind.Indexes
                ? relativePath
                : objectName;

            definitions.Add(new SchemaDefinition(kind, order++, relativePath, filePath, displayName, objectName));
        }

        return definitions;
    }

    private static string GetDefinitionSortKey(SchemaDefinitionKind kind, string filePath)
    {
        string objectName = Path.GetFileNameWithoutExtension(filePath);
        if (kind == SchemaDefinitionKind.Table && TableDependencyOrder.TryGetValue(objectName, out int tableOrder))
        {
            return $"{tableOrder:D4}:{filePath}";
        }

        return $"9999:{filePath}";
    }

    private async Task<SchemaDefinitionComparison> CompareDefinitionAsync(
        MySqlConnection conn,
        SchemaDefinition definition,
        CancellationToken ct)
    {
        return definition.Kind switch
        {
            SchemaDefinitionKind.Table => await CompareTableDefinitionAsync(conn, definition, ct),
            SchemaDefinitionKind.Indexes => await CompareIndexDefinitionAsync(conn, definition, ct),
            SchemaDefinitionKind.Trigger => await CompareTriggerDefinitionAsync(conn, definition, ct),
            SchemaDefinitionKind.Procedure => await CompareProcedureDefinitionAsync(conn, definition, ct),
            _ => new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Drifted, "definition requires review"),
        };
    }

    private async Task<TableSchemaValidationResult> GetTableValidationAsync(
        MySqlConnection conn,
        SchemaDefinition definition,
        CancellationToken ct)
    {
        string sql = await File.ReadAllTextAsync(definition.FilePath, ct);

        TableSchemaDefinition expectedSchema;
        try
        {
            expectedSchema = TableSchemaToolkit.ParseExpectedTableSchema(sql, definition.RelativePath);
        }
        catch (Exception ex)
        {
            return TableSchemaToolkit.CreateUnreadableResult(
                definition.ObjectName.ToLowerInvariant(),
                definition.RelativePath,
                TableMismatchKind.ParseError,
                ex.Message);
        }

        if (!await TableExistsAsync(conn, expectedSchema.TableName, ct))
        {
            return TableSchemaToolkit.CreateMissingResult(expectedSchema.TableName, definition.RelativePath);
        }

        try
        {
            TableSchemaDefinition liveSchema = await ReadLiveTableSchemaAsync(conn, expectedSchema.TableName, ct);
            List<TableSchemaMismatch> repairabilityMismatches = await BuildRepairabilityMismatchesAsync(conn, expectedSchema, liveSchema, ct);
            return TableSchemaToolkit.CompareTableSchemas(
                definition.RelativePath,
                expectedSchema,
                liveSchema,
                repairabilityMismatches);
        }
        catch (Exception ex)
        {
            return TableSchemaToolkit.CreateUnreadableResult(
                expectedSchema.TableName,
                definition.RelativePath,
                TableMismatchKind.MetadataReadError,
                ex.Message);
        }
    }

    private async Task<SchemaDefinitionComparison> CompareTableDefinitionAsync(
        MySqlConnection conn,
        SchemaDefinition definition,
        CancellationToken ct)
    {
        string sql = await File.ReadAllTextAsync(definition.FilePath, ct);

        TableSchemaDefinition expectedSchema;
        try
        {
            expectedSchema = TableSchemaToolkit.ParseExpectedTableSchema(sql, definition.RelativePath);
        }
        catch (Exception ex)
        {
            var unreadableResult = TableSchemaToolkit.CreateUnreadableResult(
                definition.ObjectName.ToLowerInvariant(),
                definition.RelativePath,
                TableMismatchKind.ParseError,
                ex.Message);

            return new SchemaDefinitionComparison(
                definition,
                SchemaDefinitionStatus.Unreadable,
                unreadableResult.Summary ?? "table definition could not be parsed",
                unreadableResult);
        }

        if (!await TableExistsAsync(conn, expectedSchema.TableName, ct))
        {
            var missingResult = TableSchemaToolkit.CreateMissingResult(expectedSchema.TableName, definition.RelativePath);
            return new SchemaDefinitionComparison(
                definition,
                SchemaDefinitionStatus.Missing,
                missingResult.Summary ?? "table missing",
                missingResult);
        }

        try
        {
            TableSchemaDefinition liveSchema = await ReadLiveTableSchemaAsync(conn, expectedSchema.TableName, ct);
            List<TableSchemaMismatch> repairabilityMismatches = await BuildRepairabilityMismatchesAsync(conn, expectedSchema, liveSchema, ct);
            TableSchemaValidationResult validation = TableSchemaToolkit.CompareTableSchemas(
                definition.RelativePath,
                expectedSchema,
                liveSchema,
                repairabilityMismatches);

            return new SchemaDefinitionComparison(
                definition,
                validation.Status switch
                {
                    TableValidationStatus.Match => SchemaDefinitionStatus.Match,
                    TableValidationStatus.Missing => SchemaDefinitionStatus.Missing,
                    TableValidationStatus.Unreadable => SchemaDefinitionStatus.Unreadable,
                    _ => SchemaDefinitionStatus.Drifted,
                },
                validation.Summary ?? "table definition drift detected",
                validation);
        }
        catch (Exception ex)
        {
            var unreadableResult = TableSchemaToolkit.CreateUnreadableResult(
                expectedSchema.TableName,
                definition.RelativePath,
                TableMismatchKind.MetadataReadError,
                ex.Message);

            return new SchemaDefinitionComparison(
                definition,
                SchemaDefinitionStatus.Unreadable,
                unreadableResult.Summary ?? "live table metadata could not be read",
                unreadableResult);
        }
    }

    private async Task<TableSchemaDefinition> ReadLiveTableSchemaAsync(
        MySqlConnection conn,
        string tableName,
        CancellationToken ct)
    {
        List<TableColumnDefinition> columns = [];

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, EXTRA, ORDINAL_POSITION " +
                "FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@table) " +
                "ORDER BY ORDINAL_POSITION";
            cmd.Parameters.AddWithValue("@table", tableName);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                bool isNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase);
                string? defaultValue = reader.IsDBNull(3)
                    ? null
                    : NormalizeDefaultLiteral(reader.GetString(3), isNullable);

                columns.Add(new TableColumnDefinition(
                    NormalizeIdentifier(reader.GetString(0)),
                    TableSchemaToolkit.NormalizeColumnTypeForComparison(reader.GetString(1)),
                    isNullable,
                    defaultValue,
                    NormalizeExtra(reader.IsDBNull(4) ? string.Empty : reader.GetString(4)),
                    reader.GetInt32(5)));
            }
        }

        List<string> primaryKeyColumns = [];
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE " +
                "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@table) AND CONSTRAINT_NAME = 'PRIMARY' " +
                "ORDER BY ORDINAL_POSITION";
            cmd.Parameters.AddWithValue("@table", tableName);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                primaryKeyColumns.Add(NormalizeIdentifier(reader.GetString(0)));
            }
        }

        List<TableUniqueConstraintDefinition> uniqueConstraints = [];
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT kcu.CONSTRAINT_NAME, GROUP_CONCAT(kcu.COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION SEPARATOR ',') " +
                "FROM information_schema.TABLE_CONSTRAINTS tc " +
                "JOIN information_schema.KEY_COLUMN_USAGE kcu " +
                "  ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA " +
                " AND tc.TABLE_NAME = kcu.TABLE_NAME " +
                " AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME " +
                "WHERE tc.CONSTRAINT_SCHEMA = DATABASE() AND LOWER(tc.TABLE_NAME) = LOWER(@table) AND tc.CONSTRAINT_TYPE = 'UNIQUE' " +
                "GROUP BY kcu.CONSTRAINT_NAME";
            cmd.Parameters.AddWithValue("@table", tableName);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                uniqueConstraints.Add(new TableUniqueConstraintDefinition(
                    NormalizeIdentifier(reader.GetString(0)),
                    reader.GetString(1)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(NormalizeIdentifier)
                        .ToList()));
            }
        }

        List<TableForeignKeyDefinition> foreignKeys = [];
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT kcu.CONSTRAINT_NAME, " +
                "       GROUP_CONCAT(kcu.COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION SEPARATOR ','), " +
                "       kcu.REFERENCED_TABLE_NAME, " +
                "       GROUP_CONCAT(kcu.REFERENCED_COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION SEPARATOR ','), " +
                "       COALESCE(rc.DELETE_RULE, 'RESTRICT'), " +
                "       COALESCE(rc.UPDATE_RULE, 'RESTRICT') " +
                "FROM information_schema.KEY_COLUMN_USAGE kcu " +
                "JOIN information_schema.REFERENTIAL_CONSTRAINTS rc " +
                "  ON kcu.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA " +
                " AND kcu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME " +
                " AND kcu.TABLE_NAME = rc.TABLE_NAME " +
                "WHERE kcu.TABLE_SCHEMA = DATABASE() AND LOWER(kcu.TABLE_NAME) = LOWER(@table) AND kcu.REFERENCED_TABLE_NAME IS NOT NULL " +
                "GROUP BY kcu.CONSTRAINT_NAME, kcu.REFERENCED_TABLE_NAME, rc.DELETE_RULE, rc.UPDATE_RULE";
            cmd.Parameters.AddWithValue("@table", tableName);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                foreignKeys.Add(new TableForeignKeyDefinition(
                    NormalizeIdentifier(reader.GetString(0)),
                    reader.GetString(1)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(NormalizeIdentifier)
                        .ToList(),
                    NormalizeIdentifier(reader.GetString(2)),
                    reader.GetString(3)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(NormalizeIdentifier)
                        .ToList(),
                    NormalizeReferentialAction(reader.GetString(4)),
                    NormalizeReferentialAction(reader.GetString(5))));
            }
        }

        return new TableSchemaDefinition(
            NormalizeIdentifier(tableName),
            columns,
            primaryKeyColumns,
            uniqueConstraints,
            foreignKeys);
    }

    private async Task<List<TableSchemaMismatch>> BuildRepairabilityMismatchesAsync(
        MySqlConnection conn,
        TableSchemaDefinition expectedSchema,
        TableSchemaDefinition liveSchema,
        CancellationToken ct)
    {
        List<TableSchemaMismatch> mismatches = [];
        TableRestorePlan restorePlan = TableSchemaToolkit.BuildRestorePlan(expectedSchema, liveSchema);

        foreach (var missingRequiredColumn in restorePlan.MissingRequiredColumns)
        {
            mismatches.Add(new TableSchemaMismatch(
                TableMismatchKind.RestoreIncompatible,
                TableMismatchSeverity.Blocking,
                missingRequiredColumn,
                "Restore",
                "required",
                null,
                $"Column {missingRequiredColumn} cannot be restored automatically because the canonical table requires a NOT NULL value with no default.",
                "Add a targeted data-preserving schema change or repair the related tables manually."));
        }

        if (liveSchema.Columns.Count > 0 && restorePlan.CommonColumns.Count == 0)
        {
            mismatches.Add(new TableSchemaMismatch(
                TableMismatchKind.RestoreIncompatible,
                TableMismatchSeverity.Blocking,
                expectedSchema.TableName,
                "Restore",
                null,
                null,
                $"Table {expectedSchema.TableName} has no common columns that can be restored after a rebuild.",
                "Repair this table manually or implement a targeted migration path for the schema change."));
        }

        List<InboundForeignKeyReference> inboundReferences = await GetInboundForeignKeyReferencesAsync(conn, expectedSchema.TableName, ct);
        if (inboundReferences.Count > 0)
        {
            mismatches.Add(new TableSchemaMismatch(
                TableMismatchKind.RestoreIncompatible,
                TableMismatchSeverity.Blocking,
                expectedSchema.TableName,
                "InboundForeignKeys",
                null,
                string.Join(", ", inboundReferences.Select(reference => $"{reference.TableName}.{reference.ConstraintName}")),
                $"Table {expectedSchema.TableName} is referenced by inbound foreign keys and cannot be auto-repaired as a single-table rebuild.",
                "Repair the dependent tables as an ordered set or perform a manual migration."));
        }

        return mismatches;
    }

    private async Task<List<InboundForeignKeyReference>> GetInboundForeignKeyReferencesAsync(
        MySqlConnection conn,
        string tableName,
        CancellationToken ct)
    {
        List<InboundForeignKeyReference> references = [];

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT TABLE_NAME, CONSTRAINT_NAME " +
            "FROM information_schema.KEY_COLUMN_USAGE " +
            "WHERE REFERENCED_TABLE_SCHEMA = DATABASE() AND LOWER(REFERENCED_TABLE_NAME) = LOWER(@table) " +
            "GROUP BY TABLE_NAME, CONSTRAINT_NAME";
        cmd.Parameters.AddWithValue("@table", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            references.Add(new InboundForeignKeyReference(
                NormalizeIdentifier(reader.GetString(0)),
                NormalizeIdentifier(reader.GetString(1))));
        }

        return references;
    }

    private async Task<SchemaDefinitionComparison> CompareProcedureDefinitionAsync(
        MySqlConnection conn,
        SchemaDefinition definition,
        CancellationToken ct)
    {
        if (!await ProcedureExistsAsync(conn, definition.ObjectName, ct))
        {
            return new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Missing, "procedure missing");
        }

        string sourceSql = await File.ReadAllTextAsync(definition.FilePath, ct);
        string liveSql = await GetLiveRoutineSqlAsync(conn, $"SHOW CREATE PROCEDURE {EscapeIdentifier(definition.ObjectName)}", 2, ct);

        ProcedureSchemaDefinition sourceProcedure = ParseProcedureDefinition(sourceSql, definition.RelativePath);
        ProcedureSchemaDefinition liveProcedure = ParseProcedureDefinition(liveSql, definition.ObjectName);

        return sourceProcedure == liveProcedure
            ? new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Match, "definition matches")
            : new SchemaDefinitionComparison(
                definition,
                SchemaDefinitionStatus.Drifted,
                $"procedure definition drift detected (expected {sourceProcedure}, actual {liveProcedure})");
    }

    private async Task<SchemaDefinitionComparison> CompareTriggerDefinitionAsync(
        MySqlConnection conn,
        SchemaDefinition definition,
        CancellationToken ct)
    {
        if (!await TriggerExistsAsync(conn, definition.ObjectName, ct))
        {
            return new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Missing, "trigger missing");
        }

        string sourceSql = await File.ReadAllTextAsync(definition.FilePath, ct);
        TriggerSchemaDefinition sourceTrigger = ParseSourceTriggerDefinition(sourceSql, definition.RelativePath);
        TriggerSchemaDefinition liveTrigger = await ReadLiveTriggerDefinitionAsync(conn, definition.ObjectName, ct);

        return sourceTrigger == liveTrigger
            ? new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Match, "definition matches")
            : new SchemaDefinitionComparison(
                definition,
                SchemaDefinitionStatus.Drifted,
                $"trigger definition drift detected (expected {sourceTrigger}, actual {liveTrigger})");
    }

    private static TriggerSchemaDefinition ParseSourceTriggerDefinition(string sql, string relativePath)
    {
        string normalized = Regex.Replace(sql, @"(?im)^\s*--.*$", string.Empty);
        normalized = Regex.Replace(normalized, @"(?im)^\s*DELIMITER\s+.+?$", string.Empty);
        normalized = Regex.Replace(normalized, @"(?im)^\s*USE\s+.+?;\s*$", string.Empty);
        normalized = Regex.Replace(normalized, @"(?is)DROP\s+TRIGGER\s+IF\s+EXISTS.+?;", string.Empty);

        var match = CreateTriggerRegex.Match(normalized);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse trigger definition from {relativePath}.");
        }

        return new TriggerSchemaDefinition(
            NormalizeIdentifier(match.Groups["name"].Value),
            match.Groups["timing"].Value.ToLowerInvariant(),
            match.Groups["event"].Value.ToLowerInvariant(),
            NormalizeIdentifier(match.Groups["table"].Value),
            NormalizeTriggerBody(match.Groups["body"].Value));
    }

    private static ProcedureSchemaDefinition ParseProcedureDefinition(string sql, string sourceName)
    {
        string normalized = StripRoutinePreamble(sql);
        var match = CreateProcedureRegex.Match(normalized);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse procedure definition from {sourceName}.");
        }

        return new ProcedureSchemaDefinition(
            NormalizeIdentifier(match.Groups["name"].Value),
            NormalizeProcedureParameters(match.Groups["parameters"].Value),
            NormalizeRoutineBody(match.Groups["body"].Value));
    }

    private static async Task<TriggerSchemaDefinition> ReadLiveTriggerDefinitionAsync(
        MySqlConnection conn,
        string triggerName,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT TRIGGER_NAME, ACTION_TIMING, EVENT_MANIPULATION, EVENT_OBJECT_TABLE, ACTION_STATEMENT " +
            "FROM information_schema.TRIGGERS " +
            "WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME = @name";
        cmd.Parameters.AddWithValue("@name", triggerName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException($"Trigger {triggerName} was not found in live metadata.");
        }

        return new TriggerSchemaDefinition(
            NormalizeIdentifier(reader.GetString(0)),
            reader.GetString(1).ToLowerInvariant(),
            reader.GetString(2).ToLowerInvariant(),
            NormalizeIdentifier(reader.GetString(3)),
            NormalizeTriggerBody(reader.GetString(4)));
    }

    private static string NormalizeTriggerBody(string body)
    {
        return NormalizeRoutineBody(body);
    }

    private static string NormalizeProcedureParameters(string parameters)
    {
        string normalized = parameters.Replace("`", string.Empty, StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, @"\s*,\s*", ",", RegexOptions.IgnoreCase);
        normalized = NormalizeWhitespace(normalized).Trim();
        return normalized.ToLowerInvariant();
    }

    private static string NormalizeRoutineBody(string body)
    {
        string normalized = body.Replace("`", string.Empty, StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, @"\bcurrent_timestamp\(\)", "current_timestamp", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\s*;\s*", ";", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @";(?=end\b)", string.Empty, RegexOptions.IgnoreCase);
        normalized = NormalizeWhitespace(normalized).Trim().TrimEnd(';');
        normalized = NormalizeDefaultValue(normalized);
        return normalized.ToLowerInvariant();
    }

    private async Task<SchemaDefinitionComparison> CompareIndexDefinitionAsync(
        MySqlConnection conn,
        SchemaDefinition definition,
        CancellationToken ct)
    {
        var expectedIndexes = await ParseIndexDefinitionsAsync(definition.FilePath, ct);
        if (expectedIndexes.Count == 0)
        {
            return new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Match, "no index statements found");
        }

        bool hasMissing = false;
        bool hasDrift = false;

        foreach (var expected in expectedIndexes)
        {
            var live = await GetLiveIndexAsync(conn, expected.TableName, expected.IndexName, ct);
            if (live is null)
            {
                hasMissing = true;
                continue;
            }

            if (!string.Equals(live.Columns, expected.Columns, StringComparison.OrdinalIgnoreCase))
            {
                hasDrift = true;
            }
        }

        if (!hasMissing && !hasDrift)
        {
            return new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Match, "definition matches");
        }

        return hasDrift
            ? new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Drifted, "index definition drift detected")
            : new SchemaDefinitionComparison(definition, SchemaDefinitionStatus.Missing, "index missing");
    }

    private async Task ApplyDefinitionAsync(
        MySqlConnection conn,
        SchemaDefinitionComparison item,
        IProgress<MigrationProgress>? progress,
        CancellationToken ct)
    {
        if (!CanAutoApply(item))
        {
            throw new InvalidOperationException($"Automatic updates are blocked for {item.Definition.RelativePath} because it requires a data-preserving ALTER path.");
        }

        if (item.Definition.Kind == SchemaDefinitionKind.Table)
        {
            await ApplyTableDefinitionAsync(conn, item, progress, ct);
            return;
        }

        string sql = await File.ReadAllTextAsync(item.Definition.FilePath, ct);
        await SqlScriptRunner.RunFileScriptAsync(conn, sql, ct);

        var postApply = await CompareDefinitionAsync(conn, item.Definition, ct);
        if (postApply.Status != SchemaDefinitionStatus.Match)
        {
            throw new InvalidOperationException($"The database still does not match {item.Definition.RelativePath} after applying the stored SQL file.");
        }
    }

    private async Task ApplyTableDefinitionAsync(
        MySqlConnection conn,
        SchemaDefinitionComparison item,
        IProgress<MigrationProgress>? progress,
        CancellationToken ct)
    {
        TableSchemaValidationResult validation = item.TableValidation
            ?? throw new InvalidOperationException($"Deterministic table validation was not available for {item.Definition.RelativePath}.");

        switch (validation.Status)
        {
            case TableValidationStatus.Missing:
                {
                    string sql = await File.ReadAllTextAsync(item.Definition.FilePath, ct);
                    await SqlScriptRunner.RunFileScriptAsync(conn, sql, ct);

                    var postCreate = await CompareDefinitionAsync(conn, item.Definition, ct);
                    if (postCreate.Status == SchemaDefinitionStatus.Match)
                    {
                        return;
                    }

                    if (postCreate.TableValidation is { Status: TableValidationStatus.Mismatch, CanRepair: true })
                    {
                        var repairResult = await RepairTableAsync(conn, item.Definition, postCreate.TableValidation, progress, ct);
                        if (!repairResult.IsSuccess)
                        {
                            throw new InvalidOperationException(repairResult.ErrorMessage);
                        }

                        var postRepair = await CompareDefinitionAsync(conn, item.Definition, ct);
                        if (postRepair.Status != SchemaDefinitionStatus.Match)
                        {
                            throw new InvalidOperationException($"The database still does not match {item.Definition.RelativePath} after repair. {postRepair.StatusMessage}");
                        }

                        return;
                    }

                    throw new InvalidOperationException($"The database still does not match {item.Definition.RelativePath} after applying the canonical SQL file. {postCreate.StatusMessage}");
                }

            case TableValidationStatus.Mismatch:
                {
                    var repairResult = await RepairTableAsync(conn, item.Definition, validation, progress, ct);
                    if (!repairResult.IsSuccess)
                    {
                        throw new InvalidOperationException(repairResult.ErrorMessage);
                    }

                    var postRepair = await CompareDefinitionAsync(conn, item.Definition, ct);
                    if (postRepair.Status != SchemaDefinitionStatus.Match)
                    {
                        throw new InvalidOperationException($"The database still does not match {item.Definition.RelativePath} after repair. {postRepair.StatusMessage}");
                    }

                    return;
                }

            case TableValidationStatus.Unreadable:
                throw new InvalidOperationException($"The stored SQL file or live metadata for {item.Definition.RelativePath} could not be read deterministically.");

            default:
                throw new InvalidOperationException($"Unexpected table validation status {validation.Status} for {item.Definition.RelativePath}.");
        }
    }

    private async Task<TableRepairResult> RepairTableAsync(
        MySqlConnection conn,
        SchemaDefinition definition,
        TableSchemaValidationResult validation,
        IProgress<MigrationProgress>? progress,
        CancellationToken ct)
    {
        string sql = await File.ReadAllTextAsync(definition.FilePath, ct);
        TableSchemaDefinition expectedSchema = TableSchemaToolkit.ParseExpectedTableSchema(sql, definition.RelativePath);
        TableSchemaDefinition liveSchema = await ReadLiveTableSchemaAsync(conn, expectedSchema.TableName, ct);
        TableRestorePlan restorePlan = TableSchemaToolkit.BuildRestorePlan(expectedSchema, liveSchema);

        string backupTableName = await ReserveBackupTableNameAsync(conn, expectedSchema.TableName, ct);
        List<string> stepsCompleted = [];
        string currentStep = "repair initialization";
        int rowsBackedUp = 0;
        int rowsRestored = 0;
        bool backupCreated = false;

        try
        {
            if (!validation.CanRepair)
            {
                return new TableRepairResult(
                    false,
                    expectedSchema.TableName,
                    null,
                    0,
                    0,
                    stepsCompleted,
                    $"Table repair is blocked for {expectedSchema.TableName}. Review the pending mismatch details before applying updates.",
                    false);
            }

            progress?.Report(new MigrationProgress("TABLE", $"Validating mismatch for {expectedSchema.TableName}", false));

            currentStep = "creating temporary data backup";
            progress?.Report(new MigrationProgress("TABLE", $"Creating temporary data backup {backupTableName} for {expectedSchema.TableName}", false));
            await ExecuteNonQueryAsync(
                conn,
                $"CREATE TABLE {EscapeIdentifier(backupTableName)} AS SELECT * FROM {EscapeIdentifier(expectedSchema.TableName)}",
                ct);
            backupCreated = true;
            stepsCompleted.Add($"Created temporary backup {backupTableName}");

            int sourceRows = await CountRowsAsync(conn, expectedSchema.TableName, ct);
            rowsBackedUp = await CountRowsAsync(conn, backupTableName, ct);
            if (rowsBackedUp != sourceRows)
            {
                throw new InvalidOperationException($"Backup row count mismatch detected. Source rows: {sourceRows}, backup rows: {rowsBackedUp}.");
            }

            stepsCompleted.Add($"Backed up {rowsBackedUp} rows");
            progress?.Report(new MigrationProgress("TABLE", $"Backed up {rowsBackedUp} rows.", false));

            currentStep = "validating restore feasibility";
            if (restorePlan.MissingRequiredColumns.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Column {string.Join(", ", restorePlan.MissingRequiredColumns)} cannot be restored because the canonical table requires NOT NULL values without defaults.");
            }

            if (rowsBackedUp > 0 && restorePlan.CommonColumns.Count == 0)
            {
                throw new InvalidOperationException("No common columns exist between the backup and canonical table, so existing rows cannot be restored.");
            }

            var inboundReferences = await GetInboundForeignKeyReferencesAsync(conn, expectedSchema.TableName, ct);
            if (inboundReferences.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Inbound foreign keys block automatic repair: {string.Join(", ", inboundReferences.Select(reference => $"{reference.TableName}.{reference.ConstraintName}"))}.");
            }

            stepsCompleted.Add("Validated restore feasibility");

            currentStep = "rebuilding canonical table";
            progress?.Report(new MigrationProgress("TABLE", $"Rebuilding {expectedSchema.TableName} from canonical SQL", false));
            await ExecuteNonQueryAsync(conn, $"DROP TABLE {EscapeIdentifier(expectedSchema.TableName)}", ct);
            stepsCompleted.Add($"Dropped {expectedSchema.TableName}");
            await SqlScriptRunner.RunFileScriptAsync(conn, sql, ct);
            stepsCompleted.Add($"Recreated {expectedSchema.TableName} from canonical SQL");

            currentStep = "restoring compatible data";
            progress?.Report(new MigrationProgress("TABLE", $"Restoring compatible data into {expectedSchema.TableName}", false));
            if (rowsBackedUp > 0)
            {
                string restoreSql = BuildRestoreInsertSql(expectedSchema.TableName, backupTableName, restorePlan.CommonColumns);
                await ExecuteNonQueryAsync(conn, restoreSql, ct);
                stepsCompleted.Add("Restored compatible data");
            }

            rowsRestored = await CountRowsAsync(conn, expectedSchema.TableName, ct);
            if (rowsRestored != rowsBackedUp)
            {
                throw new InvalidOperationException($"Restored row count mismatch detected. Backup rows: {rowsBackedUp}, restored rows: {rowsRestored}.");
            }

            progress?.Report(new MigrationProgress("TABLE", $"Restored {rowsRestored} rows.", false));

            currentStep = "validating repaired table";
            var postRepair = await CompareDefinitionAsync(conn, definition, ct);
            if (postRepair.Status != SchemaDefinitionStatus.Match)
            {
                throw new InvalidOperationException(postRepair.StatusMessage);
            }

            progress?.Report(new MigrationProgress("TABLE", "Validation passed after repair.", false));

            currentStep = "cleaning up temporary backup";
            progress?.Report(new MigrationProgress("TABLE", "Cleaning up temporary backup", false));
            await ExecuteNonQueryAsync(conn, $"DROP TABLE {EscapeIdentifier(backupTableName)}", ct);
            stepsCompleted.Add($"Dropped temporary backup {backupTableName}");
            progress?.Report(new MigrationProgress("TABLE", "Removed temporary backup.", false));

            return new TableRepairResult(
                true,
                expectedSchema.TableName,
                backupTableName,
                rowsBackedUp,
                rowsRestored,
                stepsCompleted,
                null,
                false);
        }
        catch (Exception ex)
        {
            string backupMessage = backupCreated
                ? $" Backup preserved as {backupTableName}."
                : string.Empty;

            return new TableRepairResult(
                false,
                expectedSchema.TableName,
                backupCreated ? backupTableName : null,
                rowsBackedUp,
                rowsRestored,
                stepsCompleted,
                $"Table repair failed during {currentStep} for {expectedSchema.TableName}.{backupMessage} Reason: {ex.Message}",
                backupCreated);
        }
    }

    private async Task<string> ReserveBackupTableNameAsync(
        MySqlConnection conn,
        string tableName,
        CancellationToken ct)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            string suffix = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture)[..8];
            string candidate = $"__mtm_migration_backup_{NormalizeIdentifier(tableName)}_{DateTime.UtcNow:yyyyMMddHHmmss}_{suffix}";
            if (!await TableExistsAsync(conn, candidate, ct))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Could not reserve a temporary backup table name for {tableName}.");
    }

    private static bool CanAutoApply(SchemaDefinitionComparison item) =>
        item.Definition.Kind switch
        {
            SchemaDefinitionKind.Table => item.TableValidation is { Status: TableValidationStatus.Missing } || item.TableValidation?.CanRepair == true,
            SchemaDefinitionKind.Indexes => item.Status == SchemaDefinitionStatus.Missing,
            SchemaDefinitionKind.Trigger => true,
            SchemaDefinitionKind.Procedure => true,
            _ => false,
        };

    private static PendingMigration ToPendingMigration(SchemaDefinitionComparison item) =>
        new(
            GetKindLabel(item.Definition.Kind),
            item.Definition.Order,
            $"{item.Definition.DisplayName} - {item.StatusMessage}",
            item.Definition.RelativePath,
            item.Definition.FilePath,
            item.TableValidation?.Status,
            item.TableValidation?.Mismatches,
            CanAutoApply(item),
            item.TableValidation?.Summary);

    private static string GetKindLabel(SchemaDefinitionKind kind) => kind switch
    {
        SchemaDefinitionKind.Table => "TABLE",
        SchemaDefinitionKind.Indexes => "INDEX",
        SchemaDefinitionKind.Trigger => "TRIGGER",
        SchemaDefinitionKind.Procedure => "PROCEDURE",
        _ => "OBJECT",
    };

    private static string ComputeDefinitionVersion(string relativePath) =>
        $"OBJ-{ComputeSha256(relativePath)[..12]}";

    private static string? ResolveDefinitionPath(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        if (Path.IsPathRooted(identifier) && File.Exists(identifier))
        {
            return identifier;
        }

        string normalized = identifier.Replace('/', Path.DirectorySeparatorChar);
        string combined = Path.Combine(AppContext.BaseDirectory, normalized);
        return File.Exists(combined) ? combined : null;
    }

    private static async Task<bool> ProcedureExistsAsync(MySqlConnection conn, string procedureName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = DATABASE() AND ROUTINE_NAME = @name AND ROUTINE_TYPE = 'PROCEDURE'";
        cmd.Parameters.AddWithValue("@name", procedureName);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }

    private static async Task<bool> TriggerExistsAsync(MySqlConnection conn, string triggerName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME = @name";
        cmd.Parameters.AddWithValue("@name", triggerName);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }

    private static async Task<string> GetLiveRoutineSqlAsync(MySqlConnection conn, string sql, int ordinal, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return string.Empty;
        }

        return reader.GetString(ordinal);
    }

    private static string NormalizeIdentifier(string identifier) =>
        identifier.Replace("`", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

    private static string NormalizeReferentialAction(string action) =>
        NormalizeWhitespace(action).Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

    private static string NormalizeColumnType(string input) =>
        TableSchemaToolkit.NormalizeColumnTypeForComparison(input);

    private static string? NormalizeDefaultLiteral(string input, bool isNullable) =>
        TableSchemaToolkit.NormalizeDefaultLiteralForComparison(input, isNullable);

    private static string NormalizeExtra(string input) =>
        TableSchemaToolkit.NormalizeExtraForComparison(input);

    private static async Task<List<ExpectedIndexDefinition>> ParseIndexDefinitionsAsync(string filePath, CancellationToken ct)
    {
        string sql = await File.ReadAllTextAsync(filePath, ct);
        return CreateIndexRegex.Matches(sql)
            .Select(match => new ExpectedIndexDefinition(
                match.Groups["name"].Value,
                match.Groups["table"].Value,
                string.Join(",",
                    Regex.Matches(match.Groups["columns"].Value, @"`(?<name>[^`]+)`")
                        .Select(column => column.Groups["name"].Value.ToLowerInvariant()))))
            .ToList();
    }

    private static async Task<LiveIndexDefinition?> GetLiveIndexAsync(
        MySqlConnection conn,
        string tableName,
        string indexName,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT TABLE_NAME, INDEX_NAME, GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',') " +
            "FROM information_schema.STATISTICS " +
            "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND INDEX_NAME = @index " +
            "GROUP BY TABLE_NAME, INDEX_NAME";
        cmd.Parameters.AddWithValue("@table", tableName);
        cmd.Parameters.AddWithValue("@index", indexName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new LiveIndexDefinition(reader.GetString(0), reader.GetString(1), reader.GetString(2).ToLowerInvariant());
    }

    private static string NormalizeRoutineSql(string sql)
    {
        string normalized = StripRoutinePreamble(sql);
        normalized = normalized.Replace("`", string.Empty, StringComparison.Ordinal);
        normalized = NormalizeWhitespace(normalized).Trim().TrimEnd(';');
        normalized = Regex.Replace(normalized, @"\s*;\s*END\b", " END", RegexOptions.IgnoreCase);
        normalized = NormalizeDefaultValue(normalized);
        return normalized.ToLowerInvariant();
    }

    private static string StripRoutinePreamble(string sql)
    {
        var customDelimiters = Regex.Matches(sql, @"(?im)^\s*DELIMITER\s+(?<delimiter>\S+)\s*$")
            .Select(match => match.Groups["delimiter"].Value)
            .Where(delimiter => !string.IsNullOrWhiteSpace(delimiter))
            .Where(delimiter => !string.Equals(delimiter, ";", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        string normalized = Regex.Replace(sql, @"(?im)^\s*--.*$", string.Empty);
        normalized = Regex.Replace(normalized, @"(?im)^\s*DELIMITER\s+.+?$", string.Empty);
        normalized = Regex.Replace(normalized, @"(?im)^\s*USE\s+.+?;\s*$", string.Empty);
        normalized = Regex.Replace(normalized, @"(?is)DROP\s+(?:PROCEDURE|TRIGGER)\s+IF\s+EXISTS.+?;", string.Empty);
        normalized = Regex.Replace(normalized, @"CREATE\s+DEFINER\s*=\s*(?:`[^`]*`|'[^']*')\s*@\s*(?:`[^`]*`|'[^']*')\s+(?=(?:PROCEDURE|TRIGGER))", "CREATE ", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"CREATE\s+DEFINER\s*=\s*[^\s]+\s+(?=(?:PROCEDURE|TRIGGER))", "CREATE ", RegexOptions.IgnoreCase);

        foreach (var delimiter in customDelimiters)
        {
            normalized = Regex.Replace(normalized, $@"\s*{Regex.Escape(delimiter)}\s*$", string.Empty);
        }

        int createIndex = normalized.IndexOf("CREATE", StringComparison.OrdinalIgnoreCase);
        if (createIndex >= 0)
        {
            normalized = normalized[createIndex..];
        }

        return normalized;
    }

    private static string NormalizeDefaultValue(string input) =>
        Regex.Replace(
            Regex.Replace(input, @"\bcurrent_timestamp\(\)", "current_timestamp", RegexOptions.IgnoreCase),
            @"\bdefault\s+'(?<value>[^']+)'",
            "default ${value}",
            RegexOptions.IgnoreCase);

    private static async Task<int> CountRowsAsync(MySqlConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {EscapeIdentifier(tableName)}";
        object? value = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(value);
    }

    private static string BuildRestoreInsertSql(string tableName, string backupTableName, IReadOnlyList<string> commonColumns)
    {
        string columnList = string.Join(", ", commonColumns.Select(EscapeIdentifier));
        return $"INSERT INTO {EscapeIdentifier(tableName)} ({columnList}) SELECT {columnList} FROM {EscapeIdentifier(backupTableName)}";
    }

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();

    private static string ResolvePath(string path) =>
        Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    private enum SchemaDefinitionKind
    {
        Table,
        Indexes,
        Trigger,
        Procedure,
    }

    private enum SchemaDefinitionStatus
    {
        Match,
        Missing,
        Drifted,
        Unreadable,
    }

    private sealed record SchemaDefinition(
        SchemaDefinitionKind Kind,
        int Order,
        string RelativePath,
        string FilePath,
        string DisplayName,
        string ObjectName);

    private sealed record SchemaDefinitionComparison(
        SchemaDefinition Definition,
        SchemaDefinitionStatus Status,
        string StatusMessage,
        TableSchemaValidationResult? TableValidation = null);

    private sealed record InboundForeignKeyReference(string TableName, string ConstraintName);

    private sealed record ExpectedIndexDefinition(string IndexName, string TableName, string Columns);

    private sealed record LiveIndexDefinition(string TableName, string IndexName, string Columns);

    private sealed record TriggerSchemaDefinition(
        string Name,
        string Timing,
        string Event,
        string TableName,
        string Body);

    private sealed record ProcedureSchemaDefinition(
        string Name,
        string Parameters,
        string Body);
}
