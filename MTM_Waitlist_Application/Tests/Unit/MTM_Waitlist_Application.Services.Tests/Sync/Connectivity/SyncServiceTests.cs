using FluentAssertions;
using Moq;
using MTM_Waitlist_Application.Core.Interfaces.Api;
using MTM_Waitlist_Application.Core.Interfaces.Waitlist;
using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Core.Models.Waitlist;
using MTM_Waitlist_Application.Services.Sync;

namespace MTM_Waitlist_Application.Tests.Unit.Services.Sync.Connectivity;

public class SyncServiceTests
{
    private readonly Mock<IConnectivity> _mockConnectivity;
    private readonly Mock<IRepository_WaitlistEntryLocal> _mockLocalRepository;
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly SyncService _service;

    public SyncServiceTests()
    {
        _mockConnectivity = new Mock<IConnectivity>();
        _mockLocalRepository = new Mock<IRepository_WaitlistEntryLocal>();
        _mockApiClient = new Mock<IApiClient>();
        _service = new SyncService(
            _mockConnectivity.Object,
            _mockLocalRepository.Object,
            _mockApiClient.Object);
    }

    [Fact]
    public async Task FlushOfflineQueueAsync_ShouldReturnFailure_WhenPendingWritesCannotBeLoaded()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockLocalRepository
            .Setup(repository => repository.GetPendingWritesAsync())
            .ReturnsAsync(Model_Dao_Result<List<(int QueueId, Model_WaitlistEntry Entry, string Operation)>>.Failure("TEST-queue-failure"));

        var result = await _service.FlushOfflineQueueAsync(cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-queue-failure");
    }

    [Fact]
    public async Task FlushOfflineQueueAsync_ShouldClearSuccessfulWrites_WhenQueueIsProcessed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var pendingWrites = new List<(int QueueId, Model_WaitlistEntry Entry, string Operation)>
        {
            (1, new Model_WaitlistEntry { Id = 101 }, "INSERT"),
            (2, new Model_WaitlistEntry { Id = 102 }, "UPDATE"),
            (3, new Model_WaitlistEntry { Id = 103 }, "DELETE")
        };

        _mockLocalRepository
            .Setup(repository => repository.GetPendingWritesAsync())
            .ReturnsAsync(Model_Dao_Result<List<(int QueueId, Model_WaitlistEntry Entry, string Operation)>>.Success(pendingWrites));
        _mockApiClient
            .Setup(client => client.PostAsync<Model_WaitlistEntry>(It.IsAny<string>(), It.IsAny<object>(), cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Success(new Model_WaitlistEntry()));
        _mockApiClient
            .Setup(client => client.PutAsync<Model_WaitlistEntry>(It.IsAny<string>(), It.IsAny<object>(), cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Success(new Model_WaitlistEntry()));
        _mockApiClient
            .Setup(client => client.DeleteAsync(It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(Model_Dao_Result.Success());
        _mockLocalRepository
            .Setup(repository => repository.ClearPendingWriteAsync(It.IsAny<int>()))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.FlushOfflineQueueAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockLocalRepository.Verify(repository => repository.ClearPendingWriteAsync(1), Times.Once);
        _mockLocalRepository.Verify(repository => repository.ClearPendingWriteAsync(2), Times.Once);
        _mockLocalRepository.Verify(repository => repository.ClearPendingWriteAsync(3), Times.Once);
    }
}