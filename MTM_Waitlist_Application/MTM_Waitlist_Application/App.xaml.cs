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
            var loginPage = _serviceProvider.GetRequiredService<View_Auth_Login>();
            loginPage.Authenticated += OnAuthenticated;

            _window = new Window(loginPage);
            return _window;
        }

        private void OnAuthenticated(object? sender, EventArgs e)
        {
            if (_window is not null)
            {
                _window.Page = new AppShell();
            }
        }
    }
}
