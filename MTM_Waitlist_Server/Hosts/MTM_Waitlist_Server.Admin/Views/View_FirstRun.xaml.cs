using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Admin.ViewModels;
using MTM_Waitlist_Server.Core.Models.FirstRun;
using System;

namespace MTM_Waitlist_Server.Admin.Views;

/// <summary>
/// Code-behind for the first-run setup wizard page.
/// Responds to step changes on the ViewModel to show/hide the correct step panel
/// and raises <see cref="SetupCompleted"/> when the wizard finishes.
/// </summary>
public sealed partial class View_FirstRun : Page
{
    /// <summary>
    /// Raised when the first-run wizard completes successfully so the
    /// shell can restart and enter the normal launch path.
    /// </summary>
    public event EventHandler? SetupCompleted;

    /// <summary>Gets the wizard ViewModel.</summary>
    public ViewModel_FirstRun ViewModel { get; }

    /// <summary>
    /// Initialises the view with its ViewModel and wires up step-change notification.
    /// </summary>
    public View_FirstRun(ViewModel_FirstRun viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ViewModel_FirstRun.CurrentStep))
            {
                ApplyStep(ViewModel.CurrentStep);
            }
        };

        // When the wizard signals completion, notify the shell.
        ViewModel.WizardCompleted += (_, _) => SetupCompleted?.Invoke(this, EventArgs.Empty);

        // Apply initial probe-driven skip logic.
        ApplyProbeStatus(viewModel.ProbeStatus);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private void ApplyProbeStatus(FirstRunStatus status)
    {
        // Jump directly to the relevant step based on what the probe found.
        int startStep = status switch
        {
            FirstRunStatus.NoAdminUser => 3,
            FirstRunStatus.SchemaMissing => 2,
            _ => 1
        };
        ApplyStep(startStep);
    }

    private void ApplyStep(int step)
    {
        PanelStep1.Visibility = step == 1
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        PanelStep2.Visibility = step == 2
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        PanelStep3.Visibility = step == 3
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    /// <summary>
    /// Triggers a check for whether the application MySQL user already exists as soon as
    /// the admin password field loses focus — so the UI can grey out the creation fields
    /// before the user clicks the setup button.
    /// </summary>
    private void AdminPassword_LostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _ = ViewModel.CheckAppUserCommand.ExecuteAsync(null);
    }
}

