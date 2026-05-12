using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Module.ConnectedUsers.ViewModels;

namespace MTM_Waitlist_Server.Module.ConnectedUsers.Views;

/// <summary>
/// Code-behind for the Connected Users module page.
/// All logic is delegated to <see cref="ViewModel_ConnectedUsers"/>.
/// </summary>
public sealed partial class View_ConnectedUsers : Page
{
    /// <summary>Typed accessor used by x:Bind expressions in the XAML.</summary>
    public ViewModel_ConnectedUsers ViewModel { get; }

    public View_ConnectedUsers(ViewModel_ConnectedUsers viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Loaded += (_, _) => ViewModel.LoadUsersCommand.Execute(null);
    }
}
