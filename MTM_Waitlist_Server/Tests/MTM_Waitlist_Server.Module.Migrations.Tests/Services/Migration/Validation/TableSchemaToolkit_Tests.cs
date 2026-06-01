using MTM_Waitlist_Server.Core.Migration;
using MTM_Waitlist_Server.Core.Models.Migration;

namespace MTM_Waitlist_Server.Module.Migrations.Tests.Services.Migration.Validation;

/// <summary>
/// Exercises the deterministic parser, comparer, and restore-feasibility helpers that
/// back the migration repair workflow.
/// </summary>
public sealed class TableSchemaToolkit_Tests
{
    [Fact]
    public void ParseExpectedTableSchema_ShouldParseEveryCanonicalTableFile_WhenDefinitionsAreCanonical()
    {
        string tablesRoot = GetRepoPath("Database", "schema", "tables");

        foreach (string filePath in Directory.GetFiles(tablesRoot, "*.sql", SearchOption.AllDirectories))
        {
            string sql = File.ReadAllText(filePath);
            TableSchemaDefinition schema = TableSchemaToolkit.ParseExpectedTableSchema(sql, filePath);

            Assert.False(string.IsNullOrWhiteSpace(schema.TableName));
            Assert.NotEmpty(schema.Columns);
        }
    }

    [Fact]
    public void ParseExpectedTableSchema_ShouldNormalizeEnumDefaultsAndForeignKeyRules_WhenCanonicalSqlUsesMixedCaseValues()
    {
        string sql = File.ReadAllText(GetRepoPath(
            "Database",
            "schema",
            "tables",
            "Waitlist",
            "WaitlistEntries.sql"));

        TableSchemaDefinition schema = TableSchemaToolkit.ParseExpectedTableSchema(sql, "WaitlistEntries.sql");
        TableColumnDefinition statusColumn = Assert.Single(schema.Columns, column => column.Name == "status");
        TableForeignKeyDefinition createdByForeignKey = Assert.Single(schema.ForeignKeys, foreignKey => foreignKey.Name == "fk_waitlistentries_createdbyuser");

        Assert.Equal("waiting", statusColumn.NormalizedDefault);
        Assert.Equal("setnull", createdByForeignKey.DeleteRule);
        Assert.Equal("cascade", createdByForeignKey.UpdateRule);
    }

