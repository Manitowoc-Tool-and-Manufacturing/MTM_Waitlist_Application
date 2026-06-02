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
    /// Username returned by the most recent successful sign-in.
    /// </summary>
    public string AuthenticatedUsername { get; private set; } = string.Empty;

    /// <summary>
    /// Display name returned by the most recent successful sign-in.
    /// </summary>
    public string AuthenticatedDisplayName { get; private set; } = string.Empty;

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
    /// Startup or connection status shown above the credential form.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Indicates whether there is a startup or connection status message to show.
    /// </summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>
    /// Indicates whether there is an error message to show (status or validation).
    /// Used to show the Copy Error button.
    /// </summary>
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage) || HasStatusMessage;

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
    private bool _isSharedWorkstation;

    /// <summary>
    /// Indicates whether startup can be retried after workstation detection or auto sign-in fails.
    /// </summary>
    [ObservableProperty]
    private bool _canRetryStartup;

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
        StatusMessage = string.Empty;
        CanRetryStartup = false;
        IsSharedWorkstation = false;

#if WINDOWS
        var windowsUsername = await Task.Run(() =>
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity?.Name ?? string.Empty;
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
            var loginModeTask = _authService.CheckWorkstationAsync(windowsUsername);
            if (loginModeTask is null)
            {
                IsSharedWorkstation = false;
                StatusMessage = "Unable to determine workstation mode.";
                CanRetryStartup = true;
                return;
            }

            var loginMode = await loginModeTask;
            if (loginMode is null)
            {
                IsSharedWorkstation = false;
                StatusMessage = "Unable to determine workstation mode.";
                CanRetryStartup = true;
                return;
            }

            if (!loginMode.IsSuccess || loginMode.Data is null)
            {
                IsSharedWorkstation = false;
                StatusMessage = loginMode.ErrorMessage;
                CanRetryStartup = true;
                return;
            }

            IsSharedWorkstation = loginMode.Data.IsSharedWorkstation;

            if (!loginMode.Data.IsSharedWorkstation)
            {
                await AttemptAutoLoginAsync(windowsUsername);
            }
        }
        finally
        {
            IsCheckingWorkstation = false;
        }
#else
        IsSharedWorkstation = true;
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
        StatusMessage = string.Empty;
        CanRetryStartup = false;

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

        var loginTask = _authService.LoginAsync(Username, Password);
        if (loginTask is null)
        {
            IsAuthenticating = false;
            ErrorMessage = "Login is currently unavailable.";
            return;
        }

        var result = await loginTask;
        if (result is null)
        {
            IsAuthenticating = false;
            ErrorMessage = "Login is currently unavailable.";
            return;
        }

        IsAuthenticating = false;

        if (result.IsSuccess)
        {
            AuthenticatedUsername = result.Data?.Username ?? Username;
            AuthenticatedDisplayName = result.Data?.DisplayName ?? string.Empty;
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

    /// <summary>
    /// Restarts workstation detection and auto sign-in after startup fails.
    /// </summary>
    [RelayCommand]
    private async Task RetryStartupAsync()
    {
        await InitializeAsync();
    }

    /// <summary>
    /// Copies the current error message to the clipboard for support purposes.
    /// </summary>
    [RelayCommand]
    private async Task CopyErrorAsync()
    {
        var errorText = string.Join(Environment.NewLine,
            ErrorMessage,
            StatusMessage);
        if (!string.IsNullOrWhiteSpace(errorText))
        {
            await Clipboard.SetTextAsync(errorText.Trim());
        }
    }

#if WINDOWS
    private async Task AttemptAutoLoginAsync(string windowsUsername)
    {
        IsCheckingWorkstation = true;
        CanRetryStartup = false;

        try
        {
            var autoLoginTask = _authService.AutoLoginAsync(windowsUsername);
            if (autoLoginTask is null)
            {
                IsSharedWorkstation = false;
                StatusMessage = "Auto-login is currently unavailable.";
                CanRetryStartup = true;
                return;
            }

            var autoLogin = await autoLoginTask;
            if (autoLogin is null)
            {
                IsSharedWorkstation = false;
                StatusMessage = "Auto-login is currently unavailable.";
                CanRetryStartup = true;
                return;
            }

            if (autoLogin.IsSuccess)
            {
                AuthenticatedUsername = autoLogin.Data?.Username ?? windowsUsername;
                AuthenticatedDisplayName = autoLogin.Data?.DisplayName ?? string.Empty;
                Password = string.Empty;
                Authenticated?.Invoke(this, EventArgs.Empty);
                return;
            }

            IsSharedWorkstation = false;
            StatusMessage = autoLogin.ErrorMessage;
            CanRetryStartup = true;
        }
        finally
        {
            IsCheckingWorkstation = false;
        }
    }
#endif

    partial void OnIsAuthenticatingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
    }

    partial void OnIsCheckingWorkstationChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
        OnPropertyChanged(nameof(HasErrorMessage));
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasErrorMessage));
    }
}
