using Core.Models.SetupTech;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Stores the in-progress Setup Tech wizard state across page navigation.
/// </summary>
public interface IService_SetupTechWorkflowState
{
    /// <summary>The current workflow state instance.</summary>
    Model_SetupTech_WorkflowState Current { get; }

    /// <summary>Clears the workflow so a new setup can begin from step one.</summary>
    void Reset();
}