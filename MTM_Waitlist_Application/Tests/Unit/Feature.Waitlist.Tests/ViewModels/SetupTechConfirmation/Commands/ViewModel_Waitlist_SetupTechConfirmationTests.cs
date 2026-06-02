using Core.Interfaces.SetupTech;
using Core.Models.Auth;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Feature.Waitlist.ViewModels.SetupTechConfirmation;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechConfirmation.Commands;

public class ViewModel_Waitlist_SetupTechConfirmationTests
{
    [Fact]
    public async Task InitializeAsync_ShouldLoadCompletionSummary_WhenWorkflowHasSavedJob()
    {
        var workflowState = new Model_SetupTech_WorkflowState
        {
            AuthSession = new Model_AuthToken
            {
                Username = "TEST-tech",
                DisplayName = "TEST Tech",
            },
            SelectedWorkcenter = new Model_VisualWorkcenter
            {
                WorkcenterId = "TEST-WC01",
            },
            ExistingActiveJob = new Model_SetupTech_ActiveJob
            {
                WorkOrderId = "TEST-OLD",
            },
            PendingActiveJob = new Model_SetupTech_ActiveJob
            {
                WorkcenterId = "TEST-WC01",
                WorkOrderId = "TEST-WO100",
                SequenceNo = 10,
                PartId = "TEST-PART-01",
            },
            SubordinateParts =
            [
                new Model_SetupTech_SubordinatePart
                {
                    SubPartId = "TEST-SUB-01",
                },
            ],
            DunnageAssignments =
            [
                new Model_SetupTech_DunnageAssignment
                {
                    DunnagePartName = "TEST-Bin",
                },
            ],
        };
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        mockWorkflowState.SetupGet(service => service.Current).Returns(workflowState);

        var viewModel = new ViewModel_Waitlist_SetupTechConfirmation(mockWorkflowState.Object);

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.ActiveJob.Should().NotBeNull();
        viewModel.WorkcenterId.Should().Be("TEST-WC01");
        viewModel.SavedByDisplayName.Should().Be("TEST Tech");
        viewModel.HadArchivedExistingJob.Should().BeTrue();
        viewModel.SubordinateParts.Should().ContainSingle();
        viewModel.DunnageAssignments.Should().ContainSingle();
        viewModel.SavedTimestamp.Should().NotBeNullOrWhiteSpace();
        viewModel.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task StartOverAsync_ShouldResetWorkflowAndRaiseEvent_WhenInvoked()
    {
        var workflowState = new Model_SetupTech_WorkflowState
        {
            PendingActiveJob = new Model_SetupTech_ActiveJob
            {
                WorkOrderId = "TEST-WO100",
            },
        };
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        mockWorkflowState.SetupGet(service => service.Current).Returns(() => workflowState);
        mockWorkflowState.Setup(service => service.Reset()).Callback(() => workflowState = new Model_SetupTech_WorkflowState());

        var viewModel = new ViewModel_Waitlist_SetupTechConfirmation(mockWorkflowState.Object);
        var startOverRaised = false;
        viewModel.StartOverRequested += (_, _) => startOverRaised = true;

        await viewModel.StartOverCommand.ExecuteAsync(null);

        startOverRaised.Should().BeTrue();
        workflowState.PendingActiveJob.Should().BeNull();
        mockWorkflowState.Verify(service => service.Reset(), Times.Once);
    }
}