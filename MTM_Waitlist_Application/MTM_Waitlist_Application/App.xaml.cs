using Core.Constants.Auth;
using Feature.Auth.Views.Login;

namespace MTM_Waitlist_Application
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private Window? _window;

        public App(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Show login immediately — session validation runs asynchronously after
            // the window is open to avoid blocking the UI thread with WinRT calls.
            var loginPage = _serviceProvider.GetRequiredService<View_Auth_Login>();
            loginPage.Authenticated += OnAuthenticated;
            _window = new Window(loginPage);

            // Check for a valid stored session on a background thread.
            // If one exists, swap straight to the shell without user interaction.
            _ = Task.Run(async () =>
            {
                if (await HasValidStoredSessionAsync())
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_window is not null)
                        {
                            _window.Page = CreateShellForCurrentRole();
                        }
                    });
                }
            });

            return _window;
        }

        private void OnAuthenticated(object? sender, EventArgs e)
        {
            if (_window is not null)
            {
                _window.Page = CreateShellForCurrentRole();
            }
        }

        private static async Task<bool> HasValidStoredSessionAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync(Constants_AuthStorage.AuthTokenKey);
                var expiresAtText = await SecureStorage.GetAsync(Constants_AuthStorage.AuthTokenExpiresAtKey);

                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expiresAtText))
                {
                    return false;
                }

                return DateTimeOffset.TryParse(expiresAtText, out var expiresAt)
                    && expiresAt > DateTimeOffset.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        private static AppShell CreateShellForCurrentRole()
        {
            // All currently implemented roles land on Dashboard because it is the only
            // authenticated feature surface available today.
            return new AppShell();
        }
    }
}
