using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Models.KillSwitch;
using MTM_Waitlist_Server.Module.KillSwitch.ViewModels;

namespace MTM_Waitlist_Server.Module.KillSwitch.Views;

/// <summary>
/// Code-behind for the Kill Switch module page.
/// All logic is handled by <see cref="ViewModel_KillSwitch"/>.
/// </summary>
public sealed partial class View_KillSwitch : Page
{
    /// <summary>Typed accessor used by x:Bind expressions in the XAML.</summary>
    public ViewModel_KillSwitch ViewModel { get; }

    public View_KillSwitch(ViewModel_KillSwitch viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Loaded += (_, _) => ViewModel.RefreshClientsCommand.Execute(null);
    }

    private void ShutDownClientButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ClientHeartbeat client })
        {
            ViewModel.ShutDownClientCommand.Execute(client);
        }
    }
}
