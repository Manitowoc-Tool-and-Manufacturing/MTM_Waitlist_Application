# DATABASE-04: Backup & Restore

---

## Confirmed Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Backup format | **`mysqldump` logical backup.** User can configure the save folder; default is `C:\MTM\WaitlistBackups\` (auto-created) |
| 2 | Schema + data or separate? | **Combined by default.** "Schema only" mode available as a checkbox option |
| 3 | Default backup folder | `C:\MTM\WaitlistBackups\` — created if missing. User can change in Settings or via Browse on the Backup page |
| 4 | Restore from a different server | **Yes** — file picker accepts any `.sql` file, not only files in the backup folder |
| 5 | Active clients during restore | **Automated kill-switch timer is mandatory.** The restore wizard always issues a countdown to connected clients before proceeding. There is no "skip timer" option in the restore flow |
| 6 | How many backups to keep | **Maximum 1 month (30 days).** Automatic retention deletes backups older than 30 days after each run. Manual clear options: "Clear backups before date" (date picker) and "Clear all backups" |

---

## Overview

The Backup & Restore module provides:

1. **Manual backup** — trigger a dump on demand with one click.
2. **Scheduled automatic backup** — nightly dump at a configured time.
3. **Backup history** — list of available backup files with size, date, and status.
4. **Restore** — restore from any backup file (with safety confirmation and automatic client kill).

All backup and restore operations run via `mysqldump` / `mysql` command-line tools called through `System.Diagnostics.Process`. The admin app captures stdout/stderr for real-time progress display.

---

## Backup Workflow

### Manual Backup

1. User clicks "Back Up Now" button.
2. Admin app verifies `mysqldump.exe` path is set (from Settings).
3. A timestamped filename is generated: `mtm_waitlist_2026-05-10_14-30-00.sql`
4. Progress panel opens showing real-time stdout from `mysqldump`.
5. On completion: checksum (`SHA-256`) is calculated and stored alongside the file as `*.sha256`.
6. Backup history list refreshes.

```
Filename:  mtm_waitlist_2026-05-10_14-30-00.sql
Size:      1.2 MB
SHA-256:   a3f9c1...
Duration:  4.2 seconds
Status:    ✅ Success
```

### Backup Command

```
mysqldump.exe
  --host=localhost
  --port=3306
  --user=waitlist_admin_dbupdater
  --password=***
  --single-transaction          // InnoDB-safe — no table locks
  --routines                    // includes stored procedures
  --triggers                    // includes triggers
  --set-gtid-purged=OFF         // avoid GTID issues on restore
  --result-file="<output_path>"
  mtm_waitlist
```

`--single-transaction` is critical for InnoDB tables — it starts a consistent snapshot without locking tables, so the API can continue serving requests during the backup.

`--routines` and `--triggers` ensure stored procedures and triggers are included, so restoring from this backup gives a fully functional database without needing to run the individual procedure/trigger scripts separately.

---

## Automatic Backup (Scheduled)

### Scheduler

The admin app uses a background `BackgroundService` (registered in the ASP.NET host) that wakes at the configured time each day:

```csharp
public class BackupSchedulerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var scheduledTime = _settings.Backup.AutoBackupSchedule;  // e.g., 02:00
            var nextRun = now.Date.Add(scheduledTime);
            if (nextRun <= now) nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            await Task.Delay(delay, ct);

            if (!ct.IsCancellationRequested)
                await _backupService.RunBackupAsync(BackupType.Automatic, ct);
        }
    }
}
```

### Retention Policy

After each automatic backup completes, the scheduler applies retention:
1. Delete backups older than `RetentionDays` (default: 30 days — approximately one month of nightly backups).
2. Log deletions to the activity log.

The date-based 30-day rule is the **only** automatic retention mechanism — there is no secondary file-count cap. Manual cleanup beyond this is provided through the Backup History UI (see below).

---

## Backup History UI

```
┌─────────────────────────────────────────────────────────────────┐
│  BACKUP & RESTORE                [Back Up Now]  [↻ Refresh]     │
├─────────────────────────────────────────────────────────────────┤
│  Backup Folder: C:\MTM\WaitlistBackups         [Browse…]        │
│  Next Auto Backup: Today at 02:00                               │
├────────────────────┬──────────┬──────────┬──────────┬──────────┤
│ File               │ Date     │ Size     │ Type     │ Actions  │
├────────────────────┼──────────┼──────────┼──────────┼──────────┤
│ mtm_waitlist_…sql  │ 05/10 14:30│ 1.2 MB │ Manual   │ [Restore]│
│ mtm_waitlist_…sql  │ 05/10 02:00│ 1.1 MB │ Auto     │ [Restore]│
│ mtm_waitlist_…sql  │ 05/09 02:00│ 1.1 MB │ Auto     │ [Restore]│
│ ...                │          │          │          │          │
├────────────────────┴──────────┴──────────┴──────────┴──────────┤
│  [Restore from File…]    Restore from a backup not listed above │
├─────────────────────────────────────────────────────────────────┤
│  MANUAL CLEANUP                                                 │
│  Clear backups before: [05/01/2026  ▼]  [Clear Before Date]    │
│                                         [Clear All Backups]     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Restore Workflow

Restore is a destructive operation that will temporarily disconnect all clients. The UI enforces a confirmation dialog and automates the safe sequence:

### Step 1 — Confirm

```
⚠ RESTORE WILL WIPE THE CURRENT DATABASE

You are about to restore:
  mtm_waitlist_2026-05-10_14-30-00.sql (1.2 MB)

This will:
  1. Notify all connected clients (5-minute warning)
  2. Stop the REST API
  3. DROP and recreate the mtm_waitlist database
  4. Restart the REST API

Type RESTORE to confirm:  [___________]  [Cancel]  [Proceed →]
```

