using MTM_Waitlist_Server.Core.Interfaces.Backup;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Backup;
using MTM_Waitlist_Server.Core.Models.Settings;
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
/// Implements backup and restore using the <c>mysqldump</c> / <c>mysql</c> CLI tools.
/// All file I/O uses the backup folder configured in <see cref="BackupSettings"/>.
/// </summary>
internal sealed class Service_Backup : IService_Backup
{
    private const string FilePrefix = "mtm_waitlist_";
    private const string FileExtension = ".sql";
    private const string ChecksumExtension = ".sha256";
    private const string AutoTag = "_auto";

    private readonly IService_SettingsStore _settingsStore;

    public Service_Backup(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <inheritdoc />
    public async Task<BackupResult> RunBackupAsync(
        BackupType type,
        bool schemaOnly = false,
        CancellationToken ct = default)
    {
        var settings = _settingsStore.Get();
        var db = settings.Database;
        var backup = settings.Backup;

        Directory.CreateDirectory(backup.BackupFolder);

        var tag = type == BackupType.Automatic ? AutoTag : string.Empty;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var fileName = $"{FilePrefix}{timestamp}{tag}{FileExtension}";
        var filePath = Path.Combine(backup.BackupFolder, fileName);

        var args = BuildDumpArgs(db, filePath, schemaOnly || type == BackupType.SchemaOnly);

        var sw = Stopwatch.StartNew();
        var (exitCode, errorText) = await RunProcessAsync(backup.MysqlDumpPath, args, ct);
        sw.Stop();

        if (exitCode != 0 || !File.Exists(filePath))
        {
            return new BackupResult(false, filePath, 0, sw.ElapsedMilliseconds,
                string.IsNullOrWhiteSpace(errorText) ? "mysqldump exited with a non-zero code." : errorText);
        }

        var fileSize = new FileInfo(filePath).Length;
        WriteChecksum(filePath);

        return new BackupResult(true, filePath, fileSize, sw.ElapsedMilliseconds, null);
    }

    /// <inheritdoc />
    public async Task<RestoreResult> RunRestoreAsync(
        string backupFilePath,
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        var settings = _settingsStore.Get();
        var db = settings.Database;

        // Locate the mysql CLI — assume it is next to mysqldump.
        var dumpDir = Path.GetDirectoryName(settings.Backup.MysqlDumpPath) ?? string.Empty;
        var mysqlExe = string.IsNullOrEmpty(dumpDir) ? "mysql" : Path.Combine(dumpDir, "mysql");

        var args = $"--host={db.Host} --port={db.Port} --user={db.UpdaterUsername} " +
                   $"--password={db.UpdaterPassword} {db.DatabaseName}";

        progress.Report($"[{DateTime.Now:HH:mm:ss}] Starting restore from {Path.GetFileName(backupFilePath)}…");

        var (exitCode, errorText) = await RunProcessWithStdinAsync(mysqlExe, args, backupFilePath, progress, ct);

        if (exitCode != 0)
        {
            return new RestoreResult(false, 0,
                string.IsNullOrWhiteSpace(errorText) ? "mysql exited with a non-zero code." : errorText);
        }

        var tableCount = await CountRestoredTablesAsync(db, ct);
        progress.Report($"[{DateTime.Now:HH:mm:ss}] ✅ Restore complete — {tableCount} tables.");
        return new RestoreResult(true, tableCount, null);
    }

    /// <inheritdoc />
    public Task ApplyRetentionPolicyAsync()
    {
        var settings = _settingsStore.Get();
        var cutoff = DateTime.Now.AddDays(-settings.Backup.RetentionDays);
        ClearBackupsInternal(cutoff);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> ClearBackupsAsync(DateTime? beforeDate = null)
    {
        int deleted = ClearBackupsInternal(beforeDate);
        return Task.FromResult(deleted);
    }

    /// <inheritdoc />
    public IReadOnlyList<BackupFileInfo> GetBackupHistory()
    {
        var folder = _settingsStore.Get().Backup.BackupFolder;
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(folder, $"{FilePrefix}*{FileExtension}")
            .OrderByDescending(f => f)
            .Select(path =>
            {
                var info = new FileInfo(path);
                var type = path.Contains(AutoTag) ? BackupType.Automatic : BackupType.Manual;
                var hasChecksum = File.Exists(path + ChecksumExtension);
                return new BackupFileInfo(info.Name, path, info.Length, info.CreationTime, type, hasChecksum);
            })
            .ToList();
    }

    /// <inheritdoc />
    public bool VerifyChecksum(string backupFilePath)
    {
        var checksumPath = backupFilePath + ChecksumExtension;
        if (!File.Exists(checksumPath) || !File.Exists(backupFilePath))
        {
            return false;
        }

        var stored = File.ReadAllText(checksumPath).Trim();
        var computed = ComputeSha256(backupFilePath);
        return string.Equals(stored, computed, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string BuildDumpArgs(DatabaseSettings db, string outputPath, bool schemaOnly)
    {
        var noData = schemaOnly ? "--no-data " : string.Empty;
        return $"--host={db.Host} --port={db.Port} --user={db.UpdaterUsername} " +
               $"--password={db.UpdaterPassword} --single-transaction --routines --triggers " +
               $"--set-gtid-purged=OFF {noData}--result-file=\"{outputPath}\" {db.DatabaseName}";
    }

    private static async Task<(int ExitCode, string ErrorText)> RunProcessAsync(
        string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {exe}");

        var errorText = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, errorText);
    }

    private static async Task<(int ExitCode, string ErrorText)> RunProcessWithStdinAsync(
        string exe, string args, string inputFile, IProgress<string> progress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {exe}");

        // Stream the SQL file into stdin.
        var stdinTask = Task.Run(async () =>
        {
            await using var fs = File.OpenRead(inputFile);
            await fs.CopyToAsync(process.StandardInput.BaseStream, ct);
            process.StandardInput.Close();
        }, ct);

        // Forward stdout lines as progress messages.
        var stdoutTask = Task.Run(async () =>
        {
            while (await process.StandardOutput.ReadLineAsync(ct) is { } line)
            {
                progress.Report($"[{DateTime.Now:HH:mm:ss}] {line}");
            }
        }, ct);

        var errorText = await process.StandardError.ReadToEndAsync(ct);
        await Task.WhenAll(stdinTask, stdoutTask);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, errorText);
    }

    private void WriteChecksum(string filePath)
    {
        var hash = ComputeSha256(filePath);
        File.WriteAllText(filePath + ChecksumExtension, hash);
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(filePath);
        var hashBytes = sha.ComputeHash(fs);
        return Convert.ToHexStringLower(hashBytes);
    }

    private int ClearBackupsInternal(DateTime? beforeDate)
    {
        var folder = _settingsStore.Get().Backup.BackupFolder;
        if (!Directory.Exists(folder))
        {
            return 0;
        }

        var files = Directory.EnumerateFiles(folder, $"{FilePrefix}*{FileExtension}");
        int deleted = 0;

        foreach (var path in files)
        {
            var created = new FileInfo(path).CreationTime;
            if (beforeDate is null || created < beforeDate.Value)
            {
                File.Delete(path);
                var checksumPath = path + ChecksumExtension;
                if (File.Exists(checksumPath))
                {
                    File.Delete(checksumPath);
                }
                deleted++;
            }
        }

        return deleted;
    }

    private static async Task<int> CountRestoredTablesAsync(DatabaseSettings db, CancellationToken ct)
    {
        var csb = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Server                  = db.Host,
            Port                    = (uint)db.Port,
            Database                = "information_schema",
            UserID                  = db.UpdaterUsername,
            Password                = db.UpdaterPassword,
            ConnectionTimeout       = (uint)db.ConnectionTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode                 = MySqlConnector.MySqlSslMode.Preferred,
        };
        try
        {
            await using var conn = new MySqlConnector.MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM TABLES WHERE TABLE_SCHEMA = @schema";
            cmd.Parameters.AddWithValue("@schema", db.DatabaseName);
            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result);
        }
        catch
        {
            // Non-fatal — the restore succeeded even if we can't count tables.
            return 0;
        }
    }
}
