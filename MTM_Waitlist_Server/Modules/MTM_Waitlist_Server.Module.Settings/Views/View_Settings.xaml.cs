using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Module.Settings.ViewModels;

namespace MTM_Waitlist_Server.Module.Settings.Views;

/// <summary>
/// Code-behind for the Settings page.
/// All logic is handled by <see cref="ViewModel_Settings"/>.
/// </summary>
public sealed partial class View_Settings : Page
{
    /// <summary>Typed accessor used by x:Bind expressions in the XAML.</summary>
    public ViewModel_Settings ViewModel { get; }

    public View_Settings(ViewModel_Settings viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }
}
