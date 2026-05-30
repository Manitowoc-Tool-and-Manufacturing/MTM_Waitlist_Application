using Microsoft.UI.Xaml;
using MTM_Waitlist_Server.Admin.Logging;
using MTM_Waitlist_Server.Admin.Services;
using MTM_Waitlist_Server.Admin.ViewModels;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using MTM_Waitlist_Server.Core.Models.Splash;
using System;

namespace MTM_Waitlist_Server.Admin;

/// <summary>
/// The startup splash window. Shows coordinator progress until startup succeeds or
/// requires user action, then transitions to <see cref="MainWindow"/>.
/// </summary>
public sealed partial class SplashWindow : Window
{
    private readonly Service_WindowSizer _windowSizer;

    /// <summary>The ViewModel driving this window's UI bindings.</summary>
    public ViewModel_Splash ViewModel { get; }

    /// <summary>
    /// Raised on the UI thread when the splash has created a <see cref="MainWindow"/>
    /// so that <c>App</c> can update its <c>_window</c> reference.
    /// </summary>
    public event Action<Window>? MainWindowCreated;

    /// <summary>Initialises the splash window and wires up the ViewModel outcome handler.</summary>
    public SplashWindow(ViewModel_Splash viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        ViewModel.OutcomeReady += OnOutcomeReady;

        Title = "MTM Waitlist Server — Starting\u2026";

        _windowSizer = new Service_WindowSizer(this);
        _windowSizer.ApplySplashSize();
    }

    // ── Outcome routing ───────────────────────────────────────────────────────

    private void OnOutcomeReady(StartupOutcome outcome)
    {
        // Always called on the UI thread (ViewModel dispatches before raising).
        switch (outcome)
        {
            case StartupOutcome.Normal:
                OpenMainWindowAndClose(new MainWindow());
                break;

            case StartupOutcome.Degraded d:
                OpenMainWindowAndClose(new MainWindow(degraded: true, degradedReason: d.Reason));
                break;

            case StartupOutcome.FirstRunWizard w:
                OpenMainWindowAndClose(new MainWindow(
                    firstRunStatus: w.Status,
                    probeResult: w.ProbeResult));
                break;

            case StartupOutcome.AccessDenied a:
                OpenMainWindowAndClose(new MainWindow(accessDenied: true));
                break;

            case StartupOutcome.ApiHostFailed:
                // Non-fatal — app continues in normal mode without the REST API.
                OpenMainWindowAndClose(new MainWindow());
                break;

            case StartupOutcome.Cancelled:
                StartupLogger.Info("Startup cancelled by user — exiting.");
                Application.Current.Exit();
                break;
        }
    }

    private void OpenMainWindowAndClose(MainWindow mainWindow)
    {
        mainWindow.Activate();
        MainWindowCreated?.Invoke(mainWindow);
        Close();
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void ViewLogButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(StartupLogger.LogFilePath)
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // If the file cannot be opened, silently ignore.
        }
    }
}
