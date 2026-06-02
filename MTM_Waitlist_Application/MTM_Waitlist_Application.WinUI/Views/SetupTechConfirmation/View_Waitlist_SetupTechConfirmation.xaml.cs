using Feature.Waitlist.ViewModels.SetupTechConfirmation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Application.WinUI;
using MTM_Waitlist_Application.WinUI.Views.SetupTech;

namespace MTM_Waitlist_Application.WinUI.Views.SetupTechConfirmation;

/// <summary>
/// WinUI 3 Setup Tech confirmation page.
/// </summary>
public sealed partial class View_Waitlist_SetupTechConfirmation : Page
{
    private readonly ViewModel_Waitlist_SetupTechConfirmation _viewModel;

    /// <summary>
    /// The ViewModel bound to this page via compiled x:Bind.
    /// </summary>
    public ViewModel_Waitlist_SetupTechConfirmation ViewModel => _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTechConfirmation"/> class.
    /// </summary>
    public View_Waitlist_SetupTechConfirmation()
    {
        _viewModel = App.Services.GetRequiredService<ViewModel_Waitlist_SetupTechConfirmation>();
        InitializeComponent();
        Loaded += OnLoaded;
        _viewModel.StartOverRequested += OnStartOverRequested;
        _viewModel.NavigateToDashboardRequested += OnNavigateToDashboardRequested;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private void OnStartOverRequested(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(View_Waitlist_SetupTech));
    }

    private void OnNavigateToDashboardRequested(object? sender, EventArgs e)
    {
        MainWindow.NavigateToDashboard();
    }
}