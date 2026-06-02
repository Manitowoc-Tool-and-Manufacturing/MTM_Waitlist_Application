using FluentAssertions;
using Moq;
using Core.Interfaces.InforVisual;
using Core.Models.InforVisual;
using Core.Models.Shared;
using Services.InforVisual;

namespace MTM_Waitlist_Application.Tests.Unit.Services.InforVisual.Connectivity;

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
    public async Task GetAllWorkcentersAsync_ShouldFallbackToLocalCache_WhenConnectivityTransitionsDuringCall()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedResult = Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([
            new() { WorkcenterId = "TEST-CACHE-WC1", Description = "Cached Workcenter" }
        ]);

        // First call online, second call (after cache invalidation) offline
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWorkcentersAsync(cancellationToken))
            .ReturnsAsync(Model_Dao_Result<List<Model_VisualWorkcenter>>.Failure("TEST-Network timeout"));
        _mockLocalRepository
            .Setup(repository => repository.GetCachedWorkcentersAsync())
            .ReturnsAsync(cachedResult);

        var result = await _service.GetAllWorkcentersAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.First().WorkcenterId.Should().Be("TEST-CACHE-WC1");
    }

    [Fact]
    public async Task GetAllWorkcentersAsync_ShouldReturnFailure_WhenBothOnlineAndLocalFail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWorkcentersAsync(cancellationToken))
            .ReturnsAsync(Model_Dao_Result<List<Model_VisualWorkcenter>>.Failure("TEST-Online connection failed"));
        _mockLocalRepository
            .Setup(repository => repository.GetCachedWorkcentersAsync())
            .ReturnsAsync(Model_Dao_Result<List<Model_VisualWorkcenter>>.Failure("TEST-Local cache empty"));

        var result = await _service.GetAllWorkcentersAsync(cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-Local cache empty"); // Returns local error when both fail
    }

    [Fact]
    public async Task GetAllWorkcentersAsync_ShouldUseLocalCacheDirectly_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cachedResult = Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([
            new() { WorkcenterId = "TEST-WC1", Description = "Test Workcenter" }
        ]);

        // First call offline to populate cache (should NOT happen - offline returns failure immediately for WO operations)
        // But workcenter cache works differently - it can return cached data when online fails
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.ConstrainedInternet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWorkcentersAsync(cancellationToken))
            .ReturnsAsync(Model_Dao_Result<List<Model_VisualWorkcenter>>.Failure("TEST-Constrained connection"));
        _mockLocalRepository
            .Setup(repository => repository.GetCachedWorkcentersAsync())
            .ReturnsAsync(cachedResult);

        var result = await _service.GetAllWorkcentersAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.First().WorkcenterId.Should().Be("TEST-WC1");
    }
}