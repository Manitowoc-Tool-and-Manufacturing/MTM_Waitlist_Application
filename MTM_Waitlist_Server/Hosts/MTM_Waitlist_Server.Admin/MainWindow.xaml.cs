using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Module.Dashboard.Views;
using MTM_Waitlist_Server.Module.Settings.Views;
using System;

namespace MTM_Waitlist_Server.Admin;

/// <summary>
/// Navigation shell window. When <paramref name="accessDenied"/> is true the window shows
/// an access-denied message and exits on close; otherwise it hosts the admin NavigationView.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly bool _accessDenied;

    public MainWindow(bool accessDenied = false)
    {
        InitializeComponent();
        _accessDenied = accessDenied;
        Title = "MTM Waitlist Server Admin";

        // 1. Get the window handle (HWND)
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // 2. Get the WindowId from the HWND
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

        // 3. Get the AppWindow object
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        // 4. Resize the window (dimensions are in pixels)
        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1200, Height = 600 });


        if (accessDenied)
        {
            ShowAccessDenied();
        }
        else
        {
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
        NavView.IsEnabled = false;
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