The confirmation text field must contain exactly `RESTORE` (case-sensitive) before Proceed is enabled.

### Step 2 — Shutdown Clients (Mandatory Timer)

The restore service sets `IService_KillSwitch.IsRestoreInProgress = true` and calls `IService_KillSwitch.SetShutdownSignal(new ShutdownTarget(ShutdownTargetType.All), warningSeconds: 300)` automatically (DATABASE-05). The countdown timer **cannot be skipped or reduced to zero** in the restore flow — at minimum 60 seconds of warning must be given. The "Proceed" button in the restore confirm dialog triggers the countdown immediately; there is no instant-proceed path. The service waits for all connected clients to disconnect OR for the full countdown to expire before continuing.

### Step 3 — Stop API

`IHostApplicationLifetime.StopApplication()` stops the Kestrel listener gracefully. The admin UI remains visible (it is the host process — the API is a guest service within it). The status bar shows: `● API Stopped — Restore in progress`.

### Step 4 — Run Restore

```
mysql.exe
  --host=localhost
  --port=3306
  --user=waitlist_admin_dbupdater
  --password=***
  < "mtm_waitlist_2026-05-10_14-30-00.sql"
```

Progress output streams to the UI in real-time. Estimated time shown (based on file size).

### Step 5 — Verify Integrity

After restore, the service runs:
```sql
SELECT COUNT(*) FROM information_schema.TABLES WHERE table_schema = 'mtm_waitlist';
```
If the expected table count is found → restore successful.

Optionally, recalculate SHA-256 of the dump that was just restored and compare to the stored `.sha256` file to confirm the file was not corrupted.

### Step 6 — Restart API

`CreateHostBuilder().Build().RunAsync()` restarts Kestrel. Status bar returns to: `● Running on :5000`.

---

## Backup Progress Panel

The backup and restore operations run `Process` asynchronously and stream stdout/stderr to an `ObservableCollection<string> _progressLines` in the ViewModel. The UI shows a scrollable log panel with the live output:

```
[14:30:01] Starting backup of mtm_waitlist...
[14:30:01] mysqldump --single-transaction --routines --triggers ...
[14:30:03] Dumping table WaitlistEntries (47 rows)
[14:30:04] Dumping table Users (12 rows)
[14:30:04] Backup complete — 1.2 MB
[14:30:04] SHA-256: a3f9c1...
[14:30:04] ✅ Backup saved to C:\MTM\WaitlistBackups\mtm_waitlist_2026-05-10_14-30-00.sql
```

---

## Schema-Only Backup Option

A checkbox in the Backup section enables "Schema only" mode:

```
mysqldump --no-data --routines --triggers ...
```

Produces a smaller file with only `CREATE TABLE`, `CREATE PROCEDURE`, `CREATE TRIGGER` statements — no `INSERT` rows. Useful when sharing the schema with a developer who does not need real data.

---

## `IService_Backup` Interface

```csharp
// Core/Interfaces/Backup/IService_Backup.cs
public interface IService_Backup
{
    Task<BackupResult> RunBackupAsync(BackupType type, bool schemaOnly = false, CancellationToken ct = default);
    Task<RestoreResult> RunRestoreAsync(string backupFilePath, IProgress<string> progress, CancellationToken ct = default);
    Task ApplyRetentionPolicyAsync();              // deletes backups older than RetentionDays (30)
    Task<int> ClearBackupsAsync(DateTime? beforeDate = null);  // null = clear all; date = clear before that date
    IReadOnlyList<BackupFileInfo> GetBackupHistory();
    bool VerifyChecksum(string backupFilePath);
}
```

---

## New Core Models

```
Core/Models/Backup/
  BackupResult.cs       ← IsSuccess, FilePath, FileSize, DurationMs, ErrorMessage
  RestoreResult.cs      ← IsSuccess, TablesRestored, ErrorMessage
  BackupFileInfo.cs     ← FileName, FilePath, SizeBytes, CreatedAt, Type (Auto/Manual), HasChecksum
  BackupType.cs (enum)  ← Manual, Automatic, SchemaOnly
```

---

## ViewModel

```
Module.Backup/
  ViewModels/
    ViewModel_Backup.cs
  Views/
    View_Backup.xaml
    View_Backup.xaml.cs
```

Key commands:
```csharp
[RelayCommand] BackUpNowAsync()
[RelayCommand] RestoreAsync(BackupFileInfo file)
[RelayCommand] RestoreFromFileAsync()              // opens file picker
[RelayCommand] RefreshHistoryAsync()
[RelayCommand] BrowseBackupFolderAsync()
[RelayCommand] DeleteBackupAsync(BackupFileInfo file)
[RelayCommand] ClearBackupsBeforeDateAsync()       // uses _selectedClearDate; confirms before executing
[RelayCommand] ClearAllBackupsAsync()              // double-confirm dialog (type CLEAR ALL)
```

Additional observable property needed:
```csharp
[ObservableProperty] DateTimeOffset _selectedClearDate  // bound to date picker in Manual Cleanup section
```

---

## Open Decisions

- **Backup encryption:** Not required. Backup folder is restricted to IT via filesystem permissions.
- **Cloud/offsite backup:** For v1, local filesystem only. A v2 enhancement could copy completed backups to a network share (UNC path) or Azure Blob Storage.
- **Email notification on backup failure:** Not needed. Backup failure is surfaced as a warning banner in the dashboard and written to the activity log. No email notification will be added.
- **Backup during business hours:** `--single-transaction` makes the backup safe to run while the API is active. The default schedule of 02:00 avoids peak-hours I/O pressure. No business-hours restriction needed.
