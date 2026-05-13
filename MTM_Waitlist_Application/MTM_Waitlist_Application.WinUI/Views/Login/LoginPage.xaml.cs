using Feature.Auth.ViewModels.Login;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Application.WinUI.Converters;

namespace MTM_Waitlist_Application.WinUI.Views.Login;

/// <summary>
/// Code-behind for the WinUI 3 login page.
/// Resolves <see cref="ViewModel_Auth_Login"/> from DI, wires the Authenticated event,
/// and triggers workstation initialisation when the page loads.
/// </summary>
public sealed partial class LoginPage : Page
{
    /// <summary>
    /// Raised when the user has been successfully authenticated.
    /// <see cref="MainWindow"/> subscribes to this to swap from the login overlay to the shell.
    /// </summary>
    public event EventHandler? Authenticated;

    /// <summary>
    /// The ViewModel bound to this page via compiled x:Bind.
    /// </summary>
    public ViewModel_Auth_Login ViewModel { get; }

    /// <summary>
    /// Initializes the login page, resolves its ViewModel, and registers converters.
    /// </summary>
    public LoginPage()
    {
        ViewModel = App.Services.GetRequiredService<ViewModel_Auth_Login>();
        ViewModel.Authenticated += OnAuthenticated;

        this.Resources["BoolToVisibilityConverter"] = new BoolToVisibilityConverter();
        this.Resources["StringToVisibilityConverter"] = new StringToVisibilityConverter();

        InitializeComponent();

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeCommand.ExecuteAsync(null);
    }

    private void OnAuthenticated(object? sender, EventArgs e)
    {
        Authenticated?.Invoke(this, EventArgs.Empty);
    }
}
