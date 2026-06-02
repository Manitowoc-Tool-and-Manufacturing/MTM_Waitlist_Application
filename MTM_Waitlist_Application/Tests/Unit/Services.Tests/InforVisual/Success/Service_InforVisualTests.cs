using FluentAssertions;
using Moq;
using Core.Interfaces.InforVisual;
using Core.Models.InforVisual;
using Core.Models.Shared;
using Services.InforVisual;

namespace MTM_Waitlist_Application.Tests.Unit.Services.InforVisual.Success;

public class Service_InforVisualTests
{
    private readonly Mock<IConnectivity> _mockConnectivity;
    private readonly Mock<IRepository_InforVisual> _mockOnlineRepository;
    private readonly Mock<IRepository_InforVisualLocal> _mockLocalRepository;
    private readonly Service_InforVisual _service;

    public Service_InforVisualTests()
    {
        _mockConnectivity = new Mock<IConnectivity>();
        _mockOnlineRepository = new Mock<IRepository_InforVisual>();
        _mockLocalRepository = new Mock<IRepository_InforVisualLocal>();
        _service = new Service_InforVisual(
            _mockConnectivity.Object,
            _mockOnlineRepository.Object,
            _mockLocalRepository.Object);
    }

    [Fact]
    public async Task GetAllWorkcentersAsync_ShouldReturnFromCache_WhenCacheIsPopulated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedWorkcenters = new List<Model_VisualWorkcenter>
        {
            new() { WorkcenterId = "TEST-WC1", Description = "Test Workcenter 1" },
            new() { WorkcenterId = "TEST-WC2", Description = "Test Workcenter 2" }
        };

        var onlineResult = Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([.. cachedWorkcenters]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWorkcentersAsync(cancellationToken))
            .ReturnsAsync(onlineResult);
        _mockLocalRepository
            .Setup(repository => repository.SaveWorkcentersAsync(It.IsAny<List<Model_VisualWorkcenter>>()))
            .ReturnsAsync(Model_Dao_Result.Success());

        // First call to populate cache
        await _service.GetAllWorkcentersAsync(cancellationToken);

        // Second call should return from cache
        var result = await _service.GetAllWorkcentersAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllWorkcentersAsync_ShouldCallOnlineRepository_WhenCacheIsNotPopulated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([
            new() { WorkcenterId = "TEST-WC1", Description = "Test Workcenter 1" }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWorkcentersAsync(cancellationToken))
            .ReturnsAsync(expected);
        _mockLocalRepository
            .Setup(repository => repository.SaveWorkcentersAsync(It.IsAny<List<Model_VisualWorkcenter>>()))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.GetAllWorkcentersAsync(cancellationToken);

        result.Should().BeEquivalentTo(expected);
        _mockOnlineRepository.Verify(repository => repository.GetAllWorkcentersAsync(cancellationToken), Times.Once);
        _mockLocalRepository.Verify(repository => repository.SaveWorkcentersAsync(It.IsAny<List<Model_VisualWorkcenter>>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateWorkcenterCache_ShouldClearCache()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var workcenters = Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([
            new() { WorkcenterId = "TEST-WC1", Description = "Test Workcenter 1" }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWorkcentersAsync(cancellationToken))
            .ReturnsAsync(workcenters);
        _mockLocalRepository
            .Setup(repository => repository.SaveWorkcentersAsync(It.IsAny<List<Model_VisualWorkcenter>>()))
            .ReturnsAsync(Model_Dao_Result.Success());

        // Populate cache
        await _service.GetAllWorkcentersAsync(cancellationToken);

        // Invalidate cache
        _service.InvalidateWorkcenterCache();

        // Reset to allow another call
        _mockLocalRepository.Reset();
        _mockLocalRepository
            .Setup(repository => repository.SaveWorkcentersAsync(It.IsAny<List<Model_VisualWorkcenter>>()))
            .ReturnsAsync(Model_Dao_Result.Success());

        // Should call online again after invalidation
        await _service.GetAllWorkcentersAsync(cancellationToken);

        _mockOnlineRepository.Verify(repository => repository.GetAllWorkcentersAsync(cancellationToken), Times.Exactly(2));
    }

    [Fact]
    public async Task GetWorkOrderHeaderAsync_ShouldReturnSuccess_WhenOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<Model_VisualWorkOrderHeader>.Success(
            new Model_VisualWorkOrderHeader { WorkOrderId = "TEST-WO1" });
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetWorkOrderHeaderAsync("TEST-WO1", cancellationToken))
            .ReturnsAsync(expected);

        var result = await _service.GetWorkOrderHeaderAsync("TEST-WO1", cancellationToken);

        result.Should().BeEquivalentTo(expected);
        _mockOnlineRepository.Verify(repository => repository.GetWorkOrderHeaderAsync("TEST-WO1", cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetWorkOrderSequencesAsync_ShouldReturnSuccess_WhenOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_VisualWorkOrderSequence>>.Success([
            new() { WorkOrderId = "TEST-WO1", SequenceNo = 10 }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetWorkOrderSequencesAsync("TEST-WO1", cancellationToken))
            .ReturnsAsync(expected);

        var result = await _service.GetWorkOrderSequencesAsync("TEST-WO1", cancellationToken);

        result.Should().BeEquivalentTo(expected);
        _mockOnlineRepository.Verify(repository => repository.GetWorkOrderSequencesAsync("TEST-WO1", cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSubordinatePartsAsync_ShouldReturnSuccess_WhenOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_Visual_SubordinatePart>>.Success([
            new() { WorkOrderId = "TEST-WO1", SequenceNo = 10, PartId = "TEST-PART1" }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetSubordinatePartsAsync("TEST-WO1", 10, cancellationToken))
            .ReturnsAsync(expected);

        var result = await _service.GetSubordinatePartsAsync("TEST-WO1", 10, cancellationToken);

        result.Should().BeEquivalentTo(expected);
        _mockOnlineRepository.Verify(repository => repository.GetSubordinatePartsAsync("TEST-WO1", 10, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetActiveWorkOrdersForResourceAsync_ShouldReturnSuccess_WhenOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_VisualWorkOrderHeader>>.Success([
            new() { WorkOrderId = "TEST-WO1", PartId = "TEST-PART1" }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetActiveWorkOrdersForResourceAsync("TEST-WC1", cancellationToken))
            .ReturnsAsync(expected);

        var result = await _service.GetActiveWorkOrdersForResourceAsync("TEST-WC1", cancellationToken);

        result.Should().BeEquivalentTo(expected);
        _mockOnlineRepository.Verify(repository => repository.GetActiveWorkOrdersForResourceAsync("TEST-WC1", cancellationToken), Times.Once);
    }
}