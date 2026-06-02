using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using System.Collections.ObjectModel;

namespace Feature.Waitlist.ViewModels.SetupTechDunnage;

/// <summary>
/// ViewModel for the Setup Tech dunnage assignment step.
/// </summary>
public partial class ViewModel_Waitlist_SetupTechDunnage : ObservableObject
{
    private readonly IService_SetupTech _setupTechService;
    private readonly IService_SetupTechWorkflowState _workflowState;
    private List<Model_DunnagePart> _allCatalogItems = [];

    /// <summary>
    /// Raised when the user returns to validation.
    /// </summary>
    public event EventHandler? NavigateBackRequested;

    /// <summary>
    /// Raised when the setup has been saved and the confirmation screen should be shown.
    /// </summary>
    public event EventHandler? NavigateToConfirmationRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModel_Waitlist_SetupTechDunnage"/> class.
    /// </summary>
    public ViewModel_Waitlist_SetupTechDunnage(
        IService_SetupTech setupTechService,
        IService_SetupTechWorkflowState workflowState)
    {
        _setupTechService = setupTechService;
        _workflowState = workflowState;
    }

    /// <summary>The current work-order identifier.</summary>
    [ObservableProperty]
    private string _workOrderId = string.Empty;

    /// <summary>The current sequence number.</summary>
    [ObservableProperty]
    private int _sequenceNo;

    /// <summary>The list of currently assigned dunnage items.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_SetupTech_DunnageAssignment> _currentAssignments = [];

    /// <summary>The full list of enabled dunnage types used as category filters.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_SetupTech_DunnageTypeConfig> _enabledDunnageTypes = [];

    /// <summary>The currently selected dunnage type filter.</summary>
    [ObservableProperty]
    private Model_SetupTech_DunnageTypeConfig? _selectedTypeFilter;

    /// <summary>The currently visible dunnage catalog items for the selected filter.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_DunnagePart> _dunnageCatalog = [];

    /// <summary>The currently selected dunnage part to add.</summary>
    [ObservableProperty]
    private Model_DunnagePart? _selectedDunnagePart;

    /// <summary>Step-level status message.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Step-level error message.</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>Indicates whether dunnage data is loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Indicates whether add/save operations are in progress.</summary>
    [ObservableProperty]
    private bool _isSaving;

