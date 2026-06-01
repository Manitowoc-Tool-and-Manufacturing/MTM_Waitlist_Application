using BCrypt.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist_Server.Admin.Helpers;
using MTM_Waitlist_Server.Core.Interfaces.FirstRun;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.ViewModels;

/// <summary>
/// Drives the three-step first-run setup wizard.
/// Raised <see cref="WizardCompleted"/> when Step 3 finishes successfully so
/// <c>MainWindow</c> can unlock navigation.
/// </summary>
public sealed partial class ViewModel_FirstRun : ObservableObject
{
    private readonly IService_FirstRun _firstRun;
    private readonly IService_SettingsStore _settingsStore;

    /// <summary>Fired after <see cref="CreateFirstUserAsync"/> succeeds.</summary>
    public event EventHandler? WizardCompleted;

    // ── Step tracking ─────────────────────────────────────────────────────────

    [ObservableProperty] public partial int CurrentStep { get; set; }
    [ObservableProperty] public partial FirstRunStatus ProbeStatus { get; set; }
    [ObservableProperty] public partial bool IsWorking { get; set; }
    [ObservableProperty] public partial string StatusMessage { get; set; }
    [ObservableProperty] public partial bool Step1Complete { get; set; }
    [ObservableProperty] public partial bool Step2Complete { get; set; }

    // ── Step 1 — privileged MySQL connection + bootstrap ─────────────────────

    [ObservableProperty] public partial string DbHost { get; set; }
    [ObservableProperty] public partial string DbPort { get; set; }
    [ObservableProperty] public partial string DbName { get; set; }
    // Privileged (e.g. root) account — used only during bootstrap, never stored.
    [ObservableProperty] public partial string DbAdminUsername { get; set; }
    [ObservableProperty] public partial string DbAdminPassword { get; set; }
    // Application MySQL user that will be created and granted access to the target DB.
    [ObservableProperty] public partial string DbAppUsername { get; set; }
    [ObservableProperty] public partial string DbAppPassword { get; set; }
    /// <summary>
    /// True when the probe found that the application MySQL user already exists on the server.
    /// When true the app-user creation fields are greyed out — the user was already created
    /// (e.g. on a previous partial run) and does not need to be re-created.
    /// </summary>
    private bool _appUserExists;
    /// <summary>Gets or sets whether the application MySQL user already exists on the server.</summary>
    public bool AppUserExists
    {
        get => _appUserExists;
        private set
        {
            if (SetProperty(ref _appUserExists, value))
            {
                OnPropertyChanged(nameof(NeedsExistingAppUserPassword));
                OnPropertyChanged(nameof(CanEditAppUsername));
                OnPropertyChanged(nameof(CanEditAppPassword));
                OnPropertyChanged(nameof(AppUserInputsOpacity));
                OnPropertyChanged(nameof(AppUserHelpText));
            }
        }
    }

    /// <summary>
    /// True when the app already has a saved password for the existing MySQL app user.
    /// When false, step 1 must allow the operator to enter a replacement password.
    /// </summary>
    public bool HasSavedAppUserPassword { get; private set; }

    /// <summary>True when step 1 must collect a replacement password for the existing MySQL app user.</summary>
    public bool NeedsExistingAppUserPassword => AppUserExists && !HasSavedAppUserPassword;

    /// <summary>Username stays locked when the MySQL user already exists.</summary>
    public bool CanEditAppUsername => !AppUserExists;

    /// <summary>Password stays editable when creating a new user or repairing an existing one with no saved password.</summary>
    public bool CanEditAppPassword => !AppUserExists || NeedsExistingAppUserPassword;

    /// <summary>Dims the app-user section only when it is fully reused without operator input.</summary>
    public double AppUserInputsOpacity => AppUserExists && !NeedsExistingAppUserPassword ? 0.45 : 1.0;

    /// <summary>Explains whether step 1 will create, reuse, or reset the application MySQL user password.</summary>
    public string AppUserHelpText => NeedsExistingAppUserPassword
        ? "This MySQL user already exists, but the saved password is unavailable. Enter a password below to reset that existing user."
        : "This account will own the database and is used by the server at runtime.";



    // ── Step 2 — migration log ────────────────────────────────────────────────

    [ObservableProperty] public partial string MigrationLog { get; set; }

    // ── Step 3 — first user fields ────────────────────────────────────────────

    [ObservableProperty] public partial string WindowsUsername { get; set; }
    [ObservableProperty] public partial string AppUsername { get; set; }
    [ObservableProperty] public partial string DisplayName { get; set; }
    [ObservableProperty] public partial string UserPassword { get; set; }
    [ObservableProperty] public partial string ConfirmPassword { get; set; }
    [ObservableProperty] public partial string SelectedRole { get; set; }

    /// <summary>Roles the first user may be assigned — bound as strings so SelectedItem round-trips cleanly.</summary>
    public IReadOnlyList<string> AvailableRoles { get; } = ["Admin", "Developer"];

