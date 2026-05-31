using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Core.Interfaces.Api;
using MTM_Waitlist_Server.Admin.Views;
using MTM_Waitlist_Server.Core.Interfaces.Window;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Module.Backup.Views;
using MTM_Waitlist_Server.Module.ConnectedUsers.Views;
using MTM_Waitlist_Server.Module.Dashboard.Views;
using MTM_Waitlist_Server.Module.KillSwitch.Views;
using MTM_Waitlist_Server.Module.Migrations.Views;
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
    private readonly string? _degradedReason;
    private readonly Model_FirstRunProbeResult? _probeResult;
    private readonly IService_ApiHost _apiHost;
    private readonly IService_WindowSizer _windowSizer;

    /// <summary>
    /// Normal / access-denied constructor — backward-compatible with existing callers.
    /// </summary>
    public MainWindow(bool accessDenied = false, bool degraded = false,
        string? degradedReason = null,
        FirstRunStatus? firstRunStatus = null,
        Model_FirstRunProbeResult? probeResult = null)
    {
        InitializeComponent();
        _accessDenied = accessDenied;
        _degraded = degraded;
        _degradedReason = degradedReason;
        _probeResult = probeResult;
        Title = "MTM Waitlist Server Admin";

        _apiHost = App.Services?.GetService(typeof(IService_ApiHost)) as IService_ApiHost
            ?? throw new InvalidOperationException("IService_ApiHost is not registered in DI.");

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

#if DEBUG
        // Attach the debug toolbar to the main grid so it is always visible in Debug builds.
        // The NavView occupies Row 0 and the status bar Row 1, so we insert an extra row.
        if (Content is Grid mainGrid)
        {
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var debugStrip = BuildDebugModeStrip();
            Grid.SetRow(debugStrip, mainGrid.RowDefinitions.Count - 1);
            mainGrid.Children.Add(debugStrip);
        }
#endif
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
    /// Shows a styled full-page degraded-mode screen that states the specific reason MySQL
    /// could not be used.  Navigation is still enabled so the user can access Settings.
    /// </summary>
    private void ShowDegradedMode()
    {
        // ── Status bar ───────────────────────────────────────────────────────
        DbStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xB9, 0x00)); // amber
        TxtDbStatus.Text = "Database unreachable";

        // ── Root page ────────────────────────────────────────────────────────
        var root = new Grid
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0x0F, 0xFF, 0xB9, 0x00)), // subtle amber tint
        };

        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // ── Centre card ──────────────────────────────────────────────────────
        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(48, 40, 48, 40),
            MaxWidth = 600,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0x40, 0xFF, 0xB9, 0x00)),
            BorderThickness = new Thickness(1),
        };
        Grid.SetRow(card, 0);

        var cardStack = new StackPanel { Spacing = 16 };

        // Warning icon
        var iconText = new TextBlock
        {
            Text = "\uE7BA", // Warning glyph
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"),
            FontSize = 48,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xB9, 0x00)),
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        // Title
        var title = new TextBlock
        {
            Text = "Database Unavailable",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        // Divider
        var divider = new Border
        {
            Height = 1,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
            Margin = new Thickness(0, 4, 0, 4),
        };

        // Reason text — specific per callsite
        var reasonText = new TextBlock
        {
            Text = _degradedReason ?? "MySQL could not be reached with the current settings.",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            FontSize = 14,
            Opacity = 0.9,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
        };

        // Action hint
        var hint = new TextBlock
        {
            Text = "You can update the connection settings below. Other modules remain accessible but database features will not work until the connection is restored.",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            FontSize = 12,
            Opacity = 0.6,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
        };

        // Open Settings button
        var btnSettings = new Button
        {
            Content = "Open Settings",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
        };
        btnSettings.Click += (_, _) =>
        {
            NavView.SelectedItem = NavView.FooterMenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == "Settings");
        };

        cardStack.Children.Add(iconText);
        cardStack.Children.Add(title);
        cardStack.Children.Add(divider);
        cardStack.Children.Add(reasonText);
        cardStack.Children.Add(hint);
        cardStack.Children.Add(btnSettings);

        card.Child = cardStack;
        root.Children.Add(card);

        ContentFrame.Content = root;
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
                ContentFrame.Content = App.Services?.GetService(typeof(View_Migrations)) as View_Migrations
                    ?? throw new InvalidOperationException("View_Migrations is not registered in DI.");
                break;

            case "Backup":
                ContentFrame.Content = App.Services?.GetService(typeof(View_Backup)) as View_Backup
                    ?? throw new InvalidOperationException("View_Backup is not registered in DI.");
                break;

            case "KillSwitch":
                ContentFrame.Content = App.Services?.GetService(typeof(View_KillSwitch)) as View_KillSwitch
                    ?? throw new InvalidOperationException("View_KillSwitch is not registered in DI.");
                break;

            case "ConnectedUsers":
                ContentFrame.Content = App.Services?.GetService(typeof(View_ConnectedUsers)) as View_ConnectedUsers
                    ?? throw new InvalidOperationException("View_ConnectedUsers is not registered in DI.");
                break;
        }
    }

    private async void BtnStartApi_Click(object sender, RoutedEventArgs e)
    {
        await _apiHost.EnsureRunningAsync();
    }

    private async void BtnStopApi_Click(object sender, RoutedEventArgs e)
    {
        await _apiHost.StopAsync();
    }

    private async void BtnRestartApi_Click(object sender, RoutedEventArgs e)
    {
        await _apiHost.StopAsync();
        await _apiHost.EnsureRunningAsync();
    }

