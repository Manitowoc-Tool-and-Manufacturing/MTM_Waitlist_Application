using FluentAssertions;
using Moq;
using Core.Interfaces.InforVisual;
using Core.Models.InforVisual;
using Core.Models.Shared;
using Services.InforVisual;

namespace MTM_Waitlist_Application.Tests.Unit.Services.InforVisual.Failure;

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
    public async Task GetAllWorkcentersAsync_ShouldFallbackToLocalCache_WhenOnlineCallFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedResult = Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([
            new() { WorkcenterId = "TEST-CACHE-WC1", Description = "Cached Workcenter" }
        ]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWorkcentersAsync(cancellationToken))
            .ReturnsAsync(Model_Dao_Result<List<Model_VisualWorkcenter>>.Failure("TEST-Online failure"));
        _mockLocalRepository
            .Setup(repository => repository.GetCachedWorkcentersAsync())
            .ReturnsAsync(cachedResult);

        var result = await _service.GetAllWorkcentersAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle().Which.WorkcenterId.Should().Be("TEST-CACHE-WC1");
    }

    [Fact]
    public async Task GetWorkOrderHeaderAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.GetWorkOrderHeaderAsync("TEST-WO1", cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Infor Visual is not available while offline.");
        _mockOnlineRepository.Verify(repository => repository.GetWorkOrderHeaderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetWorkOrderSequencesAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.GetWorkOrderSequencesAsync("TEST-WO1", cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Infor Visual is not available while offline.");
        _mockOnlineRepository.Verify(repository => repository.GetWorkOrderSequencesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSubordinatePartsAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.GetSubordinatePartsAsync("TEST-WO1", 10, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Infor Visual is not available while offline.");
        _mockOnlineRepository.Verify(repository => repository.GetSubordinatePartsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveWorkOrdersForResourceAsync_ShouldReturnFailure_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);

        var result = await _service.GetActiveWorkOrdersForResourceAsync("TEST-WC1", cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Infor Visual is not available while offline.");
        _mockOnlineRepository.Verify(repository => repository.GetActiveWorkOrdersForResourceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}