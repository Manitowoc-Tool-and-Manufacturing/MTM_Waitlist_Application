using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces.Auth;
using System.Security.Principal;

namespace Feature.Auth.ViewModels.Login;

/// <summary>
/// ViewModel for the login screen.
/// Manages credential input, validation, and delegates authentication to <see cref="IService_Auth"/>.
/// </summary>
public partial class ViewModel_Auth_Login : ObservableObject
{
    private readonly IService_Auth _authService;

    /// <summary>
    /// Raised when the user has been successfully authenticated.
    /// </summary>
    public event EventHandler? Authenticated;

    /// <summary>
    /// The Windows domain username entered by the user.
    /// </summary>
    [ObservableProperty]
    private string _username = string.Empty;

    /// <summary>
    /// The password or floor PIN entered by the user.
    /// </summary>
    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary>
    /// Validation or server error message displayed beneath the form.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Indicates whether an authentication request is in progress.
    /// Drives the activity indicator on both platform layouts.
    /// </summary>
    [ObservableProperty]
    private bool _isAuthenticating;

    /// <summary>
    /// Indicates whether the app is determining login mode or attempting auto-login.
    /// </summary>
    [ObservableProperty]
    private bool _isCheckingWorkstation;

    /// <summary>
    /// Indicates whether the current machine requires manual credential entry.
    /// </summary>
    [ObservableProperty]
    private bool _isSharedWorkstation = true;

    /// <summary>
    /// Combined busy state for progress indicators.
    /// </summary>
    public bool IsBusy => IsAuthenticating || IsCheckingWorkstation;

    /// <summary>
    /// Initialises the ViewModel with its injected auth service.
    /// </summary>
    public ViewModel_Auth_Login(IService_Auth authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Performs first-load initialisation; pre-populates the username from the
    /// current Windows identity when running on Windows.
    /// </summary>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        ErrorMessage = string.Empty;

        if (!string.IsNullOrWhiteSpace(Username))
        {
            return;
        }

#if WINDOWS
        var windowsUsername = await Task.Run(() =>
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return identity.Name;
        });

        if (!string.IsNullOrWhiteSpace(windowsUsername) && string.IsNullOrWhiteSpace(Username))
        {
            Username = windowsUsername;
        }

        if (string.IsNullOrWhiteSpace(windowsUsername))
        {
            return;
        }

        IsCheckingWorkstation = true;

        try
        {
            var loginMode = await _authService.CheckWorkstationAsync(windowsUsername);
            if (!loginMode.IsSuccess || loginMode.Data is null)
            {
                IsSharedWorkstation = true;
                return;
            }

            IsSharedWorkstation = loginMode.Data.IsSharedWorkstation;

            if (!loginMode.Data.IsSharedWorkstation)
            {
                var autoLogin = await _authService.AutoLoginAsync(windowsUsername);
                if (autoLogin.IsSuccess)
                {
                    Password = string.Empty;
                    Authenticated?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        finally
        {
            IsCheckingWorkstation = false;
        }
#else
        await Task.CompletedTask;
#endif
    }

    /// <summary>
    /// Submits the credentials to <see cref="IService_Auth.LoginAsync"/> and raises
    /// <see cref="Authenticated"/> on success.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

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

        IsAuthenticating = true;

        var result = await _authService.LoginAsync(Username, Password);

        IsAuthenticating = false;

        if (result.IsSuccess)
        {
            Password = string.Empty;
            Authenticated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
        }
    }

    /// <summary>
    /// Clears the password field.
    /// </summary>
    [RelayCommand]
    private void ClearPassword()
    {
        Password = string.Empty;
        ErrorMessage = string.Empty;
    }

    partial void OnIsAuthenticatingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
    }

    partial void OnIsCheckingWorkstationChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
    }
}
