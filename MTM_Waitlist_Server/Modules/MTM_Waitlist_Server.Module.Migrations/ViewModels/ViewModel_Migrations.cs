using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Models.Migration;
using System.Collections.ObjectModel;

namespace MTM_Waitlist_Server.Module.Migrations.ViewModels;

/// <summary>
/// ViewModel for the Migration Runner module page.
/// Shows applied and pending migrations and controls the migration runner.
/// </summary>
public partial class ViewModel_Migrations : ObservableObject
{
    private readonly IService_Migration _migration;

    [ObservableProperty] private ObservableCollection<AppliedMigration> _appliedMigrations = [];
    [ObservableProperty] private ObservableCollection<PendingMigration> _pendingMigrations = [];
    [ObservableProperty] private ObservableCollection<string> _progressLines = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _schemaVersionsTableExists;
    [ObservableProperty] private string _sqlPreview = string.Empty;

    public ViewModel_Migrations(IService_Migration migration)
    {
        _migration = migration;
    }

    /// <summary>Loads the applied and pending migration lists.</summary>
    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsBusy = true;
        SchemaVersionsTableExists = await _migration.SchemaVersionsTableExistsAsync(ct);

        if (SchemaVersionsTableExists)
        {
            var applied = await _migration.GetAppliedMigrationsAsync(ct);
            AppliedMigrations = new ObservableCollection<AppliedMigration>(applied);
        }

        PendingMigrations = new ObservableCollection<PendingMigration>(_migration.GetPendingMigrations());
        StatusMessage = $"{PendingMigrations.Count} pending migration(s).";
        IsBusy = false;
    }

    /// <summary>Applies all pending migrations in version order.</summary>
    [RelayCommand]
    private async Task ApplyMigrationsAsync(CancellationToken ct)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ProgressLines.Clear();

        var progress = new Progress<MigrationProgress>(p => ProgressLines.Add($"[{p.Version}] {p.Message}"));
        var result = await _migration.ApplyPendingMigrationsAsync(progress, ct);

        StatusMessage = result.IsSuccess
            ? $"Applied {result.MigrationsApplied} migration(s) successfully."
            : $"Migration failed at {result.FailedVersion}: {result.ErrorMessage}";

        await LoadAsync(ct);
        IsBusy = false;
    }

    /// <summary>Re-runs all stored procedures, triggers, and indexes.</summary>
    [RelayCommand]
    private async Task RerunIdempotentObjectsAsync(CancellationToken ct)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ProgressLines.Clear();

        var progress = new Progress<MigrationProgress>(p => ProgressLines.Add($"[{p.Version}] {p.Message}"));
        var result = await _migration.RerunIdempotentObjectsAsync(progress, ct);

        StatusMessage = $"Re-ran {result.ProceduresApplied} procedures, {result.TriggersApplied} triggers, {result.IndexesApplied} indexes.";
        IsBusy = false;
    }

    /// <summary>Loads the SQL preview for the selected pending migration.</summary>
    [RelayCommand]
    private void PreviewMigration(PendingMigration migration)
    {
        SqlPreview = _migration.PreviewMigrationSql(migration.Version);
    }
}
