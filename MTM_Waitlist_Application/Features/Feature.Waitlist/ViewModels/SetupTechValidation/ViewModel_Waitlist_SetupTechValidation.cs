using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces.SetupTech;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using System.Collections.ObjectModel;

namespace Feature.Waitlist.ViewModels.SetupTechValidation;

/// <summary>
/// ViewModel for the Setup Tech validation step.
/// </summary>
public partial class ViewModel_Waitlist_SetupTechValidation : ObservableObject
{
    private readonly IService_SetupTechWorkflowState _workflowState;

    /// <summary>
    /// Raised when the user wants to return to work-order selection.
    /// </summary>
    public event EventHandler? NavigateBackRequested;

    /// <summary>
    /// Raised when the user approves the job details and continues to dunnage.
    /// </summary>
    public event EventHandler? NavigateToDunnageRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModel_Waitlist_SetupTechValidation"/> class.
    /// </summary>
    public ViewModel_Waitlist_SetupTechValidation(IService_SetupTechWorkflowState workflowState)
    {
        _workflowState = workflowState;
    }

    /// <summary>The selected workstation for the current job.</summary>
    [ObservableProperty]
    private string _workcenterId = string.Empty;

    /// <summary>The selected work-order header.</summary>
    [ObservableProperty]
    private Model_VisualWorkOrderHeader? _workOrderHeader;

    /// <summary>The selected work-order sequence.</summary>
    [ObservableProperty]
    private Model_VisualWorkOrderSequence? _selectedSequence;

    /// <summary>The subordinate parts shown during validation.</summary>
    [ObservableProperty]
    private ObservableCollection<Model_SetupTech_SubordinatePart> _subordinateParts = [];

    /// <summary>Validation error text shown when setup state is incomplete.</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>Indicates whether the validation page is loading its state.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Loads the validation state from the shared workflow store.
    /// </summary>
    [RelayCommand]
    private Task InitializeAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        var state = _workflowState.Current;
        WorkcenterId = state.SelectedWorkcenter?.WorkcenterId ?? string.Empty;
        WorkOrderHeader = state.WorkOrderHeader;
        SelectedSequence = state.SelectedSequence;
        SubordinateParts = new ObservableCollection<Model_SetupTech_SubordinatePart>(state.SubordinateParts);

        if (string.IsNullOrWhiteSpace(WorkcenterId) || WorkOrderHeader is null || SelectedSequence is null)
        {
            ErrorMessage = "The setup workflow is incomplete. Return to work-order selection and try again.";
        }

        IsLoading = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Approves the current job details and continues to the dunnage step.
    /// </summary>
    [RelayCommand]
    private Task ApproveAsync()
    {
        if (WorkOrderHeader is null || SelectedSequence is null)
        {
            ErrorMessage = "Select a work order before continuing.";
            return Task.CompletedTask;
        }

        NavigateToDunnageRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns to work-order selection.
    /// </summary>
    [RelayCommand]
    private Task ChangeWorkOrderAsync()
    {
        NavigateBackRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}