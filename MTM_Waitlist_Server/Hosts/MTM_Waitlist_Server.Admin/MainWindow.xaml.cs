using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Admin.Views;
using MTM_Waitlist_Server.Core.Interfaces.Window;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Module.Dashboard.Views;
using MTM_Waitlist_Server.Module.Settings.Views;
using System;
using System.Linq;
namespace MTM_Waitlist_Server.Admin;

/// <summary>
/// Navigation shell window. Supports four launch modes:
/// normal, access-denied, first-run wizard, and degraded (DB unreachable post-setup).
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly bool _accessDenied;
    private readonly bool _degraded;
    private readonly Model_FirstRunProbeResult? _probeResult;
    private readonly IService_WindowSizer _windowSizer;

    /// <summary>
    /// Normal / access-denied constructor — backward-compatible with existing callers.
    /// </summary>
    public MainWindow(bool accessDenied = false, bool degraded = false,
        FirstRunStatus? firstRunStatus = null,
        Model_FirstRunProbeResult? probeResult = null)
    {
        InitializeComponent();
        _accessDenied = accessDenied;
        _degraded     = degraded;
        _probeResult  = probeResult;
        Title = "MTM Waitlist Server Admin";

        // Construct the window sizer now that we have a valid HWND.
        _windowSizer = new Service_WindowSizer(this);

        // Always open centered on the monitor the window lands on.
        // ApplyFirstRunSize / ApplyNormalSize each call CenterOnMonitor after resizing,
        // so the plain CenterOnMonitor() here only runs for the access-denied path.
        if (accessDenied)
        {
            _windowSizer.CenterOnMonitor();
            ShowAccessDenied();
        }
        else if (firstRunStatus.HasValue)
        {
            ShowFirstRunWizard(firstRunStatus.Value);
        }
        else if (degraded)
        {
            _windowSizer.ApplyNormalSize();
            ShowDegradedMode();
        }
        else
        {
            _windowSizer.ApplyNormalSize();
            // Navigate to Dashboard on startup.
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void ShowAccessDenied()
    {
        ContentFrame.Content = new TextBlock
        {
            Text = "Access denied. You must be a member of the required Windows administrator group to use this application.",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            Margin = new Thickness(24),
            FontSize = 16
        };
        LockNavPane();
    }

    /// <summary>
    /// Locks the NavigationView and loads the first-run wizard so the user can
    /// bootstrap the database before the normal admin shell is usable.
    /// </summary>
    private void ShowFirstRunWizard(FirstRunStatus status)
    {
        _windowSizer.ApplyFirstRunSize();
        LockNavPane();

        // Database does not exist yet — override the hardcoded green status bar values
        // so the user doesn't see a false "Connected" indicator during first-run setup.
        DbStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        TxtDbStatus.Text = "Database not configured";

        var wizard = App.Services?.GetService(typeof(View_FirstRun)) as View_FirstRun
            ?? throw new InvalidOperationException("View_FirstRun is not registered in DI.");

        // Pass the probe result into the ViewModel so the error message is visible immediately.
        if (_probeResult is not null)
        {
            wizard.ViewModel.ApplyProbeResult(_probeResult);
        }

        // When the wizard signals completion, unlock the shell and go to Dashboard.
        wizard.SetupCompleted += OnFirstRunCompleted;
        ContentFrame.Content = wizard;
    }

    private void OnFirstRunCompleted(object? sender, EventArgs e)
    {
        // Restart the app so the normal launch path (DB role check) runs cleanly.
        Microsoft.Windows.AppLifecycle.AppInstance.Restart(string.Empty);
    }

    /// <summary>
    /// Disables all nav-pane items and hides the pane-toggle button so the user
    /// cannot navigate away from the current content (first-run wizard or access-denied).
    /// The content frame itself is NOT disabled — controls inside remain interactive.
    /// </summary>
    private void LockNavPane()
    {
        // Disable every item in the main menu and the footer (Settings lives there).
        foreach (var item in NavView.MenuItems.Cast<object>().Concat(NavView.FooterMenuItems.Cast<object>()))
        {
            if (item is NavigationViewItem nvi)
            {
                nvi.IsEnabled = false;
            }
        }

        // Collapse the hamburger/pane-toggle so the pane cannot be opened.
        NavView.IsPaneToggleButtonVisible = false;
        NavView.IsPaneOpen = false;
    }

    /// <summary>
    /// Shows a notice that MySQL is currently unreachable; the rest of the shell
    /// is still accessible so the administrator can check settings and retry.
    /// </summary>
    private void ShowDegradedMode()
    {
        // Show a banner but still allow navigation (settings are still usable).
        var banner = new InfoBar
        {
            Title = "Database Unreachable",
            Message = "MySQL could not be reached. Some features are unavailable. Check Settings → Database.",
            Severity = InfoBarSeverity.Warning,
            IsOpen = true,
            IsClosable = true,
            Margin = new Thickness(0, 0, 0, 8)
        };

        // Stack the banner above the main content frame using a StackPanel.
        var panel = new StackPanel();
        panel.Children.Add(banner);

        var dashView = App.Services?.GetService(typeof(View_Dashboard)) as View_Dashboard;
        if (dashView is not null)
        {
            panel.Children.Add(dashView);
        }

        ContentFrame.Content = panel;
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        switch (item.Tag?.ToString())
        {
            case "Dashboard":
                ContentFrame.Content = App.Services?.GetService(typeof(View_Dashboard)) as View_Dashboard
                    ?? throw new InvalidOperationException("View_Dashboard is not registered in DI.");
                break;

            case "Settings":
                ContentFrame.Content = App.Services?.GetService(typeof(View_Settings)) as View_Settings
                    ?? throw new InvalidOperationException("View_Settings is not registered in DI.");
                break;

            case "Migrations":
            case "Backup":
            case "KillSwitch":
                // Placeholder until each module view is implemented.
                ContentFrame.Content = new TextBlock
                {
                    Text = $"{item.Content} — coming soon",
                    FontSize = 20,
                    Margin = new Thickness(24),
                    VerticalAlignment = VerticalAlignment.Top
                };
                break;
        }
    }

    private void BtnStartApi_Click(object sender, RoutedEventArgs e)
    {
        // TODO: call API host start via a shared IService_ApiHost.
    }

    private void BtnStopApi_Click(object sender, RoutedEventArgs e)
    {
        // TODO: call API host stop via a shared IService_ApiHost.
    }

    private void BtnRestartApi_Click(object sender, RoutedEventArgs e)
    {
        // TODO: call API host restart via a shared IService_ApiHost.
    }
}
