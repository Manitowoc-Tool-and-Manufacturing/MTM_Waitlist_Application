using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Models.Dashboard;
using MTM_Waitlist_Server.Module.Dashboard.ViewModels;

namespace MTM_Waitlist_Server.Module.Dashboard.Views;

/// <summary>
/// Code-behind for the MySQL status dashboard page.
/// All logic is handled by <see cref="ViewModel_Dashboard"/>.
/// The kill-button Click handler is used because DataTemplate bindings cannot reach
/// commands on a parent Page without RelativeSource, which is not supported in WinUI 3.
/// </summary>
public sealed partial class View_Dashboard : Page
{
    /// <summary>Typed accessor used by x:Bind expressions in the XAML.</summary>
    public ViewModel_Dashboard ViewModel { get; }

    public View_Dashboard(ViewModel_Dashboard viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private void KillButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Model_ActiveConnection connection })
        {
            ViewModel.KillConnectionCommand.Execute(connection);
        }
    }
}
