using Core.Models.Auth;
using Core.Models.InforVisual;

namespace Core.Models.SetupTech;

/// <summary>
/// Stores the in-progress Setup Tech wizard state shared across the feature's pages.
/// </summary>
public sealed class Model_SetupTech_WorkflowState
{
    /// <summary>The currently authenticated session driving the workflow.</summary>
    public Model_AuthToken? AuthSession { get; set; }

    /// <summary>The selected workstation for the current setup flow.</summary>
    public Model_VisualWorkcenter? SelectedWorkcenter { get; set; }

    /// <summary>The active job currently configured on the selected workstation.</summary>
    public Model_SetupTech_ActiveJob? ExistingActiveJob { get; set; }

    /// <summary>The raw work-order input or scanned barcode value.</summary>
    public string WorkOrderInput { get; set; } = string.Empty;

    /// <summary>The retrieved work-order header for the current setup.</summary>
    public Model_VisualWorkOrderHeader? WorkOrderHeader { get; set; }

    /// <summary>The selected sequence for the current setup.</summary>
    public Model_VisualWorkOrderSequence? SelectedSequence { get; set; }

    /// <summary>The subordinate parts loaded during validation.</summary>
    public List<Model_SetupTech_SubordinatePart> SubordinateParts { get; set; } = [];

    /// <summary>The current dunnage assignments associated with the job.</summary>
    public List<Model_SetupTech_DunnageAssignment> DunnageAssignments { get; set; } = [];

    /// <summary>The latest active-job payload prepared for confirmation and save.</summary>
    public Model_SetupTech_ActiveJob? PendingActiveJob { get; set; }
}