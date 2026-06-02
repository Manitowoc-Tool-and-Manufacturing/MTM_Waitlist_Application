using Core.Interfaces.SetupTech;
using Core.Models.Auth;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Feature.Waitlist.ViewModels.SetupTechConfirmation;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechConfirmation.Properties;

public class ViewModel_Waitlist_SetupTechConfirmationTests
{
    [Fact]
    public async Task InitializeAsync_ShouldFallbackToUsernameAndKeepArchivedFlagFalse_WhenDisplayNameAndExistingJobAreMissing()
    {
        var workflowState = new Model_SetupTech_WorkflowState
        {
            AuthSession = new Model_AuthToken
            {
                Username = "TEST-tech",
            },
            SelectedWorkcenter = new Model_VisualWorkcenter
            {
                WorkcenterId = "TEST-WC01",
            },
            PendingActiveJob = new Model_SetupTech_ActiveJob
            {
                WorkOrderId = "TEST-WO100",
            },
        };
        var mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        mockWorkflowState.SetupGet(service => service.Current).Returns(workflowState);

        var viewModel = new ViewModel_Waitlist_SetupTechConfirmation(mockWorkflowState.Object);

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.SavedByDisplayName.Should().Be("TEST-tech");
        viewModel.WorkcenterId.Should().Be("TEST-WC01");
        viewModel.HadArchivedExistingJob.Should().BeFalse();
        viewModel.ActiveJob.Should().NotBeNull();
    }
}