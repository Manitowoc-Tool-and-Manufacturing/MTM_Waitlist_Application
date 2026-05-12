using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Models.Migration;
using System;
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
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private bool _schemaVersionsTableExists;

    /// <summary>True when there is a status message to display in the InfoBar.</summary>
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    /// <summary>True when the operation log has content; used to enable the Operation Log card.</summary>
    public bool HasProgressLines => ProgressLines.Count > 0;

    /// <summary>Description shown on the Operation Log expander; notes when no output is available.</summary>
    public string OperationLogDescription => HasProgressLines
        ? "Output from the most recent migration run"
        : "No output yet — run a migration or re-run idempotent objects to populate this log";

    /// <summary>InfoBar severity: Error when <see cref="IsError"/> is true, otherwise Informational.</summary>
    public InfoBarSeverity StatusSeverity => IsError ? InfoBarSeverity.Error : InfoBarSeverity.Informational;
    [ObservableProperty] private string _sqlPreview = string.Empty;

    /// <summary>True when the SchemaVersions table has not yet been created; used to show the warning banner.</summary>
    public bool IsSchemaVersionsTableMissing => !SchemaVersionsTableExists;

    public ViewModel_Migrations(IService_Migration migration)
    {
        _migration = migration;
        ProgressLines.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasProgressLines));
            OnPropertyChanged(nameof(OperationLogDescription));
        };
    }

    /// <summary>Loads migration state using the correct three-state decision tree.</summary>
    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsBusy = true;
        IsError = false;
        StatusMessage = "Detecting database state…";

        try
        {
            // Step 1 — Detect which of the three real states the database is in.
            // DetectSchemaStateAsync creates the SchemaVersions table and backfills if needed,
            // so by the time it returns we are always in a consistent state for the next steps.
            var (state, errorMessage) = await _migration.DetectSchemaStateAsync(ct);

            switch (state)
            {
                case MigrationSchemaState.Error:
                    IsError = true;
                    StatusMessage = $"Could not connect to database: {errorMessage}";
                    SchemaVersionsTableExists = false;
                    OnPropertyChanged(nameof(IsSchemaVersionsTableMissing));
                    return;

                case MigrationSchemaState.FreshDatabase:
                    // Empty database — no tables at all. SchemaVersions was not created because
                    // there is nothing to backfill. Show V001 onward as pending.
                    SchemaVersionsTableExists = false;
                    OnPropertyChanged(nameof(IsSchemaVersionsTableMissing));
                    PendingMigrations = new ObservableCollection<PendingMigration>(_migration.GetPendingMigrations());
                    AppliedMigrations = [];
                    StatusMessage = $"Fresh database — {PendingMigrations.Count} migration(s) ready to apply.";
                    return;

                case MigrationSchemaState.PreExistingSchema:
                    // DetectSchemaStateAsync already created SchemaVersions and backfilled known
                    // versions, so we fall through to the Ready load path below.
                    SchemaVersionsTableExists = true;
                    OnPropertyChanged(nameof(IsSchemaVersionsTableMissing));
                    StatusMessage = "Schema versions table created and backfilled — loading migrations…";
                    break;

                case MigrationSchemaState.Ready:
                    SchemaVersionsTableExists = true;
                    OnPropertyChanged(nameof(IsSchemaVersionsTableMissing));
                    break;
            }

            // Step 2 — SchemaVersions is now guaranteed to exist. Load applied and pending.
            var applied = await _migration.GetAppliedMigrationsAsync(ct);
            AppliedMigrations = new ObservableCollection<AppliedMigration>(applied);
            PendingMigrations = new ObservableCollection<PendingMigration>(_migration.GetPendingMigrations());

            StatusMessage = state == MigrationSchemaState.PreExistingSchema
                ? $"Backfill complete — {applied.Count} applied, {PendingMigrations.Count} pending."
                : $"{PendingMigrations.Count} pending migration(s).";
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Could not connect to database: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
        IsError = false;
        ProgressLines.Clear();

        try
        {
            var progress = new Progress<MigrationProgress>(p => ProgressLines.Add($"[{p.Version}] {p.Message}"));
            var result = await _migration.ApplyPendingMigrationsAsync(progress, ct);

            IsError = !result.IsSuccess;
            StatusMessage = result.IsSuccess
                ? $"Applied {result.MigrationsApplied} migration(s) successfully."
                : $"Migration failed at {result.FailedVersion}: {result.ErrorMessage}";

            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Apply migrations failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
        IsError = false;
        ProgressLines.Clear();

        try
        {
            var progress = new Progress<MigrationProgress>(p => ProgressLines.Add($"[{p.Version}] {p.Message}"));
            var result = await _migration.RerunIdempotentObjectsAsync(progress, ct);

            StatusMessage = $"Re-ran {result.ProceduresApplied} procedures, {result.TriggersApplied} triggers, {result.IndexesApplied} indexes.";
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Re-run failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Loads the SQL preview for the selected pending migration.</summary>
    [RelayCommand]
    private void PreviewMigration(PendingMigration migration)
    {
        SqlPreview = _migration.PreviewMigrationSql(migration.Version);
    }

    // Raise dependent computed properties when their sources change.
    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
        OnPropertyChanged(nameof(StatusSeverity));
    }

    partial void OnIsErrorChanged(bool value) =>
        OnPropertyChanged(nameof(StatusSeverity));
}
