using Core.Interfaces.SetupTech;
using Core.Models.Auth;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Feature.Waitlist.ViewModels.SetupTechDunnage;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechDunnage.Commands;

public class ViewModel_Waitlist_SetupTechDunnageTests
{
    private readonly Mock<IService_SetupTech> _mockSetupTechService;
    private readonly Mock<IService_SetupTechWorkflowState> _mockWorkflowState;
    private Model_SetupTech_WorkflowState _currentWorkflowState;

    public ViewModel_Waitlist_SetupTechDunnageTests()
    {
        _mockSetupTechService = new Mock<IService_SetupTech>();
        _mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        _currentWorkflowState = new Model_SetupTech_WorkflowState();

        _mockWorkflowState
            .SetupGet(service => service.Current)
            .Returns(() => _currentWorkflowState);
        _mockWorkflowState
            .Setup(service => service.Reset())
            .Callback(() => _currentWorkflowState = new Model_SetupTech_WorkflowState());
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadAssignmentsAndFilterCatalog_WhenWorkflowStateIsComplete()
    {
        var viewModel = CreateViewModel();
        _currentWorkflowState.WorkOrderHeader = new Model_VisualWorkOrderHeader
        {
            WorkOrderId = "TEST-WO100",
        };
        _currentWorkflowState.SelectedSequence = new Model_VisualWorkOrderSequence
        {
            SequenceNo = 10,
        };

        _mockSetupTechService
            .Setup(service => service.GetDunnageAssignmentAsync("TEST-WO100", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Success([
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
            ]));
        _mockSetupTechService
            .Setup(service => service.GetEnabledDunnageTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Success([
                new Model_SetupTech_DunnageTypeConfig
                {
                    DunnageTypeId = 1,
                    DunnageTypeName = "TEST-Type A",
                    IsEnabled = true,
                },
                new Model_SetupTech_DunnageTypeConfig
                {
                    DunnageTypeId = 2,
                    DunnageTypeName = "TEST-Type B",
                    IsEnabled = true,
                },
            ]));
        _mockSetupTechService
            .Setup(service => service.GetDunnageCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_DunnagePart>>.Success([
                new Model_DunnagePart
                {
                    DunnagePartId = 300,
                    DunnagePartName = "TEST-Crate",
                    DunnageTypeId = 2,
                    DunnageTypeName = "TEST-Type B",
                },
                new Model_DunnagePart
                {
                    DunnagePartId = 100,
                    DunnagePartName = "TEST-Bin",
                    DunnageTypeId = 1,
                    DunnageTypeName = "TEST-Type A",
                },
                new Model_DunnagePart
                {
                    DunnagePartId = 200,
                    DunnagePartName = "TEST-Pallet",
                    DunnageTypeId = 1,
                    DunnageTypeName = "TEST-Type A",
                },
            ]));

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.CurrentAssignments.Should().ContainSingle();
        viewModel.EnabledDunnageTypes.Should().HaveCount(2);
        viewModel.SelectedTypeFilter.Should().NotBeNull();
        viewModel.SelectedTypeFilter!.DunnageTypeId.Should().Be(1);
        viewModel.DunnageCatalog.Should().HaveCount(2);
        viewModel.DunnageCatalog.Select(item => item.DunnagePartId).Should().Equal(100, 200);
        viewModel.SelectedDunnagePart.Should().NotBeNull();
        viewModel.StatusMessage.Should().Be("Select a dunnage item, then add it to the assignment.");
    }

    [Fact]
    public async Task AddSelectedDunnageAsync_ShouldSetError_WhenAuthenticatedUserIsMissing()
    {
        var viewModel = CreateViewModel
        (); viewModel.WorkOrderId = "TEST-WO100";
        viewModel.SequenceNo = 10;
        viewModel.SelectedDunnagePart = new Model_DunnagePart
        {
            DunnagePartId = 100,
            DunnagePartName = "TEST-Bin",
            DunnageTypeId = 1,
            DunnageTypeName = "TEST-Type A",
        };

        await viewModel.AddSelectedDunnageCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("The authenticated user could not be resolved for this save.");
        _mockSetupTechService.Verify(service => service.UpsertDunnageAssignmentAsync(It.IsAny<Model_SetupTech_DunnageAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAndContinueAsync_ShouldPersistJobAndRaiseConfirmation_WhenPendingJobExists()
    {
        var viewModel = CreateViewModel();
        var navigationRaised = false;
        var assignment = new Model_SetupTech_DunnageAssignment
        {
            AssignmentId = 12,
            WorkOrderId = "TEST-WO100",
            SequenceNo = 10,
            DunnagePartId = 100,
            DunnagePartName = "TEST-Bin",
            DunnageTypeId = 1,
            DunnageTypeName = "TEST-Type A",
        };

        _currentWorkflowState.PendingActiveJob = new Model_SetupTech_ActiveJob
        {
            WorkcenterId = "TEST-WC01",
            WorkOrderId = "TEST-WO100",
            SequenceNo = 10,
            PartId = "TEST-PART-01",
        };
        viewModel.CurrentAssignments = [assignment];
        viewModel.NavigateToConfirmationRequested += (_, _) => navigationRaised = true;

        _mockSetupTechService
            .Setup(service => service.SetActiveJobAsync(It.IsAny<Model_SetupTech_ActiveJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result.Success());

        await viewModel.SaveAndContinueCommand.ExecuteAsync(null);

        navigationRaised.Should().BeTrue();
        _currentWorkflowState.PendingActiveJob!.DunnageAssignments.Should().ContainSingle();
        _currentWorkflowState.DunnageAssignments.Should().ContainSingle();
        viewModel.StatusMessage.Should().Be("Setup saved successfully.");
        _mockSetupTechService.Verify(
            service => service.SetActiveJobAsync(
                It.Is<Model_SetupTech_ActiveJob>(job => job.DunnageAssignments.Count == 1 && job.DunnageAssignments[0].DunnagePartName == "TEST-Bin"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private ViewModel_Waitlist_SetupTechDunnage CreateViewModel()
    {
        return new ViewModel_Waitlist_SetupTechDunnage(_mockSetupTechService.Object, _mockWorkflowState.Object);
    }
}