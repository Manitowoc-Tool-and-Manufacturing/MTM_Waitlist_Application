using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Models.Backup;
using MTM_Waitlist_Server.Module.Backup.ViewModels;

namespace MTM_Waitlist_Server.Module.Backup.Views;

/// <summary>
/// Code-behind for the Backup &amp; Restore module page.
/// All logic is handled by <see cref="ViewModel_Backup"/>.
/// Button click handlers are used because DataTemplate bindings cannot reach
/// commands on the parent Page without RelativeSource in WinUI 3.
/// </summary>
public sealed partial class View_Backup : Page
{
    /// <summary>Typed accessor used by x:Bind expressions in the XAML.</summary>
    public ViewModel_Backup ViewModel { get; }

    public View_Backup(ViewModel_Backup viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Loaded += (_, _) => ViewModel.RefreshHistoryCommand.Execute(null);
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BackupFileInfo file })
        {
            ViewModel.RestoreCommand.Execute(file);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BackupFileInfo file })
        {
            ViewModel.DeleteBackupCommand.Execute(file);
        }
    }
}
