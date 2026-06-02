using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Feature.Waitlist.ViewModels.SetupTechConfirmation;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechConfirmation.Failure;

public class ViewModel_Waitlist_SetupTechConfirmationTests
{
    [Fact]
    public async Task InitializeAsync_ShouldSetError_WhenSavedJobIsMissing()
    {
        var workflowState = new Model_SetupTech_WorkflowState();
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        mockWorkflowState.SetupGet(service => service.Current).Returns(workflowState);

        var viewModel = new ViewModel_Waitlist_SetupTechConfirmation(mockWorkflowState.Object);

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.ActiveJob.Should().BeNull();
        viewModel.ErrorMessage.Should().Be("No saved Setup Tech job is available to display.");
    }
}