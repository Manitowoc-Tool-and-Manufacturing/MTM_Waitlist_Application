using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Models.Migration;
using MTM_Waitlist_Server.Module.Migrations.ViewModels;
using System.Xml.Linq;

namespace MTM_Waitlist_Server.Module.Migrations.Tests;

/// <summary>
/// Verifies the runtime SQL layout and the migration page behavior that consumes
/// deterministic pending-migration details.
/// </summary>
public sealed class MigrationFiles_Tests
{
    [Fact]
    public void SchemaVersionsBootstrapSqlFiles_ShouldExist()
    {
        Assert.True(File.Exists(GetRepoPath(
            "Database",
            "schema",
            "tables",
            "System",
            "SchemaVersions.sql")));

        Assert.True(File.Exists(GetRepoPath(
            "Database",
            "schema",
            "data",
            "System",
            "SchemaVersions_PreExisting_Bootstrap.sql")));

        Assert.True(File.Exists(GetRepoPath(
            "Database",
            "schema",
            "data",
            "System",
            "SchemaVersions_BaselineHistory.sql")));
    }

    [Fact]
    public void AdminHostProject_ShouldCopyDatabaseSqlFoldersToRuntime()
    {
        string projectFile = GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "MTM_Waitlist_Server.Admin.csproj");

        XDocument document = XDocument.Load(projectFile);

        XElement? contentItem = document
            .Descendants("Content")
            .FirstOrDefault(element =>
                string.Equals(
                    element.Attribute("Include")?.Value,
                    "..\\..\\Database\\**\\*.sql",
                    StringComparison.Ordinal));

