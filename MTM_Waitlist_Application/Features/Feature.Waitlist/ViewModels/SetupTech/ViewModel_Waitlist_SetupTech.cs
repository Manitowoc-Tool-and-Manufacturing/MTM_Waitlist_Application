using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums.Auth;
using Core.Interfaces.Auth;
using Core.Interfaces.InforVisual;
using Core.Interfaces.SetupTech;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using System.Collections.ObjectModel;

namespace Feature.Waitlist.ViewModels.SetupTech;

/// <summary>
/// ViewModel for the Setup Tech wizard's workstation and work-order steps.
/// </summary>
public partial class ViewModel_Waitlist_SetupTech : ObservableObject
{
    private readonly IService_InforVisual _inforVisualService;
    private readonly IService_SetupTech _setupTechService;
    private readonly IService_SetupTechWorkflowState _workflowState;
    private readonly IService_AuthTokenStore _authTokenStore;
    private List<Model_VisualWorkcenter> _allWorkcenters = [];
    private List<Model_VisualWorkOrderSequence> _allSequences = [];

    /// <summary>
    /// Raised when the workflow is ready to continue to validation.
    /// </summary>
    public event EventHandler? NavigateToValidationRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModel_Waitlist_SetupTech"/> class.
    /// </summary>
    public ViewModel_Waitlist_SetupTech(
        IService_InforVisual inforVisualService,
        IService_SetupTech setupTechService,
        IService_SetupTechWorkflowState workflowState,
        IService_AuthTokenStore authTokenStore)
    {
        _inforVisualService = inforVisualService;
        _setupTechService = setupTechService;
        _workflowState = workflowState;
        _authTokenStore = authTokenStore;
    }

    /// <summary>The currently authenticated display name shown in the workflow.</summary>
    [ObservableProperty]
    private string _currentDisplayName = string.Empty;

    /// <summary>Indicates whether the current session is allowed to use Setup Tech workflows.</summary>
    [ObservableProperty]
    private bool _isAuthorized;

    /// <summary>The access-denied message shown for unsupported roles.</summary>
    [ObservableProperty]
    private string _accessDeniedMessage = string.Empty;

    /// <summary>Workcenters available for workstation selection.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_VisualWorkcenter> _availableWorkcenters = [];

    /// <summary>The search text applied to the workstation list.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>The selected workstation for the current setup flow.</summary>
    [ObservableProperty]
    private Model_VisualWorkcenter? _selectedWorkcenter;

    /// <summary>The active job currently running at the selected workstation.</summary>
    [ObservableProperty]
    private Model_SetupTech_ActiveJob? _currentActiveJob;

    /// <summary>The current step shown on the setup page.</summary>
    [ObservableProperty]
    private int _currentStep = 1;

    /// <summary>The raw work-order input or scanned barcode value.</summary>
    [ObservableProperty]
    private string _workOrderInput = string.Empty;

    /// <summary>The retrieved work-order header for the entered work order.</summary>
    [ObservableProperty]
    private Model_VisualWorkOrderHeader? _workOrderHeader;

    /// <summary>The list of available sequences for the current work order.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_VisualWorkOrderSequence> _availableSequences = [];

    /// <summary>The currently selected work-order sequence.</summary>
    [ObservableProperty]
    private Model_VisualWorkOrderSequence? _selectedSequence;

    /// <summary>General status text shown beneath the workflow.</summary>
    [ObservableProperty]
    private string _statusMessage = "Select a workstation to begin.";

    /// <summary>Error text shown when a workflow operation fails.</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>Indicates whether a workflow operation is currently running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Returns <see langword="true"/> when the access-denied card should be shown.</summary>
    public bool ShowUnauthorizedState => !IsAuthorized;

    /// <summary>Returns <see langword="true"/> when the workstation step should be shown.</summary>
    public bool IsWorkstationStep => CurrentStep == 1;

    /// <summary>Returns <see langword="true"/> when the work-order step should be shown.</summary>
    public bool IsWorkOrderStep => CurrentStep == 2;

    /// <summary>Returns <see langword="true"/> when an active-job notice should be displayed.</summary>
    public bool HasCurrentActiveJob => CurrentActiveJob is not null && !string.IsNullOrWhiteSpace(CurrentActiveJob.WorkOrderId);

    /// <summary>Returns <see langword="true"/> when the user can continue to work-order entry.</summary>
    public bool CanContinueToWorkOrder => SelectedWorkcenter is not null;

    /// <summary>Returns <see langword="true"/> when the user can look up the current work order.</summary>
    public bool CanLookupWorkOrder => SelectedWorkcenter is not null && !string.IsNullOrWhiteSpace(WorkOrderInput);

    /// <summary>Returns <see langword="true"/> when the user can continue to validation.</summary>
    public bool CanContinueToValidation => WorkOrderHeader is not null && SelectedSequence is not null;

    /// <summary>
    /// Performs first-load initialization for the setup workflow.
    /// </summary>
    [RelayCommand]
    private async Task InitializeAsync()
    {
        _workflowState.Reset();
        await LoadSessionAsync();

        if (!IsAuthorized)
        {
            return;
        }

        await LoadWorkcentersAsync();
    }

