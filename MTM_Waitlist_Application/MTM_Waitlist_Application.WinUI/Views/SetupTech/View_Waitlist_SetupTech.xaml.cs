using Feature.Waitlist.ViewModels.SetupTech;
using Feature.Waitlist.Views.SetupTechValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Application.WinUI;

namespace MTM_Waitlist_Application.WinUI.Views.SetupTech;

/// <summary>
/// WinUI 3 Setup Tech workstation and work-order page.
/// </summary>
public sealed partial class View_Waitlist_SetupTech : Page
{
    private readonly ViewModel_Waitlist_SetupTech _viewModel;
    private bool _hasInitialized;

    /// <summary>
    /// The ViewModel bound to this page via compiled x:Bind.
    /// </summary>
    public ViewModel_Waitlist_SetupTech ViewModel => _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="View_Waitlist_SetupTech"/> class.
    /// </summary>
    public View_Waitlist_SetupTech()
    {
        _viewModel = App.Services.GetRequiredService<ViewModel_Waitlist_SetupTech>();
        InitializeComponent();
        Loaded += OnLoaded;
        _viewModel.NavigateToValidationRequested += OnNavigateToValidationRequested;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private void OnNavigateToValidationRequested(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(View_Waitlist_SetupTechValidation));
    }
}