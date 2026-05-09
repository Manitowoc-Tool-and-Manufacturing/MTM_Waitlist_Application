using MTM_Waitlist_Application.Feature.Dashboard.ViewModels.Main;

namespace MTM_Waitlist_Application.Feature.Dashboard.Views.Main;

/// <summary>
/// Code-behind for the Dashboard main screen.
/// Shared across Windows and Android — layout is defined in the platform-specific
/// XAML files: View_Dashboard_Main.Windows.xaml and View_Dashboard_Main.Android.xaml.
/// </summary>
public partial class View_Dashboard_Main : ContentPage
{
    /// <summary>
    /// Initializes the Dashboard main page with its ViewModel via dependency injection.
    /// </summary>
    /// <param name="viewModel">The ViewModel provided by the DI container.</param>
    public View_Dashboard_Main(ViewModel_Dashboard_Main viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}