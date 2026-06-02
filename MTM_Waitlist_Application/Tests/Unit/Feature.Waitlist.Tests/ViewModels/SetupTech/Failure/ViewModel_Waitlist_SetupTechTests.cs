using Core.Interfaces.Auth;
using Core.Interfaces.InforVisual;
using Core.Interfaces.SetupTech;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Feature.Waitlist.ViewModels.SetupTech;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTech.Failure;

public class ViewModel_Waitlist_SetupTechTests
{
    private readonly Mock<IService_InforVisual> _mockInforVisualService;
    private readonly Mock<IService_SetupTech> _mockSetupTechService;
    private readonly Mock<IService_AuthTokenStore> _mockAuthTokenStore;
    private readonly Mock<IService_SetupTechWorkflowState> _mockWorkflowState;
    private readonly Model_SetupTech_WorkflowState _workflowState;

    public ViewModel_Waitlist_SetupTechTests()
    {
        _mockInforVisualService = new Mock<IService_InforVisual>();
        _mockSetupTechService = new Mock<IService_SetupTech>();
        _mockAuthTokenStore = new Mock<IService_AuthTokenStore>();
        _mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        _workflowState = new Model_SetupTech_WorkflowState();

        _mockWorkflowState
            .SetupGet(service => service.Current)
            .Returns(_workflowState);
        _mockSetupTechService
            .Setup(service => service.GetActiveJobAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<Model_SetupTech_ActiveJob>.Failure("TEST-no-active-job"));
    }

    [Fact]
    public async Task LoadWorkcentersAsync_ShouldExposeError_WhenInforVisualLookupFails()
    {
        var viewModel = CreateViewModel();

        _mockInforVisualService
            .Setup(service => service.GetAllWorkcentersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_VisualWorkcenter>>.Failure("TEST-workcenters-unavailable"));

        await viewModel.LoadWorkcentersCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("TEST-workcenters-unavailable");
        viewModel.StatusMessage.Should().Be("Unable to load workstations.");
        viewModel.AvailableWorkcenters.Should().BeEmpty();
    }

    [Fact]
    public async Task ContinueToValidationAsync_ShouldSetError_WhenSubordinatePartLookupFails()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedWorkcenter = new Model_VisualWorkcenter
        {
            WorkcenterId = "TEST-WC01",
            Description = "TEST-Assembly",
        };
        viewModel.WorkOrderHeader = new Model_VisualWorkOrderHeader
        {
            WorkOrderId = "TEST-WO100",
            PartId = "TEST-PART-01",
            PartType = "TEST-TYPE",
        };
        viewModel.SelectedSequence = new Model_VisualWorkOrderSequence
        {
            WorkOrderId = "TEST-WO100",
            SequenceNo = 10,
            WorkcenterId = "TEST-WC01",
        };

        _mockInforVisualService
            .Setup(service => service.GetSubordinatePartsAsync("TEST-WO100", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_Visual_SubordinatePart>>.Failure("TEST-subparts-unavailable"));

        await viewModel.ContinueToValidationCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("TEST-subparts-unavailable");
        viewModel.StatusMessage.Should().Be("Subordinate part lookup failed.");
        _workflowState.PendingActiveJob.Should().BeNull();
    }

    private ViewModel_Waitlist_SetupTech CreateViewModel()
    {
        return new ViewModel_Waitlist_SetupTech(
            _mockInforVisualService.Object,
            _mockSetupTechService.Object,
            _mockWorkflowState.Object,
            _mockAuthTokenStore.Object);
    }
}