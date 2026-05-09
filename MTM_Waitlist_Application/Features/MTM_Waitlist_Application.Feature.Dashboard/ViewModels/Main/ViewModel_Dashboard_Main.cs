using CommunityToolkit.Mvvm.ComponentModel;

namespace MTM_Waitlist_Application.Feature.Dashboard.ViewModels.Main;

/// <summary>
/// ViewModel for the Dashboard main screen.
/// Manages summary display state for the primary landing view on both Windows and Android.
/// Populated with real data once the API and service layers (Phases 1–3) are implemented.
/// </summary>
public partial class ViewModel_Dashboard_Main : ObservableObject
{
    /// <summary>
    /// Indicates whether a background data operation is in progress.
    /// Drives activity indicators on both platform layouts.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Human-readable status line shown at the bottom of each layout.
    /// Updated by load commands once the service layer is wired up.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";
}
