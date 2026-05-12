using Core.Interfaces.Auth;
using Core.Models.Auth;
using Core.Models.Shared;
using Feature.Auth.ViewModels.Login;
using FluentAssertions;
using Moq;

namespace Feature.Auth.Tests.ViewModels.Login.Properties;

public class ViewModel_Auth_LoginTests
{
    [Fact]
    public void Constructor_ShouldDefaultErrorMessageToEmpty_WhenCreated()
    {
        var viewModel = new ViewModel_Auth_Login(Mock.Of<IService_Auth>());

        viewModel.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_ShouldPreserveExistingUsername_WhenAlreadySet()
    {
        var viewModel = new ViewModel_Auth_Login(Mock.Of<IService_Auth>())
        {
            Username = "TEST-user"
        };

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.Username.Should().Be("TEST-user");
        viewModel.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetSharedWorkstationFalse_WhenCheckWorkstationReturnsPersonalMachine()
    {
        var mockAuthService = new Mock<IService_Auth>();
        var viewModel = new ViewModel_Auth_Login(mockAuthService.Object);

        mockAuthService
            .Setup(service => service.CheckWorkstationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<Model_Auth_LoginMode>.Success(new Model_Auth_LoginMode
            {
                IsSharedWorkstation = false,
                WindowsUsername = "TEST-user"
            }));
        mockAuthService
            .Setup(service => service.AutoLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<Model_AuthToken>.Failure("TEST-auto-login-failure"));

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.IsSharedWorkstation.Should().BeFalse();
        viewModel.IsCheckingWorkstation.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_ShouldSetValidationMessage_WhenUsernameIsMissing()
    {
        var viewModel = new ViewModel_Auth_Login(Mock.Of<IService_Auth>())
        {
            Password = "TEST-password"
        };

        await viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("Enter your Windows username.");
    }

    [Fact]
    public async Task LoginAsync_ShouldSetValidationMessage_WhenPasswordIsMissing()
    {
        var viewModel = new ViewModel_Auth_Login(Mock.Of<IService_Auth>())
        {
            Username = "TEST-user"
        };

        await viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("Enter your password or PIN.");
    }

    [Fact]
    public async Task LoginAsync_ShouldRaiseAuthenticatedAndClearPassword_WhenServiceReturnsSuccess()
    {
        var mockAuthService = new Mock<IService_Auth>();
        var viewModel = new ViewModel_Auth_Login(mockAuthService.Object)
        {
            Username = "TEST-user",
            Password = "TEST-password"
        };
        var wasAuthenticated = false;
        viewModel.Authenticated += (_, _) => wasAuthenticated = true;

        mockAuthService
            .Setup(service => service.LoginAsync("TEST-user", "TEST-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<Model_AuthToken>.Success(new Model_AuthToken { Token = "TEST-token" }));

        await viewModel.LoginCommand.ExecuteAsync(null);

        wasAuthenticated.Should().BeTrue();
        viewModel.Password.Should().BeEmpty();
        viewModel.ErrorMessage.Should().BeEmpty();
        viewModel.IsAuthenticating.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_ShouldSetErrorMessage_WhenServiceReturnsFailure()
    {
        var mockAuthService = new Mock<IService_Auth>();
        var viewModel = new ViewModel_Auth_Login(mockAuthService.Object)
        {
            Username = "TEST-user",
            Password = "TEST-password"
        };

        mockAuthService
            .Setup(service => service.LoginAsync("TEST-user", "TEST-password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Model_Dao_Result<Model_AuthToken>.Failure("TEST-login-failure"));

        await viewModel.LoginCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("TEST-login-failure");
        viewModel.IsAuthenticating.Should().BeFalse();
    }
}