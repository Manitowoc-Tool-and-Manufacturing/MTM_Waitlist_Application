using Feature.Waitlist.ViewModels.SetupTechDunnage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Application.WinUI;
using MTM_Waitlist_Application.WinUI.Views.SetupTechConfirmation;
using Core.Models.SetupTech;

namespace MTM_Waitlist_Application.WinUI.Views.SetupTechDunnage;

/// <summary>
/// WinUI 3 Setup Tech dunnage assignment page.
/// </summary>
public sealed partial class View_Waitlist_SetupTechDunnage : Page
{
    private readonly ViewModel_Waitlist_SetupTechDunnage _viewModel;

    /// <summary>
    /// The ViewModel bound to this page via compiled x:Bind.
    /// </summary>
    public ViewModel_Waitlist_SetupTechDunnage ViewModel => _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTechDunnage"/> class.
    /// </summary>
    public View_Waitlist_SetupTechDunnage()
    {
        _viewModel = App.Services.GetRequiredService<ViewModel_Waitlist_SetupTechDunnage>();
        InitializeComponent();
        Loaded += OnLoaded;
        _viewModel.NavigateBackRequested += OnNavigateBackRequested;
        _viewModel.NavigateToConfirmationRequested += OnNavigateToConfirmationRequested;
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

    private void OnNavigateToConfirmationRequested(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(View_Waitlist_SetupTechConfirmation));
    }

    private void RemoveDunnage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Microsoft.UI.Xaml.Controls.Button button || button.DataContext is not Model_SetupTech_DunnageAssignment assignment)
        {
            return;
        }

        if (_viewModel.RemoveDunnageItemCommand.CanExecute(assignment))
        {
            _viewModel.RemoveDunnageItemCommand.Execute(assignment);
        }
    }
}