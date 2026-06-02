using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using System.Collections.ObjectModel;

namespace Feature.Waitlist.ViewModels.SetupTechConfirmation;

/// <summary>
/// ViewModel for the Setup Tech completion and confirmation step.
/// </summary>
public partial class ViewModel_Waitlist_SetupTechConfirmation : ObservableObject
{
    private readonly IService_SetupTechWorkflowState _workflowState;

    /// <summary>
    /// Raised when the user wants to begin a new setup.
    /// </summary>
    public event EventHandler? StartOverRequested;

    /// <summary>
    /// Raised when the user wants to return to the dashboard.
    /// </summary>
    public event EventHandler? NavigateToDashboardRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModel_Waitlist_SetupTechConfirmation"/> class.
    /// </summary>
    public ViewModel_Waitlist_SetupTechConfirmation(IService_SetupTechWorkflowState workflowState)
    {
        _workflowState = workflowState;
    }

    /// <summary>The saved active job shown in the completion summary.</summary>
    [ObservableProperty]
    private Model_SetupTech_ActiveJob? _activeJob;

    /// <summary>The current work-order subordinate parts.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_SetupTech_SubordinatePart> _subordinateParts = [];

    /// <summary>The current dunnage assignments.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_SetupTech_DunnageAssignment> _dunnageAssignments = [];

    /// <summary>The user-friendly workstation id shown in the completion banner.</summary>
    [ObservableProperty]
    private string _workcenterId = string.Empty;

    /// <summary>The current user's display name shown in the saved timestamp line.</summary>
    [ObservableProperty]
    private string _savedByDisplayName = string.Empty;

    /// <summary>The final saved timestamp shown in the confirmation footer.</summary>
    [ObservableProperty]
    private string _savedTimestamp = string.Empty;

    /// <summary>Indicates whether a previous active job was replaced during save.</summary>
    [ObservableProperty]
    private bool _hadArchivedExistingJob;

    /// <summary>Step-level error message.</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Loads the saved setup state into the confirmation screen.
    /// </summary>
    [RelayCommand]
    private Task InitializeAsync()
    {
        var state = _workflowState.Current;
        ActiveJob = state.PendingActiveJob;
        WorkcenterId = state.SelectedWorkcenter?.WorkcenterId ?? string.Empty;
        SavedByDisplayName = string.IsNullOrWhiteSpace(state.AuthSession?.DisplayName)
            ? state.AuthSession?.Username ?? string.Empty
            : state.AuthSession.DisplayName;
        SavedTimestamp = DateTime.Now.ToString("MM/dd/yyyy h:mm tt");
        HadArchivedExistingJob = state.ExistingActiveJob is not null && !string.IsNullOrWhiteSpace(state.ExistingActiveJob.WorkOrderId);
        SubordinateParts = new ObservableCollection<Model_SetupTech_SubordinatePart>(state.SubordinateParts);
        DunnageAssignments = new ObservableCollection<Model_SetupTech_DunnageAssignment>(state.DunnageAssignments);

        if (ActiveJob is null)
        {
            ErrorMessage = "No saved Setup Tech job is available to display.";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts a new Setup Tech workflow from step one.
    /// </summary>
    [RelayCommand]
    private Task StartOverAsync()
    {
        _workflowState.Reset();
        StartOverRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns to the dashboard after setup is complete.
    /// </summary>
    [RelayCommand]
    private Task GoToDashboardAsync()
    {
        NavigateToDashboardRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}