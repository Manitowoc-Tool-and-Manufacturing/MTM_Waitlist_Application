using Feature.Waitlist.ViewModels.SetupTechConfirmation;

namespace Feature.Waitlist.Views.SetupTechConfirmation;

/// <summary>
/// Shared code-behind for the Setup Tech completion page.
/// </summary>
public partial class View_Waitlist_SetupTechConfirmation : ContentPage
{
    private readonly ViewModel_Waitlist_SetupTechConfirmation _viewModel;
    private bool _hasInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTechConfirmation"/> class.
    /// </summary>
    public View_Waitlist_SetupTechConfirmation(ViewModel_Waitlist_SetupTechConfirmation viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        Loaded += OnLoaded;
        _viewModel.StartOverRequested += OnStartOverRequested;
        _viewModel.NavigateToDashboardRequested += OnNavigateToDashboardRequested;
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

    private async void OnStartOverRequested(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SetupTech");
    }

    private async void OnNavigateToDashboardRequested(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Dashboard");
    }
}