using FluentAssertions;
using Moq;
using MTM_Waitlist_Application.Core.Interfaces.Waitlist;
using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Core.Models.Waitlist;
using MTM_Waitlist_Application.Services.Waitlist;

namespace MTM_Waitlist_Application.Tests.Unit.Services.Waitlist.Success;

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
    public async Task GetAllEntriesAsync_ShouldReturnOnlineResult_WhenInternetIsAvailableAndOnlineCallSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = Model_Dao_Result<List<Model_WaitlistEntry>>.Success([new Model_WaitlistEntry { Id = 1 }]);
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.GetAllWaitlistEntriesAsync(cancellationToken))
            .ReturnsAsync(expected);

        var result = await _service.GetAllEntriesAsync(cancellationToken);

        result.Should().BeEquivalentTo(expected);
        _mockLocalRepository.Verify(repository => repository.GetAllWaitlistEntriesAsync(), Times.Never);
    }

    [Fact]
    public async Task SaveEntryAsync_ShouldMirrorToLocalCache_WhenOnlineInsertSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = new Model_WaitlistEntry { Id = 44 };
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.InsertWaitlistEntryAsync(entry, cancellationToken))
            .ReturnsAsync(Model_Dao_Result.Success());
        _mockLocalRepository
            .Setup(repository => repository.InsertWaitlistEntryAsync(entry))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.SaveEntryAsync(entry, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockLocalRepository.Verify(repository => repository.InsertWaitlistEntryAsync(entry), Times.Once);
        _mockLocalRepository.Verify(repository => repository.EnqueuePendingWriteAsync(It.IsAny<Model_WaitlistEntry>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEntryAsync_ShouldUpdateLocalCache_WhenOnlineUpdateSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = new Model_WaitlistEntry { Id = 45 };
        _mockConnectivity.Setup(connectivity => connectivity.NetworkAccess).Returns(NetworkAccess.Internet);
        _mockOnlineRepository
            .Setup(repository => repository.UpdateWaitlistEntryAsync(entry, cancellationToken))
            .ReturnsAsync(Model_Dao_Result.Success());
        _mockLocalRepository
            .Setup(repository => repository.UpdateWaitlistEntryAsync(entry))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _service.UpdateEntryAsync(entry, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        _mockLocalRepository.Verify(repository => repository.UpdateWaitlistEntryAsync(entry), Times.Once);
    }
}