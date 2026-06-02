using FluentAssertions;
using Moq;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Services.SetupTech;

namespace MTM_Waitlist_Application.Tests.Unit.Services.SetupTech.Connectivity;

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
    public async Task GetActiveJobAsync_ShouldReturnLocalCachedResult_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedResult = Model_Dao_Result<Model_SetupTech_ActiveJob>.Success(
            new Model_SetupTech_ActiveJob { WorkcenterId = "TEST-CACHED-WC1", WorkOrderId = "TEST-WO1" });
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);
        _mockActiveJobLocalRepository
            .Setup(repository => repository.GetActiveJobAsync("TEST-WC1"))
            .ReturnsAsync(cachedResult);

        var result = await _service.GetActiveJobAsync("TEST-WC1", cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.WorkcenterId.Should().Be("TEST-CACHED-WC1");
        _mockActiveJobRepository.Verify(repository => repository.GetActiveJobAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDunnageAssignmentAsync_ShouldReturnLocalCachedResult_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedResult = Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Success([
            new() { WorkOrderId = "TEST-WO1", SequenceNo = 10, DunnagePartId = 1 }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);
        _mockDunnageLocalRepository
            .Setup(repository => repository.GetDunnageAssignmentAsync("TEST-WO1", 10))
            .ReturnsAsync(cachedResult);

        var result = await _service.GetDunnageAssignmentAsync("TEST-WO1", 10, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.First().DunnagePartId.Should().Be(1);
        _mockDunnageRepository.Verify(repository => repository.GetDunnageAssignmentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetEnabledDunnageTypesAsync_ShouldReturnLocalCachedResult_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedResult = Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Success([
            new() { DunnageTypeId = 1, DunnageTypeName = "TEST-CACHED-TYPE", IsEnabled = true }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);
        _mockDunnageTypeLocalRepository
            .Setup(repository => repository.GetCachedDunnageTypesAsync())
            .ReturnsAsync(cachedResult);

        var result = await _service.GetEnabledDunnageTypesAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.First().DunnageTypeName.Should().Be("TEST-CACHED-TYPE");
        _mockDunnageTypeRepository.Verify(repository => repository.GetEnabledDunnageTypesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetJobHistoryAsync_ShouldReturnLocalCachedResult_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedResult = Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>.Success([
            new() { WorkcenterId = "TEST-WC1", WorkOrderId = "TEST-WO1" }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);
        _mockActiveJobLocalRepository
            .Setup(repository => repository.GetJobHistoryAsync("TEST-WC1"))
            .ReturnsAsync(cachedResult);

        var result = await _service.GetJobHistoryAsync("TEST-WC1", cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.First().WorkcenterId.Should().Be("TEST-WC1");
        _mockActiveJobRepository.Verify(repository => repository.GetJobHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}