    /// <summary>
    /// Loads the existing assignment, enabled types, and catalog items for the current job.
    /// </summary>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        var state = _workflowState.Current;
        WorkOrderId = state.WorkOrderHeader?.WorkOrderId ?? string.Empty;
        SequenceNo = state.SelectedSequence?.SequenceNo ?? 0;
        CurrentAssignments = new ObservableCollection<Model_SetupTech_DunnageAssignment>(state.DunnageAssignments);
        StatusMessage = "Loading dunnage assignment...";
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(WorkOrderId) || SequenceNo <= 0)
        {
            ErrorMessage = "The setup workflow is incomplete. Return to work-order selection and try again.";
            StatusMessage = string.Empty;
            return;
        }

        IsLoading = true;

        var assignmentResult = await _setupTechService.GetDunnageAssignmentAsync(WorkOrderId, SequenceNo);
        if (assignmentResult.IsSuccess && assignmentResult.Data is not null)
        {
            CurrentAssignments = new ObservableCollection<Model_SetupTech_DunnageAssignment>(assignmentResult.Data);
            state.DunnageAssignments = assignmentResult.Data.ToList();
        }

        var typeResult = await _setupTechService.GetEnabledDunnageTypesAsync();
        if (!typeResult.IsSuccess || typeResult.Data is null)
        {
            IsLoading = false;
            ErrorMessage = typeResult.ErrorMessage;
            StatusMessage = "Unable to load dunnage categories.";
            return;
        }

        EnabledDunnageTypes = new ObservableCollection<Model_SetupTech_DunnageTypeConfig>(typeResult.Data);
        SelectedTypeFilter = EnabledDunnageTypes.FirstOrDefault();

        var catalogResult = await _setupTechService.GetDunnageCatalogAsync();
        IsLoading = false;
        if (!catalogResult.IsSuccess || catalogResult.Data is null)
        {
            ErrorMessage = catalogResult.ErrorMessage;
            StatusMessage = "Assignments loaded, but the dunnage catalog is unavailable.";
            return;
        }

        _allCatalogItems = catalogResult.Data
            .OrderBy(item => item.DunnageTypeName)
            .ThenBy(item => item.DunnagePartName)
            .ToList();
        ApplyCatalogFilter();
        StatusMessage = "Select a dunnage item, then add it to the assignment.";
    }

    /// <summary>
    /// Returns to the validation step.
    /// </summary>
    [RelayCommand]
    private Task BackToValidationAsync()
    {
        NavigateBackRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds the currently selected dunnage item to the assignment.
    /// </summary>
    [RelayCommand]
    private async Task AddSelectedDunnageAsync()
    {
        if (SelectedDunnagePart is null)
        {
            ErrorMessage = "Select a dunnage item to add.";
            return;
        }

        var userId = _workflowState.Current.AuthSession?.UserId ?? 0;
        if (userId <= 0)
        {
            ErrorMessage = "The authenticated user could not be resolved for this save.";
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;

        var assignment = new Model_SetupTech_DunnageAssignment
        {
            AssignmentId = CurrentAssignments.FirstOrDefault(existing => existing.DunnagePartId == SelectedDunnagePart.DunnagePartId)?.AssignmentId ?? 0,
            WorkOrderId = WorkOrderId,
            SequenceNo = SequenceNo,
            DunnagePartId = SelectedDunnagePart.DunnagePartId,
            DunnagePartName = SelectedDunnagePart.DunnagePartName,
            DunnageTypeId = SelectedDunnagePart.DunnageTypeId,
            DunnageTypeName = SelectedDunnagePart.DunnageTypeName,
            LastModifiedByUserId = userId,
        };

        var result = await _setupTechService.UpsertDunnageAssignmentAsync(assignment);
        if (!result.IsSuccess)
        {
            IsSaving = false;
            ErrorMessage = result.ErrorMessage;
            return;
        }

        await ReloadAssignmentsAsync();
        IsSaving = false;
        StatusMessage = $"Added {assignment.DunnagePartName}.";
    }

    /// <summary>
    /// Removes a dunnage assignment from the current job.
    /// </summary>
    [RelayCommand]
    private async Task RemoveDunnageItemAsync(Model_SetupTech_DunnageAssignment? assignment)
    {
        if (assignment is null)
        {
            return;
        }

        if (assignment.AssignmentId <= 0)
        {
            CurrentAssignments.Remove(assignment);
            _workflowState.Current.DunnageAssignments = CurrentAssignments.ToList();
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;

        var result = await _setupTechService.DeleteDunnageAssignmentAsync(assignment.AssignmentId);
        if (!result.IsSuccess)
        {
            IsSaving = false;
            ErrorMessage = result.ErrorMessage;
            return;
        }

        await ReloadAssignmentsAsync();
        IsSaving = false;
        StatusMessage = $"Removed {assignment.DunnagePartName}.";
    }

    /// <summary>
    /// Saves the current job and transitions to the confirmation screen.
    /// </summary>
    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        var pendingActiveJob = _workflowState.Current.PendingActiveJob;
        if (pendingActiveJob is null)
        {
            ErrorMessage = "The job details are incomplete. Return to validation and try again.";
            return;
        }

        IsSaving = true;
        ErrorMessage = string.Empty;

        pendingActiveJob.DunnageAssignments = CurrentAssignments.ToList();
        pendingActiveJob.ActiveSince = DateTime.UtcNow;

        var result = await _setupTechService.SetActiveJobAsync(pendingActiveJob);
        IsSaving = false;
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        _workflowState.Current.DunnageAssignments = CurrentAssignments.ToList();
        _workflowState.Current.PendingActiveJob = pendingActiveJob;
        StatusMessage = "Setup saved successfully.";
        NavigateToConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task ReloadAssignmentsAsync()
    {
        var assignmentResult = await _setupTechService.GetDunnageAssignmentAsync(WorkOrderId, SequenceNo);
        if (!assignmentResult.IsSuccess || assignmentResult.Data is null)
        {
            ErrorMessage = assignmentResult.ErrorMessage;
            return;
        }

        CurrentAssignments = new ObservableCollection<Model_SetupTech_DunnageAssignment>(assignmentResult.Data);
        _workflowState.Current.DunnageAssignments = assignmentResult.Data.ToList();
    }

    private void ApplyCatalogFilter()
    {
        IEnumerable<Model_DunnagePart> filtered = _allCatalogItems;

        if (SelectedTypeFilter is not null)
        {
            filtered = filtered.Where(item => item.DunnageTypeId == SelectedTypeFilter.DunnageTypeId);
        }

        DunnageCatalog = new ObservableCollection<Model_DunnagePart>(filtered);
        SelectedDunnagePart = DunnageCatalog.FirstOrDefault();
    }

    partial void OnSelectedTypeFilterChanged(Model_SetupTech_DunnageTypeConfig? value)
    {
        ApplyCatalogFilter();
    }
}