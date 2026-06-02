using Feature.Waitlist.ViewModels.SetupTech;
using Feature.Waitlist.Views.SetupTechValidation;

namespace Feature.Waitlist.Views.SetupTech;

/// <summary>
/// Shared code-behind for the Setup Tech workstation and work-order page.
/// </summary>
public partial class View_Waitlist_SetupTech : ContentPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ViewModel_Waitlist_SetupTech _viewModel;
    private bool _hasInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTech"/> class.
    /// </summary>
    public View_Waitlist_SetupTech(ViewModel_Waitlist_SetupTech viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _viewModel = viewModel;
        BindingContext = viewModel;

        Loaded += OnLoaded;
        Appearing += OnAppearing;
        _viewModel.NavigateToValidationRequested += OnNavigateToValidationRequested;
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

    private async void OnAppearing(object? sender, EventArgs e)
    {
        if (!_hasInitialized)
        {
            return;
        }

        await _viewModel.RestoreStateCommand.ExecuteAsync(null);
    }

    private async void OnNavigateToValidationRequested(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<View_Waitlist_SetupTechValidation>());
    }
}