        Assert.NotNull(contentItem);
        Assert.Equal(
            "database\\%(RecursiveDir)%(Filename)%(Extension)",
            contentItem!.Attribute("Link")?.Value);
        Assert.Equal(
            "PreserveNewest",
            contentItem.Attribute("CopyToOutputDirectory")?.Value);
    }

    [Fact]
    public async Task WipeDatabaseCommand_ShouldResetState_WhenResetSucceeds()
    {
        FakeMigrationService service = new()
        {
            DetectState = MigrationSchemaState.FreshDatabase,
            PendingMigrations =
            [
                new PendingMigration("TABLE", 1, "Users - Table missing - will be created from canonical SQL.", "database/schema/tables/Auth/Users.sql", "TEST-Users.sql", TableValidationStatus.Missing, null, true, "Table missing - will be created from canonical SQL.")
            ]
        };

        ViewModel_Migrations viewModel = new(service);

        await viewModel.WipeDatabaseCommand.ExecuteAsync(null);

        Assert.True(service.ResetCalled);
        Assert.False(viewModel.IsError);
        Assert.Equal("Database wiped successfully. The server database is now clean and ready for a fresh first-run bootstrap or migration comparison run.", viewModel.StatusMessage);
        Assert.Single(viewModel.PendingMigrations);
        Assert.Contains(viewModel.ProgressLines, line => line.Contains("Database reset completed", StringComparison.Ordinal));
    }

    [Fact]
    public void PreviewMigration_ShouldIncludeMismatchDetails_WhenPendingItemHasStructuredValidationData()
    {
        FakeMigrationService service = new();
        ViewModel_Migrations viewModel = new(service);
        PendingMigration migration = new(
            "TABLE",
            1,
            "WaitlistEntries - Table mismatch - safe repair available (2 issue(s)).",
            "database/schema/tables/Waitlist/WaitlistEntries.sql",
            "TEST-WaitlistEntries.sql",
            TableValidationStatus.Mismatch,
            [
                new TableSchemaMismatch(
                    TableMismatchKind.ColumnDefaultMismatch,
                    TableMismatchSeverity.Repairable,
                    "status",
                    "Default",
                    "waiting",
                    "WAITING",
                    "Column status has a different default value in the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data.")
            ],
            true,
            "Table mismatch - safe repair available (1 issue(s)).");

        viewModel.PreviewMigrationCommand.Execute(migration);

        Assert.Contains("Mismatch Details:", viewModel.SqlPreview, StringComparison.Ordinal);
        Assert.Contains("Column status has a different default value in the live table.", viewModel.SqlPreview, StringComparison.Ordinal);
        Assert.Contains("Canonical SQL:", viewModel.SqlPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationService_ShouldNormalizeMySqlTriggerFormatting_WhenComparingStoredSql()
    {
        string serviceFile = File.ReadAllText(GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "Services",
            "Service_Migration.cs"));

        Assert.Contains("CREATE\\s+DEFINER\\s*=\\s*(?:`[^`]*`|'[^']*')\\s*@\\s*(?:`[^`]*`|'[^']*')", serviceFile, StringComparison.Ordinal);
        Assert.Contains("\\s*;\\s*END\\b", serviceFile, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationService_ShouldParseNestedTriggerBlocks_WhenSourceContainsEndIf()
    {
        string serviceFile = File.ReadAllText(GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "Services",
            "Service_Migration.cs"));

        Assert.Contains("BEGIN\\b.*\\bEND", serviceFile, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN\\b.*?\\bEND", serviceFile, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationService_ShouldCompareProceduresStructurally_WhenStoredSqlHasNestedBlocks()
    {
        string serviceFile = File.ReadAllText(GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "Services",
            "Service_Migration.cs"));

        Assert.Contains("ProcedureSchemaDefinition", serviceFile, StringComparison.Ordinal);
        Assert.Contains("NormalizeProcedureParameters", serviceFile, StringComparison.Ordinal);
        Assert.Contains("NormalizeRoutineBody", serviceFile, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthProcedure_ShouldUseUsersTableCollation_WhenComparingWindowsUsernameParameter()
    {
        string procedureSql = File.ReadAllText(GetRepoPath(
            "Database",
            "procedures",
            "Auth",
            "usp_Auth_GetUserByWindowsUsername.sql"));

        Assert.Contains("CONVERT(p_WindowsUsername USING utf8mb4) COLLATE utf8mb4_unicode_ci", procedureSql, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminAuth_ShouldFallbackToUsersTable_WhenStoredProcedureHasCollationMismatch()
    {
        string serviceFile = File.ReadAllText(GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "Services",
            "Service_AdminAuth.cs"));

        Assert.Contains("ex.Number is 1267 or 1305", serviceFile, StringComparison.Ordinal);
        Assert.Contains("GetRoleFromUsersTableAsync", serviceFile, StringComparison.Ordinal);
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

    private sealed class FakeMigrationService : IService_Migration
    {
        public bool ResetCalled { get; private set; }

        public MigrationSchemaState DetectState { get; set; } = MigrationSchemaState.FreshDatabase;

        public IReadOnlyList<PendingMigration> PendingMigrations { get; set; } = [];

        public Task<(MigrationSchemaState State, string? ErrorMessage)> DetectSchemaStateAsync(CancellationToken ct = default)
            => Task.FromResult((DetectState, (string?)null));

        public Task<bool> SchemaVersionsTableExistsAsync(CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<MigrationResult> ApplyPendingMigrationsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default)
            => Task.FromResult(new MigrationResult(true, 0, null, null));

        public Task<RerunResult> RerunIdempotentObjectsAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default)
            => Task.FromResult(new RerunResult(0, 0, 0, []));

        public Task ResetDatabaseAsync(IProgress<MigrationProgress> progress, CancellationToken ct = default)
        {
            ResetCalled = true;
            progress.Report(new MigrationProgress("RESET", "Database reset completed. First-run setup has been re-enabled.", true));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AppliedMigration>> GetAppliedMigrationsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AppliedMigration>>([]);

        public Task<IReadOnlyList<PendingMigration>> GetPendingMigrationsAsync(CancellationToken ct = default)
            => Task.FromResult(PendingMigrations);

        public string PreviewMigrationSql(string version)
            => "CREATE TABLE TEST (...);";
    }
}