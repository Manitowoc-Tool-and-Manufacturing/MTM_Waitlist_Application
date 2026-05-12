using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces.Auth;

namespace Feature.Auth.ViewModels.Login;

/// <summary>
/// ViewModel for the authentication login screen.
/// Provides the first manual login slice while workstation-aware auto-login
/// endpoints are still being completed in the server solution.
/// </summary>
public partial class ViewModel_Auth_Login : ObservableObject
{
    private readonly IService_Auth _authService;

    /// <summary>
    /// Raised after a successful login so the view can transition to the app shell.
    /// </summary>
    public event EventHandler? Authenticated;

    [ObservableProperty]
    private bool _isAuthenticating;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Initialises a new instance with the required auth service.
    /// </summary>
    public ViewModel_Auth_Login(IService_Auth authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Performs first-load setup for the login page.
    /// </summary>
    [RelayCommand]
    private Task InitializeAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        {
            Username = Environment.UserName;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Attempts a manual username/password login against the API.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsAuthenticating)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Enter your Windows username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter your password or PIN.";
            return;
        }

        try
        {
            IsAuthenticating = true;
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(Username.Trim(), Password);
            if (!result.IsSuccess)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Unable to sign in."
                    : result.ErrorMessage;
                return;
            }

            Password = string.Empty;
            Authenticated?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsAuthenticating = false;
        }
    }

    /// <summary>
    /// Clears the current password entry.
    /// </summary>
    [RelayCommand]
    private Task ClearPasswordAsync()
    {
        Password = string.Empty;
        ErrorMessage = string.Empty;
        return Task.CompletedTask;
    }
}