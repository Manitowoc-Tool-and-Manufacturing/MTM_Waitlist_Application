using FluentAssertions;
using Moq;
using MTM_Waitlist_Application.Core.Interfaces.Api;
using MTM_Waitlist_Application.Core.Models.Auth;
using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Services.Auth;

namespace MTM_Waitlist_Application.Tests.Unit.Services.Auth.Failure;

public class Service_AuthTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Service_Auth _service;

    public Service_AuthTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _service = new Service_Auth(_mockApiClient.Object);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenApiReturnsFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockApiClient
            .Setup(client => client.PostAsync<Model_AuthToken>("/api/auth/login", It.IsAny<object>(), cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_AuthToken>.Failure("TEST-login-failure"));

        var result = await _service.LoginAsync("TEST-user", "TEST-password", cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-login-failure");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFailure_WhenApiReturnsFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockApiClient
            .Setup(client => client.PostAsync<Model_AuthToken>("/api/auth/refresh", It.IsAny<object>(), cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_AuthToken>.Failure("TEST-refresh-failure"));

        var result = await _service.RefreshTokenAsync(cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-refresh-failure");
    }
}