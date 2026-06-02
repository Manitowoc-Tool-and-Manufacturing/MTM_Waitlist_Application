using FluentAssertions;
using Moq;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Services.SetupTech;

namespace MTM_Waitlist_Application.Tests.Unit.Services.SetupTech.Failure;

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
    public async Task SetActiveJobAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var activeJob = new Model_SetupTech_ActiveJob { WorkcenterId = "TEST-WC1" };
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.SetActiveJobAsync(activeJob, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Active jobs cannot be changed while offline.");
        _mockActiveJobRepository.Verify(repository => repository.SetActiveJobAsync(It.IsAny<Model_SetupTech_ActiveJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpsertDunnageAssignmentAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var assignment = new Model_SetupTech_DunnageAssignment { WorkOrderId = "TEST-WO1" };
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.UpsertDunnageAssignmentAsync(assignment, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Dunnage assignments cannot be changed while offline.");
        _mockDunnageRepository.Verify(repository => repository.UpsertDunnageAssignmentAsync(It.IsAny<Model_SetupTech_DunnageAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDunnageAssignmentAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.DeleteDunnageAssignmentAsync(42, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Dunnage assignments cannot be changed while offline.");
        _mockDunnageRepository.Verify(repository => repository.DeleteDunnageAssignmentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDunnageCatalogAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.GetDunnageCatalogAsync(cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("The dunnage catalog is not available while offline.");
        _mockDunnageCatalogRepository.Verify(repository => repository.GetDunnageCatalogAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}