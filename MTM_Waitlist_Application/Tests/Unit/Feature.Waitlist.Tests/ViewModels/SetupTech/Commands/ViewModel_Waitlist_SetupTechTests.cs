using Core.Interfaces.Auth;
using Core.Interfaces.InforVisual;
using Core.Interfaces.SetupTech;
using Core.Models.Auth;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Feature.Waitlist.ViewModels.SetupTech;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTech.Commands;

public class ViewModel_Waitlist_SetupTechTests
{
    private readonly Mock<IService_InforVisual> _mockInforVisualService;
    private readonly Mock<IService_SetupTech> _mockSetupTechService;
    private readonly Mock<IService_AuthTokenStore> _mockAuthTokenStore;
    private readonly Mock<IService_SetupTechWorkflowState> _mockWorkflowState;
    private Model_SetupTech_WorkflowState _currentWorkflowState;

    public ViewModel_Waitlist_SetupTechTests()
    {
        _mockInforVisualService = new Mock<IService_InforVisual>();
        _mockSetupTechService = new Mock<IService_SetupTech>();
        _mockAuthTokenStore = new Mock<IService_AuthTokenStore>();
        _mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        _currentWorkflowState = new Model_SetupTech_WorkflowState();

        _mockWorkflowState
            .SetupGet(service => service.Current)
            .Returns(() => _currentWorkflowState);
        _mockWorkflowState
            .Setup(service => service.Reset())
            .Callback(() => _currentWorkflowState = new Model_SetupTech_WorkflowState());

        _mockSetupTechService
            .Setup(service => service.GetActiveJobAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<Model_SetupTech_ActiveJob>.Failure("TEST-no-active-job"));
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetAccessDenied_WhenStoredSessionRoleIsUnsupported()
    {
        var viewModel = CreateViewModel();

        _mockAuthTokenStore
            .Setup(service => service.GetStoredSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Model_AuthToken
            {
                UserId = 41,
                Username = "TEST-user",
                DisplayName = "TEST User",
                Role = "Operator",
            });

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.IsAuthorized.Should().BeFalse();
        viewModel.ShowUnauthorizedState.Should().BeTrue();
        viewModel.AccessDeniedMessage.Should().Be("Setup Technician access is required to configure active jobs.");
        _mockInforVisualService.Verify(service => service.GetAllWorkcentersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LookupWorkOrderAsync_ShouldLoadMatchingSequences_WhenSelectedWorkcenterMatches()
    {
        var viewModel = CreateViewModel();
        var selectedWorkcenter = new Model_VisualWorkcenter
        {
            WorkcenterId = "TEST-WC01",
            Description = "TEST-Assembly",
        };

        _mockInforVisualService
            .Setup(service => service.GetWorkOrderHeaderAsync("TEST-WO100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<Model_VisualWorkOrderHeader>.Success(new Model_VisualWorkOrderHeader
            {
                WorkOrderId = "TEST-WO100",
                PartId = "TEST-PART-01",
                PartType = "TEST-TYPE",
                WorkOrderStatus = "Released",
            }));
        _mockInforVisualService
            .Setup(service => service.GetWorkOrderSequencesAsync("TEST-WO100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_VisualWorkOrderSequence>>.Success([
                new Model_VisualWorkOrderSequence
                {
                    WorkOrderId = "TEST-WO100",
                    SequenceNo = 10,
                    WorkcenterId = "TEST-WC01",
                    WorkcenterDescription = "TEST-Assembly",
                },
                new Model_VisualWorkOrderSequence
                {
                    WorkOrderId = "TEST-WO100",
                    SequenceNo = 20,
                    WorkcenterId = "TEST-WC02",
                    WorkcenterDescription = "TEST-Pack",
                },
            ]));

        viewModel.SelectedWorkcenter = selectedWorkcenter;
        viewModel.WorkOrderInput = "test-wo100";

        await viewModel.LookupWorkOrderCommand.ExecuteAsync(null);

        viewModel.WorkOrderInput.Should().Be("TEST-WO100");
        viewModel.WorkOrderHeader.Should().NotBeNull();
        viewModel.AvailableSequences.Should().HaveCount(1);
        viewModel.AvailableSequences[0].SequenceNo.Should().Be(10);
        viewModel.SelectedSequence.Should().NotBeNull();
        _currentWorkflowState.WorkOrderHeader.Should().NotBeNull();
        _currentWorkflowState.WorkOrderInput.Should().Be("TEST-WO100");
    }

    [Fact]
    public async Task ContinueToValidationAsync_ShouldPersistWorkflowAndRaiseNavigation_WhenSequenceSelected()
    {
        var viewModel = CreateViewModel();
        var selectedWorkcenter = new Model_VisualWorkcenter
        {
            WorkcenterId = "TEST-WC01",
            Description = "TEST-Assembly",
        };
        var selectedSequence = new Model_VisualWorkOrderSequence
        {
            WorkOrderId = "TEST-WO100",
            SequenceNo = 10,
            WorkcenterId = "TEST-WC01",
            WorkcenterDescription = "TEST-Assembly",
        };
        var workOrderHeader = new Model_VisualWorkOrderHeader
        {
            WorkOrderId = "TEST-WO100",
            PartId = "TEST-PART-01",
            PartType = "TEST-TYPE",
        };
        var subordinateParts = new List<Model_Visual_SubordinatePart>
        {
            new()
            {
                WorkOrderId = "TEST-WO100",
                SequenceNo = 10,
                PartId = "TEST-SUB-01",
                PartDescription = "TEST-Sub Part",
                RequiredQty = 2,
                QtyOnHand = 7,
            },
        };
        var navigationRaised = false;

        _currentWorkflowState.AuthSession = new Model_AuthToken
        {
            UserId = 77,
            Username = "TEST-tech",
            Role = "SetupTech",
        };
        _mockInforVisualService
            .Setup(service => service.GetSubordinatePartsAsync("TEST-WO100", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_Visual_SubordinatePart>>.Success(subordinateParts));

        viewModel.NavigateToValidationRequested += (_, _) => navigationRaised = true;
        viewModel.SelectedWorkcenter = selectedWorkcenter;
        viewModel.WorkOrderHeader = workOrderHeader;
        viewModel.SelectedSequence = selectedSequence;

        await viewModel.ContinueToValidationCommand.ExecuteAsync(null);

        navigationRaised.Should().BeTrue();
        _currentWorkflowState.SelectedWorkcenter.Should().BeSameAs(selectedWorkcenter);
        _currentWorkflowState.SelectedSequence.Should().BeSameAs(selectedSequence);
        _currentWorkflowState.SubordinateParts.Should().ContainSingle();
        _currentWorkflowState.PendingActiveJob.Should().NotBeNull();
        _currentWorkflowState.PendingActiveJob!.SetupTechUserId.Should().Be(77);
        _currentWorkflowState.PendingActiveJob.SubordinateParts.Should().ContainSingle(part => part.SubPartId == "TEST-SUB-01");
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