using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Models.Migration;
using MTM_Waitlist_Server.Module.Migrations.ViewModels;
using System;
using System.Threading.Tasks;

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

    private async void WipeDatabaseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var confirmationTextBox = new TextBox
        {
            PlaceholderText = "Type WIPE DATABASE to confirm"
        };

        var content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "This will permanently drop and recreate the configured MySQL database. All data, migrations, procedures, triggers, and indexes will be deleted.",
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = "Type WIPE DATABASE to continue.",
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                },
                confirmationTextBox
            }
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Wipe Entire Database",
            PrimaryButtonText = "Wipe Database",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            Content = content
        };

        ContentDialogResult result;
        do
        {
            result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            if (!string.Equals(confirmationTextBox.Text?.Trim(), "WIPE DATABASE", StringComparison.Ordinal))
            {
                dialog.Title = "Confirmation Text Did Not Match";
            }
        }
        while (!string.Equals(confirmationTextBox.Text?.Trim(), "WIPE DATABASE", StringComparison.Ordinal));

        await ViewModel.WipeDatabaseCommand.ExecuteAsync(null);
    }
}
