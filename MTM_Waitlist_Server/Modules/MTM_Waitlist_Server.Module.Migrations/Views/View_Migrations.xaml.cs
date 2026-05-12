using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Models.Migration;
using MTM_Waitlist_Server.Module.Migrations.ViewModels;

namespace MTM_Waitlist_Server.Module.Migrations.Views;

/// <summary>
/// Code-behind for the Database Migrations module page.
/// All logic is handled by <see cref="ViewModel_Migrations"/>.
/// </summary>
public sealed partial class View_Migrations : Page
{
    /// <summary>Typed accessor used by x:Bind expressions in the XAML.</summary>
    public ViewModel_Migrations ViewModel { get; }

    public View_Migrations(ViewModel_Migrations viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Loaded += (_, _) => ViewModel.LoadCommand.Execute(null);
    }

    private void PreviewButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button { Tag: PendingMigration migration })
        {
            ViewModel.PreviewMigrationCommand.Execute(migration);
        }
    }
}