    [Fact]
    public void CompareTableSchemas_ShouldMatchSchemaVersions_WhenLiveMetadataUsesMySqlFormatting()
    {
        string sql = File.ReadAllText(GetRepoPath(
            "Database",
            "schema",
            "tables",
            "System",
            "SchemaVersions.sql"));

        TableSchemaDefinition expected = TableSchemaToolkit.ParseExpectedTableSchema(sql, "SchemaVersions.sql");
        TableSchemaDefinition live = new(
            "schemaversions",
            [
                LiveColumn("Id", "int", false, null, "auto_increment", 1),
                LiveColumn("Version", "varchar(20)", false, null, string.Empty, 2),
                LiveColumn("Description", "varchar(200)", false, null, string.Empty, 3),
                LiveColumn("Script", "varchar(300)", false, null, string.Empty, 4),
                LiveColumn("Checksum", "varchar(64)", false, null, string.Empty, 5),
                LiveColumn("AppliedAt", "datetime", false, null, string.Empty, 6),
                LiveColumn("AppliedBy", "varchar(100)", false, null, string.Empty, 7),
                LiveColumn("ExecutionMs", "int", false, null, string.Empty, 8),
                LiveColumn("Success", "tinyint(1)", false, "1", string.Empty, 9),
                LiveColumn("ErrorMessage", "text", true, null, string.Empty, 10),
            ],
            ["id"],
            [new TableUniqueConstraintDefinition("uq_schemaversions_version", ["version"])],
            []);

        TableSchemaValidationResult result = TableSchemaToolkit.CompareTableSchemas("SchemaVersions.sql", expected, live);

        Assert.True(
            result.Status == TableValidationStatus.Match,
            string.Join(Environment.NewLine, result.Mismatches.Select(mismatch => $"{mismatch.Kind} {mismatch.ObjectName}.{mismatch.PropertyName}: expected={mismatch.ExpectedValue}, actual={mismatch.ActualValue}")));
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public void CompareTableSchemas_ShouldIgnoreRepairabilityBlockers_WhenSchemasMatch()
    {
        string sql = File.ReadAllText(GetRepoPath(
            "Database",
            "schema",
            "tables",
            "Auth",
            "Users.sql"));

        TableSchemaDefinition expected = TableSchemaToolkit.ParseExpectedTableSchema(sql, "Users.sql");
        TableSchemaDefinition live = new(
            expected.TableName,
            expected.Columns,
            expected.PrimaryKeyColumns,
            expected.UniqueConstraints,
            expected.ForeignKeys);

        TableSchemaMismatch inboundForeignKeyBlocker = new(
            TableMismatchKind.RestoreIncompatible,
            TableMismatchSeverity.Blocking,
            "users",
            "InboundForeignKeys",
            null,
            "refreshtokens.fk_RefreshTokens_Users",
            "Table users is referenced by inbound foreign keys and cannot be auto-repaired as a single-table rebuild.",
            "Repair the dependent tables as an ordered set or perform a manual migration.");

        TableSchemaValidationResult result = TableSchemaToolkit.CompareTableSchemas(
            "Users.sql",
            expected,
            live,
            [inboundForeignKeyBlocker]);

        Assert.Equal(TableValidationStatus.Match, result.Status);
        Assert.Empty(result.Mismatches);
        Assert.False(result.CanRepair);
    }

    [Fact]
    public void CompareTableSchemas_ShouldReturnStructuredMismatches_WhenLiveColumnsDriftFromCanonicalDefinition()
    {
        TableSchemaDefinition expected = new(
            "users",
            [
                new TableColumnDefinition("id", "int", false, null, "auto_increment", 1),
                new TableColumnDefinition("username", "varchar(100)", false, null, string.Empty, 2),
            ],
            ["id"],
            [],
            []);

        TableSchemaDefinition live = new(
            "users",
            [
                new TableColumnDefinition("id", "int", false, null, "auto_increment", 1),
                new TableColumnDefinition("username", "varchar(200)", true, "guest", string.Empty, 2),
                new TableColumnDefinition("legacyflag", "tinyint(1)", false, "0", string.Empty, 3),
            ],
            ["id"],
            [],
            []);

        TableSchemaValidationResult result = TableSchemaToolkit.CompareTableSchemas("Users.sql", expected, live);

        Assert.Equal(TableValidationStatus.Mismatch, result.Status);
        Assert.True(result.CanRepair);
        Assert.Contains(result.Mismatches, mismatch => mismatch.Kind == TableMismatchKind.ColumnTypeMismatch && mismatch.ObjectName == "username");
        Assert.Contains(result.Mismatches, mismatch => mismatch.Kind == TableMismatchKind.ColumnNullabilityMismatch && mismatch.ObjectName == "username");
        Assert.Contains(result.Mismatches, mismatch => mismatch.Kind == TableMismatchKind.ColumnDefaultMismatch && mismatch.ObjectName == "username");
        Assert.Contains(result.Mismatches, mismatch => mismatch.Kind == TableMismatchKind.ColumnUnexpected && mismatch.ObjectName == "legacyflag");
    }

    [Fact]
    public void BuildRestorePlan_ShouldFlagMissingRequiredColumns_WhenBackupCannotPopulateCanonicalColumns()
    {
        TableSchemaDefinition expected = new(
            "users",
            [
                new TableColumnDefinition("id", "int", false, null, "auto_increment", 1),
                new TableColumnDefinition("username", "varchar(100)", false, null, string.Empty, 2),
                new TableColumnDefinition("displayname", "varchar(200)", false, null, string.Empty, 3),
            ],
            ["id"],
            [],
            []);

        TableSchemaDefinition backup = new(
            "users",
            [
                new TableColumnDefinition("id", "int", false, null, "auto_increment", 1),
                new TableColumnDefinition("username", "varchar(100)", false, null, string.Empty, 2),
            ],
            ["id"],
            [],
            []);

        TableRestorePlan plan = TableSchemaToolkit.BuildRestorePlan(expected, backup);

        Assert.Contains("displayname", plan.MissingRequiredColumns);
        Assert.Contains("id", plan.CommonColumns);
        Assert.Contains("username", plan.CommonColumns);
    }

    private static string GetRepoPath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "..", "..", "..", "..", "..", "MTM_Waitlist_Server");
            candidate = Path.GetFullPath(candidate);

            if (Directory.Exists(candidate))
            {
                return Path.Combine(new[] { candidate }.Concat(segments).ToArray());
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the MTM_Waitlist_Server repository root from the test base directory.");
    }

    private static TableColumnDefinition LiveColumn(
        string name,
        string type,
        bool isNullable,
        string? defaultValue,
        string extra,
        int ordinal) =>
        new(
            name.ToLowerInvariant(),
            TableSchemaToolkit.NormalizeColumnTypeForComparison(type),
            isNullable,
            defaultValue is null ? null : TableSchemaToolkit.NormalizeDefaultLiteralForComparison(defaultValue, isNullable),
            TableSchemaToolkit.NormalizeExtraForComparison(extra),
            ordinal);
}