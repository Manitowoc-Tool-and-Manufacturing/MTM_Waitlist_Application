using FluentAssertions;
using Moq;
using MTM_Waitlist_Application.Core.Interfaces.Waitlist;
using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Core.Models.Waitlist;
using MTM_Waitlist_Application.Data.Mock;

namespace MTM_Waitlist_Application.Tests.Unit.Data.Mock.WaitlistSeeds;

public class MockDataSeederTests
{
    private readonly Mock<IRepository_WaitlistEntryLocal> _mockLocalRepository;

    public MockDataSeederTests()
    {
        _mockLocalRepository = new Mock<IRepository_WaitlistEntryLocal>();
    }

    [Fact]
    public async Task SeedIfEmptyAsync_ShouldInsertFiveEntries_WhenLocalStoreIsEmpty()
    {
        var insertedIds = new List<int>();
        _mockLocalRepository
            .Setup(repo => repo.GetAllWaitlistEntriesAsync())
            .ReturnsAsync(Model_Dao_Result<List<Model_WaitlistEntry>>.Success([]));
        _mockLocalRepository
            .Setup(repo => repo.InsertWaitlistEntryAsync(It.IsAny<Model_WaitlistEntry>()))
            .Callback<Model_WaitlistEntry>(entry => insertedIds.Add(entry.Id))
            .ReturnsAsync(Model_Dao_Result.Success());

        await MockDataSeeder.SeedIfEmptyAsync(_mockLocalRepository.Object);

        insertedIds.Should().Equal(9001, 9002, 9003, 9004, 9005);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_ShouldNotInsertEntries_WhenLocalStoreAlreadyHasData()
    {
        _mockLocalRepository
            .Setup(repo => repo.GetAllWaitlistEntriesAsync())
            .ReturnsAsync(Model_Dao_Result<List<Model_WaitlistEntry>>.Success([new Model_WaitlistEntry { Id = 1 }]));

        await MockDataSeeder.SeedIfEmptyAsync(_mockLocalRepository.Object);

        _mockLocalRepository.Verify(repo => repo.InsertWaitlistEntryAsync(It.IsAny<Model_WaitlistEntry>()), Times.Never);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_ShouldNotInsertEntries_WhenReadFails()
    {
        _mockLocalRepository
            .Setup(repo => repo.GetAllWaitlistEntriesAsync())
            .ReturnsAsync(Model_Dao_Result<List<Model_WaitlistEntry>>.Failure("TEST-read-failure"));

        await MockDataSeeder.SeedIfEmptyAsync(_mockLocalRepository.Object);

        _mockLocalRepository.Verify(repo => repo.InsertWaitlistEntryAsync(It.IsAny<Model_WaitlistEntry>()), Times.Never);
    }
}