    /// <summary>Initialises a new instance and pre-fills connection and user fields from current context.</summary>
    public ViewModel_FirstRun(IService_FirstRun firstRun, IService_SettingsStore settingsStore,
        Model_FirstRunProbeResult? initialProbe = null)
    {
        _firstRun = firstRun;
        _settingsStore = settingsStore;

        // Set defaults for fields that previously had inline initializers.
        CurrentStep = 1;
        StatusMessage = string.Empty;
        MigrationLog = string.Empty;
        AppUsername = string.Empty;
        DisplayName = string.Empty;
        UserPassword = string.Empty;
        ConfirmPassword = string.Empty;
        SelectedRole = "Admin";
        DbAdminUsername = "root";
        DbAdminPassword = string.Empty;
        DbAppPassword = string.Empty;

        // Pre-fill host/port/name from any existing saved settings so the user
        // doesn't have to re-type them on repeat visits to the wizard.
        var db = settingsStore.Get().Database;
        DbHost = db.Host;
        DbPort = db.Port.ToString();
        DbName = db.DatabaseName;
        HasSavedAppUserPassword = !string.IsNullOrWhiteSpace(db.UpdaterPassword);
        // App-user defaults are shown as hints; root credentials are never pre-filled.
        DbAppUsername = string.IsNullOrWhiteSpace(db.UpdaterUsername)
            ? "waitlist_admin_dbupdater"
            : db.UpdaterUsername;

        WindowsUsername = WindowsIdentity.GetCurrent().Name;

        // Probe immediately — if the app user already exists we can grey out the
        // creation fields without waiting for the admin password to be entered.
        _ = ProbeAppUserExistsAsync(db.Host, db.Port, db.UpdaterUsername, db.UpdaterPassword);
    }

