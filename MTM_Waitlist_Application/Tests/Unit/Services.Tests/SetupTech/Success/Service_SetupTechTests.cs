using FluentAssertions;
using Moq;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Services.SetupTech;

namespace MTM_Waitlist_Application.Tests.Unit.Services.SetupTech.Success;

public class Service_SetupTechTests
{
    private readonly Mock<IConnectivity> _mockConnectivity;
    private readonly Mock<IRepository_SetupTechActiveJob> _mockActiveJobRepository;
    private readonly Mock<IRepository_SetupTechActiveJobLocal> _mockActiveJobLocalRepository;
    private readonly Mock<IRepository_DunnageCatalog> _mockDunnageCatalogRepository;
    private readonly Mock<IRepository_WorkOrderDunnage> _mockDunnageRepository;
    private readonly Mock<IRepository_WorkOrderDunnageLocal> _mockDunnageLocalRepository;
    private readonly Mock<IRepository_SetupTechDunnageTypeConfig> _mockDunnageTypeRepository;
    private readonly Mock<IRepository_SetupTechDunnageTypeConfigLocal> _mockDunnageTypeLocalRepository;
    private readonly Service_SetupTech _service;

    public Service_SetupTechTests()
    {
        _mockConnectivity = new Mock<IConnectivity>();
        _mockActiveJobRepository = new Mock<IRepository_SetupTechActiveJob>();
        _mockActiveJobLocalRepository = new Mock<IRepository_SetupTechActiveJobLocal>();
        _mockDunnageCatalogRepository = new Mock<IRepository_DunnageCatalog>();
        _mockDunnageRepository = new Mock<IRepository_WorkOrderDunnage>();
        _mockDunnageLocalRepository = new Mock<IRepository_WorkOrderDunnageLocal>();
        _mockDunnageTypeRepository = new Mock<IRepository_SetupTechDunnageTypeConfig>();
        _mockDunnageTypeLocalRepository = new Mock<IRepository_SetupTechDunnageTypeConfigLocal>();
        _service = new Service_SetupTech(
            _mockConnectivity.Object,
            _mockActiveJobRepository.Object,
            _mockActiveJobLocalRepository.Object,
            _mockDunnageCatalogRepository.Object,
            _mockDunnageRepository.Object,
            _mockDunnageLocalRepository.Object,
            _mockDunnageTypeRepository.Object,
            _mockDunnageTypeLocalRepository.Object);
    }

    [Fact]
    public async Task GetActiveJobAsync_ShouldReturnOnlineResult_WhenInternetIsAvailableAndOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<Model_SetupTech_ActiveJob>.Success(
            new Model_SetupTech_ActiveJob { WorkcenterId = "TEST-WC1", WorkOrderId = "TEST-WO1" });
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockActiveJobRepository
            .Setup(repository => repository.GetActiveJobAsync("TEST-WC1", cancellationToken))
            .ReturnsAsync(expected);

        var result = await _service.GetActiveJobAsync("TEST-WC1", cancellationToken);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetDunnageAssignmentAsync_ShouldReturnOnlineResult_WhenInternetIsAvailableAndOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Success([
            new() { AssignmentId = 1, WorkOrderId = "TEST-WO1", SequenceNo = 10, DunnagePartId = 1 }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockDunnageRepository
            .Setup(repository => repository.GetDunnageAssignmentAsync("TEST-WO1", 10, cancellationToken))
            .ReturnsAsync(expected);
        _mockDunnageLocalRepository
            .Setup(repository => repository.GetDunnageAssignmentAsync("TEST-WO1", 10))
            .ReturnsAsync(Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Failure("TEST-No local data"));

        var result = await _service.GetDunnageAssignmentAsync("TEST-WO1", 10, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().WorkOrderId.Should().Be("TEST-WO1");
    }

    [Fact]
    public async Task UpsertDunnageAssignmentAsync_ShouldCallOnlineRepository_WhenInternetIsAvailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var assignment = new Model_SetupTech_DunnageAssignment
        {
            WorkOrderId = "TEST-WO1",
            SequenceNo = 10,
            DunnagePartId = 1
        };
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockDunnageRepository
            .Setup(repository => repository.UpsertDunnageAssignmentAsync(assignment, cancellationToken))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.UpsertDunnageAssignmentAsync(assignment, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockDunnageRepository.Verify(repository => repository.UpsertDunnageAssignmentAsync(assignment, cancellationToken), Times.Once);
        _mockDunnageLocalRepository.Verify(repository => repository.UpsertDunnageAssignmentAsync(assignment), Times.Once);
    }

    [Fact]
    public async Task DeleteDunnageAssignmentAsync_ShouldCallOnlineRepository_WhenInternetIsAvailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockDunnageRepository
            .Setup(repository => repository.DeleteDunnageAssignmentAsync(42, cancellationToken))
            .ReturnsAsync(Model_Dao_Result.Success());
        _mockDunnageLocalRepository
            .Setup(repository => repository.DeleteDunnageAssignmentAsync(42))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.DeleteDunnageAssignmentAsync(42, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockDunnageRepository.Verify(repository => repository.DeleteDunnageAssignmentAsync(42, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetEnabledDunnageTypesAsync_ShouldSyncToLocalCache_WhenOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Success([
            new() { DunnageTypeId = 1, DunnageTypeName = "TEST-Type1", IsEnabled = true },
            new() { DunnageTypeId = 2, DunnageTypeName = "TEST-Type2", IsEnabled = true }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockDunnageTypeRepository
            .Setup(repository => repository.GetEnabledDunnageTypesAsync(cancellationToken))
            .ReturnsAsync(expected);
        _mockDunnageTypeLocalRepository
            .Setup(repository => repository.SaveDunnageTypesAsync(It.IsAny<List<Model_SetupTech_DunnageTypeConfig>>()))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.GetEnabledDunnageTypesAsync(cancellationToken);

        result.Should().BeEquivalentTo(expected);
        _mockDunnageTypeLocalRepository.Verify(repository => repository.SaveDunnageTypesAsync(It.IsAny<List<Model_SetupTech_DunnageTypeConfig>>()), Times.Once);
    }

    [Fact]
    public async Task GetJobHistoryAsync_ShouldReturnOnlineResult_WhenInternetIsAvailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>.Success([
            new() { WorkcenterId = "TEST-WC1", WorkOrderId = "TEST-WO1" }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockActiveJobRepository
            .Setup(repository => repository.GetJobHistoryAsync("TEST-WC1", cancellationToken))
            .ReturnsAsync(expected);

        var result = await _service.GetJobHistoryAsync("TEST-WC1", cancellationToken);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SetActiveJobAsync_ShouldCallOnlineRepository_WhenInternetIsAvailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var activeJob = new Model_SetupTech_ActiveJob
        {
            WorkcenterId = "TEST-WC1",
            WorkOrderId = "TEST-WO1",
            SequenceNo = 10
        };
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockActiveJobRepository
            .Setup(repository => repository.SetActiveJobAsync(activeJob, cancellationToken))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.SetActiveJobAsync(activeJob, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockActiveJobRepository.Verify(repository => repository.SetActiveJobAsync(activeJob, cancellationToken), Times.Once);
        _mockActiveJobLocalRepository.Verify(repository => repository.SetActiveJobAsync(activeJob), Times.Once);
    }
}