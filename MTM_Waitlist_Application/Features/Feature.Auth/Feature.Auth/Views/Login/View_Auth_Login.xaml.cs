using Feature.Auth.ViewModels.Login;
namespace Feature.Auth.Views.Login;

/// <summary>
/// Login page shared code-behind for both Windows and Android layouts.
/// Handles first-load initialisation and shell handoff after successful auth.
/// </summary>
public partial class View_Auth_Login : ContentPage
{
    private readonly ViewModel_Auth_Login _viewModel;
    private bool _hasInitialized;

    /// <summary>
    /// Raised when the login workflow completes successfully.
    /// </summary>
    public event EventHandler? Authenticated;

    /// <summary>
    /// Initialises the page with its injected ViewModel.
    /// </summary>
    public View_Auth_Login(ViewModel_Auth_Login viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        Loaded += OnLoaded;
        _viewModel.Authenticated += OnAuthenticated;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private void OnAuthenticated(object? sender, EventArgs e) => Authenticated?.Invoke(this, EventArgs.Empty);
}