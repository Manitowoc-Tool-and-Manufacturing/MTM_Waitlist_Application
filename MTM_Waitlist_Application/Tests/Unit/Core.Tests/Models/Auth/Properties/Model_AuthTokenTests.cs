using FluentAssertions;
using Core.Models.Auth;

namespace MTM_Waitlist_Application.Tests.Unit.Core.Models.Auth.Properties;

public class Model_AuthTokenTests
{
    [Fact]
    public void IsExpired_ShouldBeTrue_WhenExpiryIsInThePast()
    {
        var token = new Model_AuthToken
        {
            Token = "TEST-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldBeFalse_WhenExpiryIsInTheFuture()
    {
        var token = new Model_AuthToken
        {
            Token = "TEST-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1)
        };

        token.IsExpired.Should().BeFalse();
    }
}