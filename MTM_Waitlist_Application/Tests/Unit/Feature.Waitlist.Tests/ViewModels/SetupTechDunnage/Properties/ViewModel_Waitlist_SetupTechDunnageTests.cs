using Core.Interfaces.SetupTech;
using Core.Models.InforVisual;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Feature.Waitlist.ViewModels.SetupTechDunnage;
using FluentAssertions;
using Moq;

namespace Feature.Waitlist.Tests.ViewModels.SetupTechDunnage.Properties;

public class ViewModel_Waitlist_SetupTechDunnageTests
{
    private readonly Mock<IService_SetupTech> _mockSetupTechService;
    private readonly Mock<IService_SetupTechWorkflowState> _mockWorkflowState;
    private readonly Model_SetupTech_WorkflowState _workflowState;

    public ViewModel_Waitlist_SetupTechDunnageTests()
    {
        _mockSetupTechService = new Mock<IService_SetupTech>();
        _mockWorkflowState = new Mock<IService_SetupTechWorkflowState>();
        _workflowState = new Model_SetupTech_WorkflowState
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

        _mockWorkflowState
            .SetupGet(service => service.Current)
            .Returns(_workflowState);
    }

    [Fact]
    public async Task InitializeAsync_ShouldPopulateIdentifiersAndDefaultSelection_WhenWorkflowStateIsComplete()
    {
        var viewModel = CreateViewModel();
        SetupSuccessfulLoads();

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.WorkOrderId.Should().Be("TEST-WO100");
        viewModel.SequenceNo.Should().Be(10);
        viewModel.SelectedTypeFilter.Should().NotBeNull();
        viewModel.SelectedTypeFilter!.DunnageTypeId.Should().Be(1);
        viewModel.SelectedDunnagePart.Should().NotBeNull();
        viewModel.SelectedDunnagePart!.DunnagePartId.Should().Be(100);
    }

    [Fact]
    public async Task SelectedTypeFilter_ShouldFilterCatalog_WhenFilterChanges()
    {
        var viewModel = CreateViewModel();
        SetupSuccessfulLoads();

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.SelectedTypeFilter = viewModel.EnabledDunnageTypes.Single(type => type.DunnageTypeId == 2);

        viewModel.DunnageCatalog.Should().ContainSingle();
        viewModel.DunnageCatalog[0].DunnagePartId.Should().Be(300);
        viewModel.SelectedDunnagePart.Should().NotBeNull();
        viewModel.SelectedDunnagePart!.DunnagePartId.Should().Be(300);
    }

    private void SetupSuccessfulLoads()
    {
        _mockSetupTechService
            .Setup(service => service.GetDunnageAssignmentAsync("TEST-WO100", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Success([]));
        _mockSetupTechService
            .Setup(service => service.GetEnabledDunnageTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Success([
                new Model_SetupTech_DunnageTypeConfig
                {
                    DunnageTypeId = 1,
                    DunnageTypeName = "TEST-Type A",
                    IsEnabled = true,
                    DisplayOrder = 1,
                },
                new Model_SetupTech_DunnageTypeConfig
                {
                    DunnageTypeId = 2,
                    DunnageTypeName = "TEST-Type B",
                    IsEnabled = true,
                    DisplayOrder = 2,
                },
            ]));
        _mockSetupTechService
            .Setup(service => service.GetDunnageCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<List<Model_DunnagePart>>.Success([
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
                new Model_DunnagePart
                {
                    DunnagePartId = 300,
                    DunnagePartName = "TEST-Crate",
                    DunnageTypeId = 2,
                    DunnageTypeName = "TEST-Type B",
                },
            ]));
    }

    private ViewModel_Waitlist_SetupTechDunnage CreateViewModel()
    {
        return new ViewModel_Waitlist_SetupTechDunnage(_mockSetupTechService.Object, _mockWorkflowState.Object);
    }
}