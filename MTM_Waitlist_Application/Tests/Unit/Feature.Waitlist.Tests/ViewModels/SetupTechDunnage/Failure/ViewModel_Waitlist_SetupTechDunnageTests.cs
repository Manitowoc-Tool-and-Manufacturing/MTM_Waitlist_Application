using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Feature.Waitlist.ViewModels.SetupTechDunnage;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechDunnage.Failure;

public class ViewModel_Waitlist_SetupTechDunnageTests
{
    private readonly Mock<IService_SetupTech> _mockSetupTechService;
    private readonly Mock<IService_SetupTechWorkflowState> _mockWorkflowState;
    private readonly Model_SetupTech_WorkflowState _workflowState;

    public ViewModel_Waitlist_SetupTechDunnageTests()
    {
        _mockSetupTechService = new Mock<IService_SetupTech>();
        _mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        _workflowState = new Model_SetupTech_WorkflowState();

        _mockWorkflowState
            .SetupGet(service => service.Current)
            .Returns(_workflowState);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetError_WhenWorkflowStateIsIncomplete()
    {
        var viewModel = CreateViewModel();

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("The setup workflow is incomplete. Return to work-order selection and try again.");
        viewModel.StatusMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAndContinueAsync_ShouldSetError_WhenSetActiveJobFails()
    {
        var viewModel = CreateViewModel();
        _workflowState.PendingActiveJob = new Model_SetupTech_ActiveJob
        {
            WorkcenterId = "TEST-WC01",
            WorkOrderId = "TEST-WO100",
            SequenceNo = 10,
            PartId = "TEST-PART-01",
            SetupTechUserId = 7,
        };
        viewModel.CurrentAssignments =
        [
            new Model_SetupTech_DunnageAssignment
            {
                AssignmentId = 12,
                WorkOrderId = "TEST-WO100",
                SequenceNo = 10,
                DunnagePartId = 100,
                DunnagePartName = "TEST-Bin",
                DunnageTypeId = 1,
                DunnageTypeName = "TEST-Type A",
            },
        ];

        _mockSetupTechService
            .Setup(service => service.SetActiveJobAsync(It.IsAny<Model_SetupTech_ActiveJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result.Failure("TEST-save-failed"));

        await viewModel.SaveAndContinueCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("TEST-save-failed");
        _workflowState.DunnageAssignments.Should().BeEmpty();
    }

    private ViewModel_Waitlist_SetupTechDunnage CreateViewModel()
    {
        return new ViewModel_Waitlist_SetupTechDunnage(_mockSetupTechService.Object, _mockWorkflowState.Object);
    }
}