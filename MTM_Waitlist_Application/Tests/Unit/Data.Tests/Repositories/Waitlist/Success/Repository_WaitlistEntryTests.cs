using FluentAssertions;
using Moq;
using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Models.Shared;
using Core.Models.Waitlist;
using Data.Repositories.Waitlist;

namespace MTM_Waitlist_Application.Tests.Unit.Data.Repositories.Waitlist.Success;

public class Repository_WaitlistEntryTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Repository_WaitlistEntry _repository;

    public Repository_WaitlistEntryTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _repository = new Repository_WaitlistEntry(_mockApiClient.Object);
    }

    [Fact]
    public async Task GetAllWaitlistEntriesAsync_ShouldCallApiClientWithWaitlistEndpoint_WhenRequested()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = new List<Model_WaitlistEntry> { new() { Id = 1 } };
        _mockApiClient
            .Setup(client => client.GetAsync<List<Model_WaitlistEntry>>(Constants_Api.WaitlistEntryEndpoint, cancellationToken))
            .ReturnsAsync(Model_Dao_Result<List<Model_WaitlistEntry>>.Success(expected));

        var result = await _repository.GetAllWaitlistEntriesAsync(cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetWaitlistEntryByIdAsync_ShouldCallApiClientWithIdEndpoint_WhenRequested()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = new Model_WaitlistEntry { Id = 42 };
        _mockApiClient
            .Setup(client => client.GetAsync<Model_WaitlistEntry>($"{Constants_Api.WaitlistEntryEndpoint}/42", cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Success(expected));

        var result = await _repository.GetWaitlistEntryByIdAsync(42, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task InsertWaitlistEntryAsync_ShouldReturnSuccess_WhenApiClientReturnsSuccess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = new Model_WaitlistEntry { Id = 99 };
        _mockApiClient
            .Setup(client => client.PostAsync<Model_WaitlistEntry>(Constants_Api.WaitlistEntryEndpoint, entry, cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Success(entry));

        var result = await _repository.InsertWaitlistEntryAsync(entry, cancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWaitlistEntryAsync_ShouldReturnSuccess_WhenApiClientReturnsSuccess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = new Model_WaitlistEntry { Id = 77 };
        _mockApiClient
            .Setup(client => client.PutAsync<Model_WaitlistEntry>($"{Constants_Api.WaitlistEntryEndpoint}/77", entry, cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Success(entry));

        var result = await _repository.UpdateWaitlistEntryAsync(entry, cancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteWaitlistEntryAsync_ShouldReturnSuccess_WhenApiClientReturnsSuccess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockApiClient
            .Setup(client => client.DeleteAsync($"{Constants_Api.WaitlistEntryEndpoint}/11", cancellationToken))
            .ReturnsAsync(Model_Dao_Result.Success());

        var result = await _repository.DeleteWaitlistEntryAsync(11, cancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}