    /// <summary>
    /// Attempts to connect to MySQL as the application user using any credentials already
    /// persisted in settings.  If the connection succeeds the user exists and the creation
    /// fields should be greyed out.  Failures are silently swallowed — the fields stay
    /// enabled so the user can still fill them in.
    /// </summary>
    private async Task ProbeAppUserExistsAsync(
        string host, int port, string appUsername, string appPassword)
    {
        if (string.IsNullOrWhiteSpace(appUsername) || string.IsNullOrWhiteSpace(appPassword))
        {
            return;
        }

        try
        {
            var csb = new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = (uint)port,
                UserID = appUsername,
                Password = appPassword,
                ConnectionTimeout = 5,
                AllowPublicKeyRetrieval = true,
                SslMode = MySqlSslMode.Preferred,
                // Do NOT specify a database — the schema may not exist yet.
                // We only want to verify the MySQL user account exists and can authenticate.
            };

            await using var conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync();

            // Connection succeeded — the MySQL user exists.
            AppUserExists = true;
        }
        catch
        {
            // Could not connect as the app user — either the user doesn't exist yet or
            // credentials are wrong.  Leave AppUserExists = false so fields stay enabled.
            AppUserExists = false;
        }
    }

    /// <summary>
    /// Called after DI construction to surface the probe result that opened the wizard,
    /// so the user can see the exact error before they attempt to correct credentials.
    /// </summary>
    internal void ApplyProbeResult(Model_FirstRunProbeResult probe)
    {
        ProbeStatus = probe.Status;
        // Don't surface the raw probe error here — it fires before the wizard opens
        // (e.g. "Access denied for waitlist_admin_dbupdater") and confuses the user
        // who hasn't entered credentials yet. Status messages appear after user actions.
    }

    // ── Step 1 — Bootstrap database ──────────────────────────────────────────

    /// <summary>
    /// Probes the MySQL server with the privileged credentials to check whether the
    /// application user already exists.  Called when the user finishes typing the
    /// host / port or privileged password so the UI can grey out the creation fields
    /// before they attempt to run setup.
    /// </summary>
    [RelayCommand]
    private async Task CheckAppUserAsync()
    {
        if (string.IsNullOrWhiteSpace(DbAdminPassword)) { return; }
        if (!int.TryParse(DbPort, out var port)) { return; }

        try
        {
            var csb = new MySqlConnectionStringBuilder
            {
                Server = DbHost,
                Port = (uint)port,
                UserID = DbAdminUsername,
                Password = DbAdminPassword,
                ConnectionTimeout = 5,
                AllowPublicKeyRetrieval = true,
                SslMode = MySqlSslMode.Preferred,
            };

            await using var conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM mysql.user WHERE User = @user";
            cmd.Parameters.AddWithValue("@user", DbAppUsername);
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
            AppUserExists = count > 0;
        }
        catch
        {
            // Ignore probe errors — the user may not have typed credentials yet.
            AppUserExists = false;
        }
    }

    /// <summary>
    /// Connects with the privileged MySQL account, creates the target database, creates
    /// the application MySQL user, grants privileges, and saves settings.
    /// </summary>
    [RelayCommand]
    private async Task SetupDatabaseAsync()
    {
        IsWorking = true;
        StatusMessage = "Creating database and application user…";
        Step1Complete = false;

        try
        {
            if (!int.TryParse(DbPort, out var port) || port < 1 || port > 65535)
            {
                StatusMessage = "Port must be a number between 1 and 65535.";
                return;
            }

            if (string.IsNullOrWhiteSpace(DbAdminPassword))
            {
                StatusMessage = "❌ Please enter the privileged MySQL account password.";
                return;
            }

            if (!AppUserExists && string.IsNullOrWhiteSpace(DbAppPassword))
            {
                StatusMessage = "❌ Please enter a password for the application database user.";
                return;
            }

            var error = await _firstRun.SetupDatabaseAsync(
                DbHost, port, DbName,
                DbAdminUsername, DbAdminPassword,
                DbAppUsername, DbAppPassword);

            if (error is not null)
            {
                StatusMessage = $"❌ {error}";
                return;
            }

            HasSavedAppUserPassword = !string.IsNullOrWhiteSpace(DbAppPassword)
                || HasSavedAppUserPassword;
            OnPropertyChanged(nameof(NeedsExistingAppUserPassword));
            OnPropertyChanged(nameof(CanEditAppPassword));
            OnPropertyChanged(nameof(AppUserInputsOpacity));
            OnPropertyChanged(nameof(AppUserHelpText));

            Step1Complete = true;
            StatusMessage = AppUserExists
                ? NeedsExistingAppUserPassword
                    ? $"✅ Database '{DbName}' ready. Existing user '{DbAppUsername}' password was reset and access refreshed."
                    : $"✅ Database '{DbName}' ready. User '{DbAppUsername}' already existed — reused saved credentials."
                : $"✅ Database '{DbName}' created and user '{DbAppUsername}' configured.";
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <summary>Advances from Step 1 to Step 2 after a successful connection test.</summary>
    [RelayCommand]
    private void GoToStep2()
    {
        if (!Step1Complete) { return; }
        CurrentStep = 2;
        StatusMessage = string.Empty;

        // Skip Step 2 if schema already exists — probe told us only NoAdminUser.
        if (ProbeStatus == FirstRunStatus.NoAdminUser)
        {
            Step2Complete = true;
            CurrentStep = 3;
        }
    }

    // ── Step 2 — Bootstrap Migration ─────────────────────────────────────────

    /// <summary>
    /// Runs the baseline compare-and-apply workflow against the target database using
    /// the runtime application user saved by Step 1.
    /// </summary>
    [RelayCommand]
    private async Task RunBootstrapAsync(CancellationToken ct)
    {
        IsWorking = true;
        MigrationLog = string.Empty;
        StatusMessage = "Comparing SQL definitions to the database…";

        try
        {
            var result = await _firstRun.BootstrapSchemaAsync(
                new Progress<string>(AppendLog),
                ct);

            if (!result.IsSuccess)
            {
                StatusMessage = $"❌ {result.ErrorMessage}";
                AppendLog($"❌ {result.ErrorMessage}");
                return;
            }

            Step2Complete = true;
            StatusMessage = $"✅ Baseline compare-and-apply complete — {result.MigrationsApplied} object(s) updated.";
            CurrentStep = 3;
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ {ex.Message}";
            AppendLog($"❌ Error: {ex.Message}");
        }
        finally
        {
            IsWorking = false;
        }
    }

    // ── Step 3 — Create First Admin User ─────────────────────────────────────

    /// <summary>Creates the first admin user and marks the wizard as complete.</summary>
    [RelayCommand]
    private async Task CreateFirstUserAsync()
    {
        if (!ValidateStep3()) { return; }

        IsWorking = true;
        StatusMessage = "Creating user…";

        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(UserPassword);
            await _firstRun.CreateFirstUserAsync(
                WindowsUsername, AppUsername, DisplayName, hash, SelectedRole);

            await _firstRun.MarkCompleteAsync();

            StatusMessage = $"✅ User '{DisplayName}' created. Setup complete.";
            WizardCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ {ex.Message}";
        }
        finally
        {
            IsWorking = false;
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private bool ValidateStep3()
    {
        if (string.IsNullOrWhiteSpace(WindowsUsername))
        {
            StatusMessage = "Windows username is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(AppUsername))
        {
            StatusMessage = "App username is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            StatusMessage = "Display name is required.";
            return false;
        }
        if (UserPassword.Length < 8)
        {
            StatusMessage = "Password must be at least 8 characters.";
            return false;
        }
        if (UserPassword != ConfirmPassword)
        {
            StatusMessage = "Passwords do not match.";
            return false;
        }
        return true;
    }

    private void AppendLog(string line)
    {
        MigrationLog += (MigrationLog.Length > 0 ? "\n" : string.Empty) + line;
    }
}
