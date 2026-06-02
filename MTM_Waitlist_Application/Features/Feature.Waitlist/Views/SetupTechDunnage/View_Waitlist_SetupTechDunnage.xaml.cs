using Feature.Waitlist.ViewModels.SetupTechDunnage;
using Feature.Waitlist.Views.SetupTechConfirmation;

namespace Feature.Waitlist.Views.SetupTechDunnage;

/// <summary>
/// Shared code-behind for the Setup Tech dunnage page.
/// </summary>
public partial class View_Waitlist_SetupTechDunnage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ViewModel_Waitlist_SetupTechDunnage _viewModel;
    private bool _hasInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTechDunnage"/> class.
    /// </summary>
    public View_Waitlist_SetupTechDunnage(ViewModel_Waitlist_SetupTechDunnage viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _viewModel = viewModel;
        BindingContext = viewModel;

        Loaded += OnLoaded;
        _viewModel.NavigateBackRequested += OnNavigateBackRequested;
        _viewModel.NavigateToConfirmationRequested += OnNavigateToConfirmationRequested;
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

    private async void OnNavigateBackRequested(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnNavigateToConfirmationRequested(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<View_Waitlist_SetupTechConfirmation>());
    }
}