namespace MTM_Waitlist_Server.Core.Models.Splash;

/// <summary>
/// Progress token emitted by <c>Service_StartupCoordinator</c> for each startup step.
/// Reported via <see cref="System.IProgress{T}"/> and consumed by <c>ViewModel_Splash</c>
/// to update the splash screen step list.
/// </summary>
public sealed record StartupStep(
    StartupStepId StepId,
    StartupStepState State,
    string FriendlyLabel,
    string? Detail = null);
