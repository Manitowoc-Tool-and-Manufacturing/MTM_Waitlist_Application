using Core.Interfaces.SetupTech;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Feature.Waitlist.ViewModels.SetupTechValidation;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechValidation.Commands;

public class ViewModel_Waitlist_SetupTechValidationTests
{
    [Fact]
    public async Task InitializeAsync_ShouldPopulateState_WhenWorkflowStateIsComplete()
    {
        var workflowState = new Model_SetupTech_WorkflowState
        {
            SelectedWorkcenter = new Model_VisualWorkcenter
            {
                WorkcenterId = "TEST-WC01",
            },
            WorkOrderHeader = new Model_VisualWorkOrderHeader
            {
                WorkOrderId = "TEST-WO100",
                PartId = "TEST-PART-01",
            },
            SelectedSequence = new Model_VisualWorkOrderSequence
            {
                WorkOrderId = "TEST-WO100",
                SequenceNo = 10,
            },
            SubordinateParts =
            [
                new Model_SetupTech_SubordinatePart
                {
                    WorkOrderId = "TEST-WO100",
                    SequenceNo = 10,
                    SubPartId = "TEST-SUB-01",
                },
            ],
        };
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        mockWorkflowState.SetupGet(service => service.Current).Returns(workflowState);

        var viewModel = new ViewModel_Waitlist_SetupTechValidation(mockWorkflowState.Object);

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.WorkcenterId.Should().Be("TEST-WC01");
        viewModel.WorkOrderHeader.Should().NotBeNull();
        viewModel.SelectedSequence.Should().NotBeNull();
        viewModel.SubordinateParts.Should().ContainSingle();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveAsync_ShouldRaiseNavigateToDunnage_WhenSelectionPresent()
    {
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        var viewModel = new ViewModel_Waitlist_SetupTechValidation(mockWorkflowState.Object)
        {
            WorkOrderHeader = new Model_VisualWorkOrderHeader
            {
                WorkOrderId = "TEST-WO100",
            },
            SelectedSequence = new Model_VisualWorkOrderSequence
            {
                SequenceNo = 10,
            },
        };
        var navigationRaised = false;
        viewModel.NavigateToDunnageRequested += (_, _) => navigationRaised = true;

        await viewModel.ApproveCommand.ExecuteAsync(null);

        navigationRaised.Should().BeTrue();
        viewModel.ErrorMessage.Should().BeEmpty();
    }
}