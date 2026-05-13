using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Core.Interfaces.Lifecycle;
using MTM_Waitlist_Application.WinUI.Views.Dashboard;
using MTM_Waitlist_Application.WinUI.Views.Login;

namespace MTM_Waitlist_Application.WinUI;

/// <summary>
/// Application shell window.
/// Starts by showing <see cref="LoginPage"/> in a full-screen overlay.
/// After successful authentication, hides the overlay, reveals the
/// <see cref="NavigationView"/> sidebar, and navigates to the Dashboard.
/// </summary>
public sealed partial class MainWindow : Window
{
    private static MainWindow? _current;

    private readonly IService_AppLifecycle _appLifecycle;
    private readonly ILogger<MainWindow> _logger;
    private LoginPage? _loginPage;
    private int _shellActivated;

    /// <summary>
    /// Initializes the window and navigates to the login page.
    /// </summary>
    public MainWindow()
    {
        _appLifecycle = App.Services.GetRequiredService<IService_AppLifecycle>();
        _logger = App.Services.GetRequiredService<ILogger<MainWindow>>();
        _current = this;

        _logger.LogInformation("[STARTUP][WinUI] MainWindow construction starting");
        InitializeComponent();
        Closed += OnClosed;
        _logger.LogInformation("[STARTUP][WinUI] MainWindow XAML initialized");

        // Size and center the window on launch
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        _logger.LogInformation("[STARTUP][WinUI] MainWindow resized to 1280x800");

        // Navigate to the login page first
        _loginPage = App.Services.GetRequiredService<LoginPage>();
        _loginPage.Authenticated += OnAuthenticated;
        LoginFrame.Content = _loginPage;
        _logger.LogInformation("[STARTUP][WinUI] LoginPage loaded into LoginFrame");
    }

    /// <summary>
    /// Shows the kill-switch shutdown warning overlay on the active main window.
    /// </summary>
    public static void ShowShutdownWarning(string message, int warningSeconds)
    {
        _current?.DispatcherQueue.TryEnqueue(() =>
            _current.ShowShutdownWarningCore(message, warningSeconds));
    }

    private void ShowShutdownWarningCore(string message, int warningSeconds)
    {
        ShutdownMessageText.Text = string.IsNullOrWhiteSpace(message)
            ? "The application has been asked to close by an administrator."
            : message;

        ShutdownCountdownText.Text = warningSeconds > 0
            ? $"Closing in {warningSeconds} seconds."
            : "Closing now.";

        ShutdownOverlay.Visibility = Visibility.Visible;
        _logger.LogWarning(
            "[KillSwitch][WinUI] Shutdown warning displayed — warningSeconds={WarningSeconds}",
            warningSeconds);
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        _logger.LogInformation("[STARTUP][WinUI] MainWindow closing; stopping authenticated lifecycle");
        await _appLifecycle.StopAuthenticatedSessionAsync();
        _current = null;
    }

    /// <summary>
    /// Called when <see cref="LoginPage"/> raises <c>Authenticated</c>.
    /// Collapses the login overlay and reveals the NavigationView shell.
    /// </summary>
    private void OnAuthenticated(object? sender, EventArgs e)
    {
        if (Interlocked.CompareExchange(ref _shellActivated, 1, 0) != 0)
        {
            _logger.LogInformation("[STARTUP][WinUI] Shell activation already completed; ignoring duplicate authentication event");
            return;
        }

        _logger.LogInformation("[STARTUP][WinUI] Authentication completed; activating shell");

        var authenticatedUsername = _loginPage?.ViewModel.AuthenticatedUsername ?? string.Empty;
        var authenticatedDisplayName = _loginPage?.ViewModel.AuthenticatedDisplayName ?? string.Empty;

        if (_loginPage is not null)
        {
            _loginPage.Authenticated -= OnAuthenticated;
            _loginPage = null;
        }

        LoginOverlay.Visibility = Visibility.Collapsed;
        ShellNav.Visibility = Visibility.Visible;

        // Select Dashboard as the default landing page
        ShellNav.SelectedItem = NavDashboard;
        ShellFrame.Navigate(typeof(DashboardPage));
        _logger.LogInformation("[STARTUP][WinUI] Dashboard navigation completed; starting authenticated lifecycle");

        _ = Task.Run(async () =>
        {
            await _appLifecycle.StartAuthenticatedSessionAsync(authenticatedUsername, authenticatedDisplayName);
            _logger.LogInformation("[STARTUP][WinUI] MainWindow idle on Dashboard");
        });
    }

    /// <summary>
    /// Handles NavigationView item selection and navigates the shell Frame.
    /// </summary>
    private void ShellNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            _ => null
        };

        if (pageType is not null && ShellFrame.CurrentSourcePageType != pageType)
        {
            ShellFrame.Navigate(pageType);
        }
    }
}
