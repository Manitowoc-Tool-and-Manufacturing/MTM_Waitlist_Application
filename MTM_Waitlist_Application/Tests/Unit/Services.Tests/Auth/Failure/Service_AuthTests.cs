using FluentAssertions;
using Moq;
using Core.Interfaces.Api;
using Core.Interfaces.Auth;
using Core.Models.Auth;
using Core.Models.Shared;
using Services.Auth;

namespace MTM_Waitlist_Application.Tests.Unit.Services.Auth.Failure;

public class Service_AuthTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Mock<IService_AuthTokenStore> _mockTokenStore;
    private readonly Service_Auth _service;

    public Service_AuthTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _mockTokenStore = new Mock<IService_AuthTokenStore>();
        _service = new Service_Auth(_mockApiClient.Object, _mockTokenStore.Object);
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

    [Fact]
    public async Task CheckWorkstationAsync_ShouldReturnFailure_WhenApiReturnsFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockApiClient
            .Setup(client => client.PostAsync<Model_Auth_LoginMode>("/api/auth/check-workstation", It.IsAny<object>(), cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_Auth_LoginMode>.Failure("TEST-workstation-failure"));

        var result = await _service.CheckWorkstationAsync("TEST-machine", cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-workstation-failure");
    }

    [Fact]
    public async Task AutoLoginAsync_ShouldReturnFailure_WhenApiReturnsFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _mockApiClient
            .Setup(client => client.PostAsync<Model_AuthToken>("/api/auth/auto-login", It.IsAny<object>(), cancellationToken))
            .ReturnsAsync(Model_Dao_Result<Model_AuthToken>.Failure("TEST-auto-login-failure"));

        var result = await _service.AutoLoginAsync("TEST-user", cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-auto-login-failure");
    }
}