#if DEBUG
    // ─────────────────────────────────────────────────────────────────────────
    // DEBUG-ONLY: mode-testing infrastructure
    // Visible only in Debug builds.  Never compiled into Release.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the persistent debug toolbar that lets developers force any launch mode
    /// without restarting the app or editing the database.
    /// </summary>
    private Border BuildDebugModeStrip()
    {
        static Button MakeBtn(string label, Windows.UI.Color accent, RoutedEventHandler handler)
        {
            var btn = new Button
            {
                Content = label,
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(4, 0, 0, 0),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(0x28, accent.R, accent.G, accent.B)),
            };
            btn.Click += handler;
            return btn;
        }

        var strip = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0xCC, 0x1A, 0x1A, 0x2E)),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0x60, 0x80, 0x00, 0xFF)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(12, 6, 12, 6),
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };

        panel.Children.Add(new TextBlock
        {
            Text = "🛠 DEBUG MODES:",
            FontSize = 11,
            Opacity = 0.7,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        });

        panel.Children.Add(MakeBtn("Normal Launch",
            Windows.UI.Color.FromArgb(0xFF, 0x00, 0xC8, 0x53),
            (_, _) => SimulateDegradedMode(null)));

        panel.Children.Add(MakeBtn("DB Unreachable",
            Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xB9, 0x00),
            (_, _) => SimulateDegradedMode(
                "MySQL could not be reached at localhost:3306.\n\nDetail: [DEBUG] Simulated network failure.")));

        panel.Children.Add(MakeBtn("Schema Dropped",
            Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x6D, 0x00),
            (_, _) => SimulateDegradedMode(
                "The database schema for 'mtm_waitlist' was not found. [DEBUG] Simulated schema drop.")));

        panel.Children.Add(MakeBtn("No Admin User",
            Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x17, 0x44),
            (_, _) => SimulateDegradedMode(
                "No active Admin or Developer user was found. [DEBUG] Simulated account deletion.")));

        panel.Children.Add(MakeBtn("Access Denied",
            Windows.UI.Color.FromArgb(0xFF, 0xAA, 0x00, 0xFF),
            (_, _) => SimulateAccessDenied()));

        strip.Child = panel;
        return strip;
    }

    /// <summary>Re-renders the content frame in degraded mode with the given reason (or normal if null).</summary>
    private void SimulateDegradedMode(string? reason)
    {
        if (reason is null)
        {
            // Return to normal — restore status bar and load Dashboard directly.
            DbStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            TxtDbStatus.Text = "Connected — mtm_waitlist @ localhost:3306";

            // Load Dashboard directly into ContentFrame; also sync the nav selection.
            ContentFrame.Content = App.Services?.GetService(typeof(View_Dashboard)) as View_Dashboard
                ?? throw new InvalidOperationException("View_Dashboard is not registered in DI.");
            NavView.SelectedItem = NavView.MenuItems[0];
            return;
        }

        // Render degraded card directly into ContentFrame (no inner debug strip).
        var root = new Grid
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0x0F, 0xFF, 0xB9, 0x00)),
        };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(48, 40, 48, 40),
            MaxWidth = 600,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0x40, 0xFF, 0xB9, 0x00)),
            BorderThickness = new Thickness(1),
        };
        Grid.SetRow(card, 0);

        var stack = new StackPanel { Spacing = 16 };
        stack.Children.Add(new TextBlock
        {
            Text = "\uE7BA",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"),
            FontSize = 48,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xB9, 0x00)),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Database Unavailable",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new Border
        {
            Height = 1,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
            Margin = new Thickness(0, 4, 0, 4),
        });
        stack.Children.Add(new TextBlock
        {
            Text = reason,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            FontSize = 14,
            Opacity = 0.9,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
        });

        card.Child = stack;
        root.Children.Add(card);

        DbStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xB9, 0x00));
        TxtDbStatus.Text = "Database unreachable [DEBUG]";
        ContentFrame.Content = root;
    }

    /// <summary>Re-renders the content frame as the access-denied screen.</summary>
    private void SimulateAccessDenied()
    {
        DbStatusDot.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        TxtDbStatus.Text = "Access denied [DEBUG]";

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var card = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(48, 40, 48, 40),
            MaxWidth = 500,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x40, 0xAA, 0x00, 0xFF)),
            BorderThickness = new Thickness(1),
        };
        Grid.SetRow(card, 0);

        var stack = new StackPanel { Spacing = 16 };
        stack.Children.Add(new TextBlock
        {
            Text = "\uE72E",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"),
            FontSize = 48,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xAA, 0x00, 0xFF)),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Access Denied",
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "You must be a member of the required Windows administrator group to use this application. [DEBUG SIMULATION]",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            FontSize = 14,
            Opacity = 0.9,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
        });

        card.Child = stack;
        root.Children.Add(card);

        ContentFrame.Content = root;
    }

#endif
}
