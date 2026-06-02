using Core.Interfaces.SetupTech;
using Feature.Waitlist.ViewModels.SetupTechValidation;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechValidation.Failure;

public class ViewModel_Waitlist_SetupTechValidationTests
{
    [Fact]
    public async Task InitializeAsync_ShouldSetError_WhenWorkflowStateIsIncomplete()
    {
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        mockWorkflowState.SetupGet(service => service.Current).Returns(new Core.Models.SetupTech.Model_SetupTech_WorkflowState());
        var viewModel = new ViewModel_Waitlist_SetupTechValidation(mockWorkflowState.Object);

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("The setup workflow is incomplete. Return to work-order selection and try again.");
        viewModel.WorkcenterId.Should().BeEmpty();
    }

    [Fact]
    public async Task ApproveAsync_ShouldSetError_WhenSelectionIsMissing()
    {
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        var viewModel = new ViewModel_Waitlist_SetupTechValidation(mockWorkflowState.Object);
        var navigationRaised = false;
        viewModel.NavigateToDunnageRequested += (_, _) => navigationRaised = true;

        await viewModel.ApproveCommand.ExecuteAsync(null);

        navigationRaised.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Select a work order before continuing.");
    }
}