    /// <summary>
    /// Restores or resets the view state when returning to this page.
    /// </summary>
    [RelayCommand]
    private async Task RestoreStateAsync()
    {
        if (!IsAuthorized)
        {
            return;
        }

        if (_workflowState.Current.SelectedWorkcenter is null && CurrentStep != 1)
        {
            await ResetWizardAsync();
        }
    }

    /// <summary>
    /// Loads all available workcenters and applies the current filter text.
    /// </summary>
    [RelayCommand]
    private async Task LoadWorkcentersAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        StatusMessage = "Loading workstations...";

        var result = await _inforVisualService.GetAllWorkcentersAsync();
        IsBusy = false;

        if (!result.IsSuccess || result.Data is null)
        {
            ErrorMessage = result.ErrorMessage;
            StatusMessage = "Unable to load workstations.";
            return;
        }

        _allWorkcenters = result.Data
            .OrderBy(workcenter => workcenter.WorkcenterId)
            .ToList();

        ApplyWorkcenterFilter();
        StatusMessage = $"Loaded {_allWorkcenters.Count} workstations.";
    }

    /// <summary>
    /// Moves the workflow back to workstation selection.
    /// </summary>
    [RelayCommand]
    private void BackToWorkstation()
    {
        CurrentStep = 1;
        ErrorMessage = string.Empty;
        StatusMessage = "Select a workstation to continue.";
    }

    /// <summary>
    /// Continues from workstation selection to work-order entry.
    /// </summary>
    [RelayCommand]
    private void ContinueToWorkOrder()
    {
        if (SelectedWorkcenter is null)
        {
            ErrorMessage = "Select a workstation before continuing.";
            return;
        }

        CurrentStep = 2;
        ErrorMessage = string.Empty;
        StatusMessage = $"{SelectedWorkcenter.WorkcenterId} selected. Enter or scan a work order.";
    }

    /// <summary>
    /// Looks up the current work order and available sequences for the selected workstation.
    /// </summary>
    [RelayCommand]
    private async Task LookupWorkOrderAsync()
    {
        if (!CanLookupWorkOrder || SelectedWorkcenter is null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        StatusMessage = "Loading work order...";

        var normalizedWorkOrder = WorkOrderInput.Trim().ToUpperInvariant();
        var headerResult = await _inforVisualService.GetWorkOrderHeaderAsync(normalizedWorkOrder);
        if (!headerResult.IsSuccess || headerResult.Data is null)
        {
            IsBusy = false;
            ErrorMessage = headerResult.ErrorMessage;
            StatusMessage = "Work order lookup failed.";
            return;
        }

        var sequenceResult = await _inforVisualService.GetWorkOrderSequencesAsync(normalizedWorkOrder);
        IsBusy = false;
        if (!sequenceResult.IsSuccess || sequenceResult.Data is null)
        {
            ErrorMessage = sequenceResult.ErrorMessage;
            StatusMessage = "No sequences were returned.";
            return;
        }

        WorkOrderInput = normalizedWorkOrder;
        WorkOrderHeader = headerResult.Data;
        _workflowState.Current.WorkOrderInput = normalizedWorkOrder;
        _workflowState.Current.WorkOrderHeader = headerResult.Data;

        _allSequences = sequenceResult.Data
            .Where(sequence => string.Equals(sequence.WorkcenterId, SelectedWorkcenter.WorkcenterId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (_allSequences.Count == 0)
        {
            _allSequences = sequenceResult.Data.ToList();
            StatusMessage = "Work order loaded. No exact workstation match was found, so all sequences are shown.";
        }
        else
        {
            StatusMessage = $"Work order loaded. {_allSequences.Count} sequence(s) available.";
        }

        AvailableSequences = new ObservableCollection<Model_VisualWorkOrderSequence>(
            _allSequences.OrderBy(sequence => sequence.SequenceNo));
        SelectedSequence = AvailableSequences.FirstOrDefault();
    }

    /// <summary>
    /// Loads subordinate parts and continues to the validation step.
    /// </summary>
    [RelayCommand]
    private async Task ContinueToValidationAsync()
    {
        if (!CanContinueToValidation || SelectedWorkcenter is null || WorkOrderHeader is null || SelectedSequence is null)
        {
            ErrorMessage = "Select a sequence before continuing.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        StatusMessage = "Loading subordinate parts...";

        var subordinatePartsResult = await _inforVisualService.GetSubordinatePartsAsync(WorkOrderHeader.WorkOrderId, SelectedSequence.SequenceNo);
        IsBusy = false;

        if (!subordinatePartsResult.IsSuccess || subordinatePartsResult.Data is null)
        {
            ErrorMessage = subordinatePartsResult.ErrorMessage;
            StatusMessage = "Subordinate part lookup failed.";
            return;
        }

        _workflowState.Current.SelectedWorkcenter = SelectedWorkcenter;
        _workflowState.Current.ExistingActiveJob = CurrentActiveJob;
        _workflowState.Current.WorkOrderHeader = WorkOrderHeader;
        _workflowState.Current.SelectedSequence = SelectedSequence;
        _workflowState.Current.SubordinateParts = subordinatePartsResult.Data
            .Select(part => new Model_SetupTech_SubordinatePart
            {
                WorkOrderId = part.WorkOrderId,
                SequenceNo = part.SequenceNo,
                SubPartId = part.PartId,
                SubPartDesc = part.PartDescription,
                RequiredQty = part.RequiredQty,
                QtyOnHand = part.QtyOnHand,
            })
            .ToList();

        _workflowState.Current.PendingActiveJob = new Model_SetupTech_ActiveJob
        {
            WorkcenterId = SelectedWorkcenter.WorkcenterId,
            WorkOrderId = WorkOrderHeader.WorkOrderId,
            SequenceNo = SelectedSequence.SequenceNo,
            PartId = WorkOrderHeader.PartId,
            PartType = WorkOrderHeader.PartType,
            SetupTechUserId = _workflowState.Current.AuthSession?.UserId ?? 0,
            ActiveSince = DateTime.UtcNow,
            SubordinateParts = _workflowState.Current.SubordinateParts,
        };

        StatusMessage = "Validation data loaded.";
        NavigateToValidationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears the current wizard state and reloads workcenters.
    /// </summary>
    [RelayCommand]
    private async Task ResetWizardAsync()
    {
        var session = _workflowState.Current.AuthSession;
        _workflowState.Reset();
        _workflowState.Current.AuthSession = session;

        SearchText = string.Empty;
        SelectedWorkcenter = null;
        CurrentActiveJob = null;
        CurrentStep = 1;
        WorkOrderInput = string.Empty;
        WorkOrderHeader = null;
        AvailableSequences = [];
        SelectedSequence = null;
        ErrorMessage = string.Empty;
        StatusMessage = "Select a workstation to begin.";

        await LoadWorkcentersAsync();
    }

    private async Task LoadSessionAsync()
    {
        var session = await _authTokenStore.GetStoredSessionAsync();
        _workflowState.Current.AuthSession = session;
        CurrentDisplayName = string.IsNullOrWhiteSpace(session?.DisplayName)
            ? session?.Username ?? string.Empty
            : session.DisplayName;

        IsAuthorized = session is not null && IsAllowedRole(session.Role);
        AccessDeniedMessage = IsAuthorized
            ? string.Empty
            : "Setup Technician access is required to configure active jobs.";
        StatusMessage = IsAuthorized
            ? "Select a workstation to begin."
            : AccessDeniedMessage;
    }

    private async Task LoadActiveJobAsync(Model_VisualWorkcenter workcenter)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        StatusMessage = $"Loading current job for {workcenter.WorkcenterId}...";

        var activeJobResult = await _setupTechService.GetActiveJobAsync(workcenter.WorkcenterId);
        IsBusy = false;

        if (!activeJobResult.IsSuccess)
        {
            CurrentActiveJob = null;
            StatusMessage = $"{workcenter.WorkcenterId} selected. No active job cache was found.";
            return;
        }

        CurrentActiveJob = activeJobResult.Data;
        StatusMessage = $"{workcenter.WorkcenterId} selected. Enter or scan a work order.";
    }

    private void ApplyWorkcenterFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allWorkcenters
            : _allWorkcenters.Where(workcenter =>
                workcenter.WorkcenterId.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || workcenter.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        AvailableWorkcenters = new ObservableCollection<Model_VisualWorkcenter>(filtered);
    }

    private static bool IsAllowedRole(string role)
    {
        return string.Equals(role, Enum_UserRole.SetupTech.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Enum_UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Enum_UserRole.Developer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyWorkcenterFilter();
    }

    partial void OnSelectedWorkcenterChanged(Model_VisualWorkcenter? value)
    {
        OnPropertyChanged(nameof(CanContinueToWorkOrder));

        if (value is null)
        {
            return;
        }

        _workflowState.Current.SelectedWorkcenter = value;
        WorkOrderInput = string.Empty;
        WorkOrderHeader = null;
        AvailableSequences = [];
        SelectedSequence = null;
        ErrorMessage = string.Empty;
        _ = LoadActiveJobAsync(value);
    }

    partial void OnWorkOrderInputChanged(string value)
    {
        OnPropertyChanged(nameof(CanLookupWorkOrder));
    }

    partial void OnCurrentActiveJobChanged(Model_SetupTech_ActiveJob? value)
    {
        OnPropertyChanged(nameof(HasCurrentActiveJob));
    }

    partial void OnIsAuthorizedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowUnauthorizedState));
    }

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsWorkstationStep));
        OnPropertyChanged(nameof(IsWorkOrderStep));
    }

    partial void OnSelectedSequenceChanged(Model_VisualWorkOrderSequence? value)
    {
        OnPropertyChanged(nameof(CanContinueToValidation));
    }

    partial void OnWorkOrderHeaderChanged(Model_VisualWorkOrderHeader? value)
    {
        OnPropertyChanged(nameof(CanContinueToValidation));
    }
}