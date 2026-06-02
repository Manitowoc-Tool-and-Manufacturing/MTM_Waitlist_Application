using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MTM_Waitlist_Application.WinUI.Views.SetupTech;

/// <summary>
/// Code-behind for the WinUI 3 Setup Tech shell page.
/// The page hosts the Setup Tech workflow inside an inner Frame and routes to step one on load.
/// </summary>
public sealed partial class SetupTechPage : Page
{
    private bool _hasNavigatedToFirstStep;

    /// <summary>
    /// Initializes the Setup Tech shell page.
    /// </summary>
    public SetupTechPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasNavigatedToFirstStep)
        {
            return;
        }

        _hasNavigatedToFirstStep = true;
        SetupTechFrame.Navigate(typeof(View_Waitlist_SetupTech));
    }
}