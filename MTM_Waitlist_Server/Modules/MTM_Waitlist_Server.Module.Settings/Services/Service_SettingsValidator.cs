using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Settings;
using System.Text.RegularExpressions;

namespace MTM_Waitlist_Server.Module.Settings.Services;

/// <summary>
/// Validates <see cref="ServerSettings"/> against the rules specified in DATABASE-03.
/// Returns a list of human-readable error strings; an empty list means the settings are valid.
/// </summary>
public sealed class Service_SettingsValidator : IService_SettingsValidator
{
    // DATABASE-03: database name must be lowercase, no spaces, [a-z0-9_]+
    private static readonly Regex DbNamePattern = new(@"^[a-z0-9_]+$", RegexOptions.Compiled);

    /// <inheritdoc/>
    public IReadOnlyList<string> Validate(ServerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var errors = new List<string>();

        ValidateDatabase(settings.Database, errors);
        ValidateApi(settings.Api, errors);
        ValidateBackup(settings.Backup, errors);
        ValidateVisual(settings.Visual, errors);
        ValidateNotifications(settings.Notifications, errors);

        return errors;
    }

    private static void ValidateDatabase(DatabaseSettings db, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(db.Host))
        {
            errors.Add("Database: Host is required.");
        }

        if (db.Port is < 1 or > 65535)
        {
            errors.Add($"Database: Port {db.Port} is out of range (1–65535).");
        }

        if (string.IsNullOrWhiteSpace(db.DatabaseName))
        {
            errors.Add("Database: DatabaseName is required.");
        }
        else if (!DbNamePattern.IsMatch(db.DatabaseName))
        {
            errors.Add("Database: DatabaseName must be lowercase with no spaces ([a-z0-9_] only).");
        }

        if (string.IsNullOrWhiteSpace(db.AppUsername))
        {
            errors.Add("Database: AppUsername is required.");
        }

        if (string.IsNullOrWhiteSpace(db.UpdaterUsername))
        {
            errors.Add("Database: UpdaterUsername is required.");
        }
    }

    private static void ValidateApi(ApiSettings api, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(api.ListenAddress))
        {
            errors.Add("API: ListenAddress is required.");
        }

        if (api.JwtExpiryMinutes <= 0)
        {
            errors.Add("API: JwtExpiryMinutes must be greater than 0.");
        }

        if (api.RefreshTokenExpiryDays <= 0)
        {
            errors.Add("API: RefreshTokenExpiryDays must be greater than 0.");
        }
    }

    private static void ValidateBackup(BackupSettings backup, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(backup.BackupFolder))
        {
            errors.Add("Backup: BackupFolder is required.");
        }

        if (string.IsNullOrWhiteSpace(backup.MysqlDumpPath))
        {
            errors.Add("Backup: MysqlDumpPath is required.");
        }

        if (backup.RetentionDays <= 0)
        {
            errors.Add("Backup: RetentionDays must be greater than 0.");
        }

        // Validate HH:mm format
        if (backup.AutoBackupEnabled &&
            !TimeOnly.TryParseExact(backup.AutoBackupTime, "HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
        {
            errors.Add("Backup: AutoBackupTime must be in HH:mm format (e.g. 02:00).");
        }
    }

    private static void ValidateVisual(VisualSettings visual, List<string> errors)
    {
        if (!visual.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(visual.Host))
        {
            errors.Add("Visual: Host is required when Visual integration is enabled.");
        }

        if (visual.Port is < 1 or > 65535)
        {
            errors.Add($"Visual: Port {visual.Port} is out of range (1–65535).");
        }

        if (string.IsNullOrWhiteSpace(visual.Username))
        {
            errors.Add("Visual: Username is required when Visual integration is enabled.");
        }
    }

    private static void ValidateNotifications(NotificationSettings notifications, List<string> errors)
    {
        if (notifications.KillSwitchDefaultWarningSeconds <= 0)
        {
            errors.Add("Notifications: KillSwitchDefaultWarningSeconds must be greater than 0.");
        }
    }
}
