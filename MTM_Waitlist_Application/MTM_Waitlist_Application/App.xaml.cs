using Core.Constants.Auth;
using Core.Interfaces.KillSwitch;
using Core.Models.KillSwitch;
using Feature.Auth.Views.Login;
using Microsoft.Extensions.Logging;

namespace MTM_Waitlist_Application
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<App> _logger;
        private Window? _window;
        private View_Auth_Login? _loginPage;
        private int _shellSwapped; // 0 = not yet swapped, 1 = swapped; Interlocked guard against double swap

        public App(IServiceProvider serviceProvider, ILogger<App> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation("[STARTUP] App constructor — InitializeComponent starting");
            InitializeComponent();
            _logger.LogInformation("[STARTUP] App constructor — InitializeComponent complete");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _logger.LogInformation("[STARTUP] CreateWindow — resolving View_Auth_Login");

            // Show login immediately — session validation runs asynchronously after
            // the window is open to avoid blocking the UI thread with WinRT calls.
            _loginPage = _serviceProvider.GetRequiredService<View_Auth_Login>();
            _loginPage.Authenticated += OnAuthenticated;
            _window = new Window(_loginPage);

            _logger.LogInformation("[STARTUP] CreateWindow — login window created, starting session check");

            // Check for a valid stored session on a background thread.
            // If one exists, swap straight to the shell without user interaction.
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("[STARTUP] Session check — reading SecureStorage");
                var hasSession = await HasValidStoredSessionAsync();
                _logger.LogInformation("[STARTUP] Session check — valid session found: {HasSession}", hasSession);

                if (hasSession)
                {
                    _logger.LogInformation("[STARTUP] Session valid — swapping to shell on main thread");
                    MainThread.BeginInvokeOnMainThread(() => SwapToShell("[STARTUP] Shell swapped — app ready (session)"));
                }
                else
                {
                    _logger.LogInformation("[STARTUP] No valid session — login screen shown");
                }
            });

            return _window;
        }

        private void OnAuthenticated(object? sender, EventArgs e)
        {
            _logger.LogInformation("[STARTUP] OnAuthenticated — swapping to shell");
            SwapToShell("[STARTUP] OnAuthenticated — shell active");
        }

        /// <summary>
        /// Swaps the window page to the authenticated shell exactly once.
        /// Uses an Interlocked flag to prevent a double swap when both the stored-session
        /// check and the login-page InitializeCommand fire in parallel.
        /// Must be called on the main thread.
        /// </summary>
        private void SwapToShell(string logMessage)
        {
            if (Interlocked.CompareExchange(ref _shellSwapped, 1, 0) != 0)
            {
                _logger.LogInformation("[STARTUP] SwapToShell — already swapped, ignoring duplicate call");
                return;
            }

            // Unsubscribe so the login page cannot trigger a second swap after the shell is active.
            if (_loginPage is not null)
            {
                _loginPage.Authenticated -= OnAuthenticated;
                _loginPage = null;
            }

            if (_window is not null)
            {
                _window.Page = CreateShellForCurrentRole();
                _logger.LogInformation("{LogMessage}", logMessage);
            }

            // Start the kill-switch heartbeat now that the user is authenticated.
            // Runs on a background thread so SecureStorage reads don't block the UI.
            _ = Task.Run(StartKillSwitchAsync);
        }

        /// <summary>
        /// Reads the authenticated user's credentials from SecureStorage and starts
        /// the kill-switch heartbeat loop.
        /// </summary>
        private async Task StartKillSwitchAsync()
        {
            try
            {
                var username = await MainThread.InvokeOnMainThreadAsync(
                    () => SecureStorage.GetAsync(Constants_AuthStorage.AuthUsernameKey)) ?? string.Empty;
                var displayName = await MainThread.InvokeOnMainThreadAsync(
                    () => SecureStorage.GetAsync(Constants_AuthStorage.AuthDisplayNameKey)) ?? string.Empty;

                // Workstation name is not stored in SecureStorage — pass null so the
                // admin console will show the machine name from DeviceInfo.Name instead.
                var killSwitch = _serviceProvider.GetRequiredService<IService_KillSwitch>();
                killSwitch.ShutdownSignalReceived += OnShutdownSignalReceived;
                killSwitch.StartHeartbeat(username, displayName, workstationName: null);

                _logger.LogInformation("[KillSwitch] Heartbeat started — username={Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KillSwitch] Failed to start heartbeat");
            }
        }

        /// <summary>
        /// Handles a shutdown signal from the admin console.
        /// Displays a countdown dialog and closes the application when the warning expires.
        /// Guaranteed to run on the main thread (raised by the kill-switch service).
        /// </summary>
        private async void OnShutdownSignalReceived(object? sender, Model_KillSwitch_Signal signal)
        {
            _logger.LogWarning(
                "[KillSwitch] Shutdown signal received — target={Target}, warningSeconds={Warning}, message={Msg}",
                signal.Target, signal.WarningSeconds, signal.Message);

            // Stop sending further heartbeats — the session is ending.
            if (sender is IService_KillSwitch ks)
            {
                ks.ShutdownSignalReceived -= OnShutdownSignalReceived;
                ks.StopHeartbeat();
            }

            if (signal.WarningSeconds > 0 && _window?.Page is not null)
            {
                // Show a non-cancellable countdown alert for the warning period.
                // Blocks the UI intentionally — the user should not be able to continue working.
                await _window.Page.DisplayAlertAsync(
                    "⚠️  Application Closing",
                    $"{signal.Message}\n\nThis application will close in {signal.WarningSeconds} seconds.",
                    "OK");

                await Task.Delay(TimeSpan.FromSeconds(signal.WarningSeconds));
            }

            _logger.LogInformation("[KillSwitch] Closing application per admin signal");
            Application.Current?.Quit();
        }

        private async Task<bool> HasValidStoredSessionAsync()
        {
            try
            {
                _logger.LogInformation("[STARTUP] HasValidStoredSessionAsync — reading token (on main thread)");

                // SecureStorage on WinUI uses the WinRT PasswordVault which requires
                // the UI thread. We marshal each call back to the main thread to avoid
                // the InvalidOperationException thrown when called from a background thread.
                var token = await MainThread.InvokeOnMainThreadAsync(
                    () => SecureStorage.GetAsync(Constants_AuthStorage.AuthTokenKey));

                _logger.LogInformation("[STARTUP] HasValidStoredSessionAsync — token present: {HasToken}", !string.IsNullOrWhiteSpace(token));

                var expiresAtText = await MainThread.InvokeOnMainThreadAsync(
                    () => SecureStorage.GetAsync(Constants_AuthStorage.AuthTokenExpiresAtKey));

                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expiresAtText))
                {
                    _logger.LogInformation("[STARTUP] HasValidStoredSessionAsync — no stored session");
                    return false;
                }

                var valid = DateTimeOffset.TryParse(expiresAtText, out var expiresAt)
                    && expiresAt > DateTimeOffset.UtcNow;

                _logger.LogInformation(
                    "[STARTUP] HasValidStoredSessionAsync — expiresAt={ExpiresAt}, valid={Valid}",
                    expiresAtText, valid);

                return valid;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[STARTUP] HasValidStoredSessionAsync — SecureStorage read failed");
                return false;
            }
        }

        private AppShell CreateShellForCurrentRole()
        {
            _logger.LogInformation("[STARTUP] CreateShellForCurrentRole — creating AppShell");
            // All currently implemented roles land on Dashboard because it is the only
            // authenticated feature surface available today.
            return new AppShell();
        }
    }
}
