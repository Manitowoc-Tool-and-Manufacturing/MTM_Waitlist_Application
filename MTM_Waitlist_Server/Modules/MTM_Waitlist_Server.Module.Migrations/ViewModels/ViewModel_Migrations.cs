using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Interfaces.Migration;
using MTM_Waitlist_Server.Core.Models.Migration;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Module.Migrations.ViewModels;

/// <summary>
/// ViewModel for the database compare-and-update page.
/// Shows pending object drift, update history, and recovery actions.
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

    /// <summary>True when at least one pending definition still needs an update.</summary>
    public bool HasPendingMigrations => PendingMigrations.Count > 0;

    /// <summary>True when automatic update commands should be enabled.</summary>
    public bool CanApplyPendingMigrations => !IsBusy && PendingMigrations.Any(migration => migration.CanAutoApply);

    /// <summary>Description shown on the pending-updates card.</summary>
    public string PendingUpdatesDescription => HasPendingMigrations
        ? BuildPendingUpdatesDescription()
        : "No pending updates - the checked-in SQL currently matches the live database";

    /// <summary>True when the operation log has content; used to enable the Operation Log card.</summary>
    public bool HasProgressLines => ProgressLines.Count > 0;

    /// <summary>Description shown on the Operation Log expander; notes when no output is available.</summary>
    public string OperationLogDescription => HasProgressLines
        ? "Output from the most recent compare or apply run"
        : "No output yet - run a comparison or apply updates to populate this log";

    /// <summary>True when destructive actions should be disabled.</summary>
    public bool CanWipeDatabase => !IsBusy;

    /// <summary>InfoBar severity: Error when <see cref="IsError"/> is true, otherwise Informational.</summary>
    public InfoBarSeverity StatusSeverity => IsError ? InfoBarSeverity.Error : InfoBarSeverity.Informational;
    [ObservableProperty] private string _sqlPreview = string.Empty;

    /// <summary>True when the update-history table has not yet been created; used to show the warning banner.</summary>
    public bool IsSchemaVersionsTableMissing => !SchemaVersionsTableExists;

    public ViewModel_Migrations(IService_Migration migration)
    {
        _migration = migration;
        ProgressLines.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasProgressLines));
            OnPropertyChanged(nameof(OperationLogDescription));
        };

        PendingMigrations.CollectionChanged += (_, _) => UpdatePendingMigrationState();
    }

    /// <summary>Loads database update state using the compare-first workflow.</summary>
    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsBusy = true;
        IsError = false;
        StatusMessage = "Comparing stored SQL definitions to the database...";

        try
        {
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
                    SchemaVersionsTableExists = false;
                    OnPropertyChanged(nameof(IsSchemaVersionsTableMissing));
                    PendingMigrations = new ObservableCollection<PendingMigration>(await _migration.GetPendingMigrationsAsync(ct));
                    UpdatePendingMigrationState();
                    AppliedMigrations = [];
                    StatusMessage = PendingMigrations.Count > 0
                        ? $"Fresh database detected - {PendingMigrations.Count} stored object definition(s) are ready to apply."
                        : "Fresh database detected - no stored object definitions were found to apply.";
                    return;

                case MigrationSchemaState.PreExistingSchema:
                    SchemaVersionsTableExists = true;
                    OnPropertyChanged(nameof(IsSchemaVersionsTableMissing));
                    StatusMessage = "Update history table is ready - loading comparison results...";
                    break;

                case MigrationSchemaState.Ready:
                    SchemaVersionsTableExists = true;
                    OnPropertyChanged(nameof(IsSchemaVersionsTableMissing));
                    break;
            }

            var applied = await _migration.GetAppliedMigrationsAsync(ct);
            AppliedMigrations = new ObservableCollection<AppliedMigration>(applied);
            PendingMigrations = new ObservableCollection<PendingMigration>(await _migration.GetPendingMigrationsAsync(ct));
            UpdatePendingMigrationState();

            StatusMessage = state == MigrationSchemaState.PreExistingSchema
                ? PendingMigrations.Count > 0
                    ? $"Comparison ready - {PendingMigrations.Count} pending update(s), {applied.Count} recorded update attempt(s)."
                    : $"Comparison ready - no pending updates, {applied.Count} recorded update attempt(s)."
                : PendingMigrations.Count > 0
                    ? $"Comparison ready - {PendingMigrations.Count} pending update(s)."
                    : "Comparison ready - no pending updates.";
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

    /// <summary>Applies the pending updates that can be safely applied automatically.</summary>
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

            await LoadAsync(ct);

            IsError = !result.IsSuccess;
            StatusMessage = result.IsSuccess
                ? $"Applied {result.MigrationsApplied} database update(s) successfully."
                : $"Update run stopped at {result.FailedVersion}: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Apply updates failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Applies pending replaceable objects without rebuilding tables.</summary>
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

            await LoadAsync(ct);

            StatusMessage = $"Applied {result.ProceduresApplied} procedures, {result.TriggersApplied} triggers, and {result.IndexesApplied} index file(s).";

            if (result.ErrorMessages.Count > 0)
            {
                IsError = true;
                StatusMessage = $"Replaceable-object apply finished with errors: {string.Join(" | ", result.ErrorMessages)}";
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Apply replaceable objects failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Loads the SQL preview for the selected pending object definition.</summary>
    [RelayCommand]
    private void PreviewMigration(PendingMigration migration)
    {
        SqlPreview = BuildPreviewText(migration, _migration.PreviewMigrationSql(migration.Script));
    }

    // Raise dependent computed properties when their sources change.
    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
        OnPropertyChanged(nameof(StatusSeverity));
    }

    partial void OnIsErrorChanged(bool value) =>
        OnPropertyChanged(nameof(StatusSeverity));

    private void UpdatePendingMigrationState()
    {
        OnPropertyChanged(nameof(HasPendingMigrations));
        OnPropertyChanged(nameof(CanApplyPendingMigrations));
        OnPropertyChanged(nameof(PendingUpdatesDescription));
    }

    private string BuildPendingUpdatesDescription()
    {
        int autoApplicableCount = PendingMigrations.Count(migration => migration.CanAutoApply);
        int blockedCount = PendingMigrations.Count - autoApplicableCount;

        return blockedCount > 0
            ? $"{PendingMigrations.Count} pending definition(s): {autoApplicableCount} safe to apply, {blockedCount} require manual action"
            : $"{autoApplicableCount} pending definition(s) are safe to apply automatically";
    }

    private static string BuildPreviewText(PendingMigration migration, string sql)
    {
        StringBuilder builder = new();
        builder.AppendLine($"Object: {migration.Script}");
        builder.AppendLine($"Apply Status: {migration.AutoApplySummary}");

        if (!string.IsNullOrWhiteSpace(migration.DetailSummary))
        {
            builder.AppendLine($"Summary: {migration.DetailSummary}");
        }

        if (migration.HasMismatchDetails)
        {
            builder.AppendLine();
            builder.AppendLine("Mismatch Details:");

            foreach (var mismatch in migration.MismatchDetails)
            {
                builder.Append("- ");
                builder.Append(mismatch.Message);

                if (!string.IsNullOrWhiteSpace(mismatch.ExpectedValue) || !string.IsNullOrWhiteSpace(mismatch.ActualValue))
                {
                    builder.Append($" | expected: {mismatch.ExpectedValue ?? "<none>"} | actual: {mismatch.ActualValue ?? "<none>"}");
                }

                if (!string.IsNullOrWhiteSpace(mismatch.RecommendedAction))
                {
                    builder.Append($" | action: {mismatch.RecommendedAction}");
                }

                builder.AppendLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(sql))
        {
            builder.AppendLine();
            builder.AppendLine("Canonical SQL:");
            builder.AppendLine(sql);
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>Drops and recreates the configured database so the baseline workflow can start from a clean state.</summary>
    [RelayCommand]
    internal async Task WipeDatabaseAsync(CancellationToken ct)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        IsError = false;
        SqlPreview = string.Empty;
        ProgressLines.Clear();

        try
        {
            var progress = new Progress<MigrationProgress>(p => ProgressLines.Add($"[{p.Version}] {p.Message}"));
            await _migration.ResetDatabaseAsync(progress, ct);

            AppliedMigrations = [];
            await LoadAsync(ct);
            StatusMessage = "Database wiped successfully. The server database is now clean and ready for a fresh first-run bootstrap or migration comparison run.";
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Database wipe failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanWipeDatabase));
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanWipeDatabase));
        OnPropertyChanged(nameof(CanApplyPendingMigrations));
    }
}
