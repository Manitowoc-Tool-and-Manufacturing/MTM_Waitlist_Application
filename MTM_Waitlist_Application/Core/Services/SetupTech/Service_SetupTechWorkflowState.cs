using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;

namespace Services.SetupTech;

/// <summary>
/// In-memory store for the current Setup Tech wizard state.
/// </summary>
public sealed class Service_SetupTechWorkflowState : IService_SetupTechWorkflowState
{
    /// <inheritdoc/>
    public Model_SetupTech_WorkflowState Current { get; private set; } = new();

    /// <inheritdoc/>
    public void Reset()
    {
        Current = new Model_SetupTech_WorkflowState();
    }
}