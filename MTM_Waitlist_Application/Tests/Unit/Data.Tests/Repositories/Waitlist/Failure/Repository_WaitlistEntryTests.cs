using FluentAssertions;
using Moq;
using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Models.Shared;
using Core.Models.Waitlist;
using Data.Repositories.Waitlist;

namespace MTM_Waitlist_Application.Tests.Unit.Data.Repositories.Waitlist.Failure;

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
    public async Task InsertWaitlistEntryAsync_ShouldReturnFailure_WhenApiClientReturnsFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = new Model_WaitlistEntry { Id = 12 };
        _mockApiClient
            .Setup(client => client.PostAsync<Model_WaitlistEntry>(Constants_Api.WaitlistEntryEndpoint, entry, cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Failure("TEST-insert-failure"));

        var result = await _repository.InsertWaitlistEntryAsync(entry, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-insert-failure");
    }

    [Fact]
    public async Task UpdateWaitlistEntryAsync_ShouldReturnFailure_WhenApiClientReturnsFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = new Model_WaitlistEntry { Id = 13 };
        _mockApiClient
            .Setup(client => client.PutAsync<Model_WaitlistEntry>($"{Constants_Api.WaitlistEntryEndpoint}/13", entry, cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_WaitlistEntry>.Failure("TEST-update-failure"));

        var result = await _repository.UpdateWaitlistEntryAsync(entry, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-update-failure");
    }
}