using Core.Constants.Auth;
using Feature.Auth.Views.Login;
using Microsoft.Extensions.Logging;

namespace MTM_Waitlist_Application
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<App> _logger;
        private Window? _window;

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
            var loginPage = _serviceProvider.GetRequiredService<View_Auth_Login>();
            loginPage.Authenticated += OnAuthenticated;
            _window = new Window(loginPage);

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
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_window is not null)
                        {
                            _window.Page = CreateShellForCurrentRole();
                            _logger.LogInformation("[STARTUP] Shell swapped — app ready");
                        }
                    });
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
            if (_window is not null)
            {
                _window.Page = CreateShellForCurrentRole();
                _logger.LogInformation("[STARTUP] OnAuthenticated — shell active");
            }
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
