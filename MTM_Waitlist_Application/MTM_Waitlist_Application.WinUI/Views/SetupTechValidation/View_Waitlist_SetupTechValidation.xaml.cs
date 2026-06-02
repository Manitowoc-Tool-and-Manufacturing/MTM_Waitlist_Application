using Feature.Waitlist.ViewModels.SetupTechValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Application.WinUI;
using MTM_Waitlist_Application.WinUI.Views.SetupTechDunnage;

namespace MTM_Waitlist_Application.WinUI.Views.SetupTechValidation;

/// <summary>
/// WinUI 3 Setup Tech validation page.
/// </summary>
public sealed partial class View_Waitlist_SetupTechValidation : Page
{
    private readonly ViewModel_Waitlist_SetupTechValidation _viewModel;

    /// <summary>
    /// The ViewModel bound to this page via compiled x:Bind.
    /// </summary>
    public ViewModel_Waitlist_SetupTechValidation ViewModel => _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTechValidation"/> class.
    /// </summary>
    public View_Waitlist_SetupTechValidation()
    {
        _viewModel = App.Services.GetRequiredService<ViewModel_Waitlist_SetupTechValidation>();
        InitializeComponent();
        Loaded += OnLoaded;
        _viewModel.NavigateBackRequested += OnNavigateBackRequested;
        _viewModel.NavigateToDunnageRequested += OnNavigateToDunnageRequested;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private void OnNavigateBackRequested(object? sender, EventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void OnNavigateToDunnageRequested(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(View_Waitlist_SetupTechDunnage));
    }
}