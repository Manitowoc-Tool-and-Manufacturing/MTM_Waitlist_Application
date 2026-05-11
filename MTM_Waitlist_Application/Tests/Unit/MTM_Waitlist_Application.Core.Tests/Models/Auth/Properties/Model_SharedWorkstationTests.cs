using FluentAssertions;
using MTM_Waitlist_Application.Core.Models.Auth;

namespace MTM_Waitlist_Application.Tests.Unit.Core.Models.Auth.Properties;

public class Model_SharedWorkstationTests
{
    [Fact]
    public void Constructor_ShouldDefaultWindowsUsernameToEmptyString_WhenCreated()
    {
        var workstation = new Model_SharedWorkstation();

        workstation.WindowsUsername.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldDefaultIsActiveToTrue_WhenCreated()
    {
        var workstation = new Model_SharedWorkstation();

        workstation.IsActive.Should().BeTrue();
    }
}