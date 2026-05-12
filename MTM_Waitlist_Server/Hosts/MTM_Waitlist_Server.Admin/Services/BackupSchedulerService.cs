using Microsoft.Extensions.Hosting;
using MTM_Waitlist_Server.Core.Interfaces.Backup;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Backup;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Background service that triggers a nightly automatic backup at the time
/// configured in <see cref="Core.Models.Settings.BackupSettings.AutoBackupTime"/>.
/// After each backup it applies the retention policy to remove old files.
/// </summary>
internal sealed class BackupSchedulerService : BackgroundService
{
    private readonly IService_Backup _backup;
    private readonly IService_SettingsStore _settingsStore;

    public BackupSchedulerService(IService_Backup backup, IService_SettingsStore settingsStore)
    {
        _backup = backup;
        _settingsStore = settingsStore;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var settings = _settingsStore.Get().Backup;

            if (!settings.AutoBackupEnabled)
            {
                // Check again in 5 minutes in case settings change while running.
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
                continue;
            }

            var scheduledTime = ParseScheduledTime(settings.AutoBackupTime);
            var now = DateTime.Now;
            var nextRun = now.Date.Add(scheduledTime);

            // If we already passed today's run time, schedule for tomorrow.
            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            await Task.Delay(delay, ct);

            if (ct.IsCancellationRequested)
            {
                break;
            }

            await _backup.RunBackupAsync(BackupType.Automatic, schemaOnly: false, ct);
            await _backup.ApplyRetentionPolicyAsync();
        }
    }

    private static TimeSpan ParseScheduledTime(string hhMm)
    {
        if (TimeSpan.TryParseExact(hhMm, @"hh\:mm", null, out var result))
        {
            return result;
        }

        // Default to 02:00 if the stored value is malformed.
        return new TimeSpan(2, 0, 0);
    }
}
