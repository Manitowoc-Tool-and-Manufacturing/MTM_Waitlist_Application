using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist_Server.Core.Interfaces.Backup;
using MTM_Waitlist_Server.Core.Models.Backup;
using System.Collections.ObjectModel;

namespace MTM_Waitlist_Server.Module.Backup.ViewModels;

/// <summary>
/// ViewModel for the Backup &amp; Restore module page.
/// </summary>
public partial class ViewModel_Backup : ObservableObject
{
    private readonly IService_Backup _backup;

    [ObservableProperty] private ObservableCollection<BackupFileInfo> _backupHistory = [];
    [ObservableProperty] private ObservableCollection<string> _progressLines = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private DateTimeOffset _selectedClearDate = DateTimeOffset.Now;

    /// <summary>True when the operation log has content; used to enable the Operation Log card.</summary>
    public bool HasProgressLines => ProgressLines.Count > 0;

    /// <summary>Description shown on the Operation Log expander; notes when no output is available.</summary>
    public string OperationLogDescription => HasProgressLines
        ? "Output from the most recent backup or restore operation"
        : "No output yet — run a backup or restore to populate this log";

    public ViewModel_Backup(IService_Backup backup)
    {
        _backup = backup;
        ProgressLines.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasProgressLines));
            OnPropertyChanged(nameof(OperationLogDescription));
        };
    }

    /// <summary>Runs a full manual backup.</summary>
    [RelayCommand]
    private async Task BackUpNowAsync(CancellationToken ct)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ProgressLines.Clear();

        var progress = new Progress<string>(line => ProgressLines.Add(line));
        var result = await _backup.RunBackupAsync(BackupType.Manual, schemaOnly: false, ct);

        StatusMessage = result.IsSuccess
            ? $"Backup complete — {result.FilePath}"
            : $"Backup failed: {result.ErrorMessage}";

        await RefreshHistoryAsync();
        IsBusy = false;
    }

    /// <summary>Restores from the selected backup file.</summary>
    [RelayCommand]
    private async Task RestoreAsync(BackupFileInfo file)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ProgressLines.Clear();

        var progress = new Progress<string>(line => ProgressLines.Add(line));
        var result = await _backup.RunRestoreAsync(file.FilePath, progress, CancellationToken.None);

        StatusMessage = result.IsSuccess
            ? $"Restore complete — {result.TablesRestored} tables restored."
            : $"Restore failed: {result.ErrorMessage}";

        IsBusy = false;
    }

    /// <summary>Opens a file picker to restore from an arbitrary file.</summary>
    [RelayCommand]
    private Task RestoreFromFileAsync()
    {
        // TODO: open file picker and call RestoreAsync with selected path.
        return Task.CompletedTask;
    }

    /// <summary>Refreshes the backup history list from disk.</summary>
    [RelayCommand]
    private Task RefreshHistoryAsync()
    {
        BackupHistory = new ObservableCollection<BackupFileInfo>(_backup.GetBackupHistory());
        return Task.CompletedTask;
    }

    /// <summary>Opens the backup folder in Explorer.</summary>
    [RelayCommand]
    private Task BrowseBackupFolderAsync()
    {
        // TODO: open folder via Process.Start.
        return Task.CompletedTask;
    }

    /// <summary>Deletes a single backup file.</summary>
    [RelayCommand]
    private async Task DeleteBackupAsync(BackupFileInfo file)
    {
        if (File.Exists(file.FilePath))
        {
            File.Delete(file.FilePath);
        }

        await RefreshHistoryAsync();
    }

    /// <summary>Deletes all backup files before the selected date after confirmation.</summary>
    [RelayCommand]
    private async Task ClearBackupsBeforeDateAsync()
    {
        var count = await _backup.ClearBackupsAsync(SelectedClearDate.DateTime);
        StatusMessage = $"Deleted {count} backup(s) before {SelectedClearDate:d}.";
        await RefreshHistoryAsync();
    }

    /// <summary>Deletes ALL backup files after double confirmation.</summary>
    [RelayCommand]
    private async Task ClearAllBackupsAsync()
    {
        // TODO: show double-confirm dialog requiring the user to type "CLEAR ALL".
        var count = await _backup.ClearBackupsAsync(null);
        StatusMessage = $"Deleted {count} backup(s).";
        await RefreshHistoryAsync();
    }
}
