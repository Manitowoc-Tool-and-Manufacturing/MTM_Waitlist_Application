using FluentAssertions;
using Moq;
using Core.Interfaces.Waitlist;
using Core.Models.Shared;
using Core.Models.Waitlist;
using Services.Waitlist;

namespace MTM_Waitlist_Application.Tests.Unit.Services.Waitlist.Connectivity;

public class Service_WaitlistEntryTests
{
    private readonly Mock<IConnectivity> _mockConnectivity;
    private readonly Mock<IRepository_WaitlistEntry> _mockOnlineRepository;
    private readonly Mock<IRepository_WaitlistEntryLocal> _mockLocalRepository;
    private readonly Service_WaitlistEntry _service;

    public Service_WaitlistEntryTests()
    {
        _mockConnectivity = new Mock<IConnectivity>();
        _mockOnlineRepository = new Mock<IRepository_WaitlistEntry>();
        _mockLocalRepository = new Mock<IRepository_WaitlistEntryLocal>();
        _service = new Service_WaitlistEntry(
            _mockConnectivity.Object,
            _mockOnlineRepository.Object,
            _mockLocalRepository.Object);
    }

    [Fact]
    public async Task GetAllEntriesAsync_ShouldUseLocalRepository_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var localResult = Model_Dao_Result<List<Model_WaitlistEntry>>.Success([new Model_WaitlistEntry { Id = 2 }]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);
        _mockLocalRepository
            .Setup(repository => repository.GetAllWaitlistEntriesAsync())
            .ReturnsAsync(localResult);

        var result = await _service.GetAllEntriesAsync(cancellationToken);

        result.Should().BeEquivalentTo(localResult);
        _mockOnlineRepository.Verify(repository => repository.GetAllWaitlistEntriesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetEntryByIdAsync_ShouldFallBackToLocalRepository_WhenOnlineCallFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var localResult = Model_Dao_Result<Model_WaitlistEntry>.Success(new Model_WaitlistEntry { Id = 12 });
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetWaitlistEntryByIdAsync(12, cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Failure("TEST-online-failure"));
        _mockLocalRepository
            .Setup(repository => repository.GetWaitlistEntryByIdAsync(12))
            .ReturnsAsync(localResult);

        var result = await _service.GetEntryByIdAsync(12, cancellationToken);

        result.Should().BeEquivalentTo(localResult);
    }

    [Fact]
    public async Task SaveEntryAsync_ShouldSaveLocallyAndQueueInsert_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = new Model_WaitlistEntry { Id = 51 };
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);
        _mockLocalRepository
            .Setup(repository => repository.InsertWaitlistEntryAsync(entry))
            .ReturnsAsync(Model_Dao_Result.Success());
        _mockLocalRepository
            .Setup(repository => repository.EnqueuePendingWriteAsync(entry, "INSERT"))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.SaveEntryAsync(entry, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockLocalRepository.Verify(repository => repository.EnqueuePendingWriteAsync(entry, "INSERT"), Times.Once);
    }

    [Fact]
    public async Task DeleteEntryAsync_ShouldDeleteLocallyAndQueuePlaceholderDelete_WhenOffline()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.None);
        _mockLocalRepository
            .Setup(repository => repository.DeleteWaitlistEntryAsync(52))
            .ReturnsAsync(Model_Dao_Result.Success());
        _mockLocalRepository
            .Setup(repository => repository.EnqueuePendingWriteAsync(
                It.Is<Model_WaitlistEntry>(entry => entry.Id == 52), "DELETE"))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.DeleteEntryAsync(52, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockLocalRepository.Verify(repository => repository.EnqueuePendingWriteAsync(
            It.Is<Model_WaitlistEntry>(entry => entry.Id == 52), "DELETE"), Times.Once);
    }
}