using Feature.Dashboard.ViewModels.Main;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist_Application.WinUI.Views.Dashboard;

/// <summary>
/// Code-behind for the WinUI 3 dashboard page.
/// Resolves <see cref="ViewModel_Dashboard_Main"/> from DI and exposes it
/// for compiled x:Bind in <c>DashboardPage.xaml</c>.
/// </summary>
public sealed partial class DashboardPage : Page
{
    /// <summary>
    /// The ViewModel bound to this page via compiled x:Bind.
    /// </summary>
    public ViewModel_Dashboard_Main ViewModel { get; }

    /// <summary>
    /// Initializes the dashboard page and resolves its ViewModel from DI.
    /// </summary>
    public DashboardPage()
    {
        ViewModel = App.Services.GetRequiredService<ViewModel_Dashboard_Main>();
        InitializeComponent();
    }
}
