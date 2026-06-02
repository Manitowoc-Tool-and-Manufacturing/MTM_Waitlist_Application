using Core.Interfaces.Auth;
using Core.Interfaces.InforVisual;
using Core.Interfaces.SetupTech;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Feature.Waitlist.ViewModels.SetupTech;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTech.Properties;

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
    public void SelectedWorkcenter_ShouldEnableContinueToWorkOrder_WhenValueAssigned()
    {
        var viewModel = CreateViewModel();

        viewModel.CanContinueToWorkOrder.Should().BeFalse();

        viewModel.SelectedWorkcenter = new Model_VisualWorkcenter
        {
            WorkcenterId = "TEST-WC01",
            Description = "TEST-Assembly",
        };

        viewModel.CanContinueToWorkOrder.Should().BeTrue();
        _workflowState.SelectedWorkcenter.Should().NotBeNull();
        _workflowState.SelectedWorkcenter!.WorkcenterId.Should().Be("TEST-WC01");
    }

    [Fact]
    public void WorkOrderHeaderAndSelectedSequence_ShouldEnableContinueToValidation_WhenBothPresent()
    {
        var viewModel = CreateViewModel();

        viewModel.WorkOrderHeader = new Model_VisualWorkOrderHeader
        {
            WorkOrderId = "TEST-WO100",
        };

        viewModel.CanContinueToValidation.Should().BeFalse();

        viewModel.SelectedSequence = new Model_VisualWorkOrderSequence
        {
            WorkOrderId = "TEST-WO100",
            SequenceNo = 10,
        };

        viewModel.CanContinueToValidation.Should().BeTrue();
    }

    [Fact]
    public void CurrentStep_ShouldToggleStepFlags_WhenValueChanges()
    {
        var viewModel = CreateViewModel();

        viewModel.IsWorkstationStep.Should().BeTrue();
        viewModel.IsWorkOrderStep.Should().BeFalse();

        viewModel.CurrentStep = 2;

        viewModel.IsWorkstationStep.Should().BeFalse();
        viewModel.IsWorkOrderStep.Should().BeTrue();
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