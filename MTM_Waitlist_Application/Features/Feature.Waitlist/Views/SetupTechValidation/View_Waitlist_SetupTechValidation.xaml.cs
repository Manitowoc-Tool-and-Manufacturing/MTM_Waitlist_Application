using Feature.Waitlist.ViewModels.SetupTechValidation;
using Feature.Waitlist.Views.SetupTechDunnage;

namespace Feature.Waitlist.Views.SetupTechValidation;

/// <summary>
/// Shared code-behind for the Setup Tech validation page.
/// </summary>
public partial class View_Waitlist_SetupTechValidation : ContentPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ViewModel_Waitlist_SetupTechValidation _viewModel;
    private bool _hasInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTechValidation"/> class.
    /// </summary>
    public View_Waitlist_SetupTechValidation(ViewModel_Waitlist_SetupTechValidation viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _viewModel = viewModel;
        BindingContext = viewModel;

        Loaded += OnLoaded;
        _viewModel.NavigateBackRequested += OnNavigateBackRequested;
        _viewModel.NavigateToDunnageRequested += OnNavigateToDunnageRequested;
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

    private async void OnNavigateToDunnageRequested(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(_serviceProvider.GetRequiredService<View_Waitlist_SetupTechDunnage>());
    }
}