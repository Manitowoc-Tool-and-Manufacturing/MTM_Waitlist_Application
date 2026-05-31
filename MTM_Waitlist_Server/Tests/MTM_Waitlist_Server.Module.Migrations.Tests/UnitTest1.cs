using System.IO;
using System.Xml.Linq;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Models.Migration;
using MTM_Waitlist_Server.Module.Migrations.ViewModels;

namespace MTM_Waitlist_Server.Module.Migrations.Tests;

/// <summary>
/// Verifies that versioned SQL migrations and runtime SQL deployment stay aligned
/// with how the server admin host discovers migration files on disk.
/// </summary>
public sealed class MigrationFiles_Tests
{
    [Fact]
    public void SetupTechMigrations_ShouldExistInVersionOrder()
    {
        var migrationsPath = GetRepoPath("Database", "migrations");
        var files = Directory.GetFiles(migrationsPath, "V*.sql")
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("V003__SetupTech_Schema.sql", files);
        Assert.Contains("V004__SetupTech_Default_DunnageTypeConfig.sql", files);

        var schemaIndex = Array.IndexOf(files, "V003__SetupTech_Schema.sql");
        var defaultsIndex = Array.IndexOf(files, "V004__SetupTech_Default_DunnageTypeConfig.sql");

        Assert.True(schemaIndex >= 0);
        Assert.True(defaultsIndex > schemaIndex);
    }

    [Fact]
    public void SetupTechDefaultDataMigration_ShouldBeSafeAndIdempotent()
    {
        var sql = File.ReadAllText(GetRepoPath(
            "Database",
            "migrations",
            "V004__SetupTech_Default_DunnageTypeConfig.sql"));

        Assert.Contains("INSERT IGNORE INTO `SetupTechDunnageTypeConfig`", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ON DUPLICATE KEY UPDATE", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdminHostProject_ShouldCopyDatabaseSqlFoldersToRuntime()
    {
        var projectFile = GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "MTM_Waitlist_Server.Admin.csproj");

        var document = XDocument.Load(projectFile);

        var contentItem = document
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
        var service = new FakeMigrationService
        {
            DetectState = MigrationSchemaState.FreshDatabase,
            PendingMigrations =
            [
                new PendingMigration("V001", 1, "Initial Schema", "V001__Initial_Schema.sql", "TEST-V001.sql")
            ]
        };

        var viewModel = new ViewModel_Migrations(service);

        await viewModel.WipeDatabaseCommand.ExecuteAsync(null);

        Assert.True(service.ResetCalled);
        Assert.False(viewModel.IsError);
        Assert.Equal("Database wiped successfully. The server database is now clean and ready for a fresh migration/bootstrap run.", viewModel.StatusMessage);
        Assert.Single(viewModel.PendingMigrations);
        Assert.Contains(viewModel.ProgressLines, line => line.Contains("Database reset completed", StringComparison.Ordinal));
    }

    [Fact]
    public void MigrationService_ShouldTreatOnlySuccessfulVersionsAsApplied()
    {
        var serviceFile = File.ReadAllText(GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "Services",
            "Service_Migration.cs"));

        Assert.Contains("SELECT Version FROM SchemaVersions WHERE Success = 1", serviceFile, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationService_ShouldUpsertSchemaVersionRows_WhenRetryingFailedMigration()
    {
        var serviceFile = File.ReadAllText(GetRepoPath(
            "Hosts",
            "MTM_Waitlist_Server.Admin",
            "Services",
            "Service_Migration.cs"));

        Assert.Contains("ON DUPLICATE KEY UPDATE", serviceFile, StringComparison.Ordinal);
        Assert.Contains("Success = VALUES(Success)", serviceFile, StringComparison.Ordinal);
    }

    private static string GetRepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "..", "..", "..", "..", "..", "MTM_Waitlist_Server");
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

        public IReadOnlyList<PendingMigration> GetPendingMigrations()
            => PendingMigrations;

        public string PreviewMigrationSql(string version)
            => string.Empty;
    }
}
