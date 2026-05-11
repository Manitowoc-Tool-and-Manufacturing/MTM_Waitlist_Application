using FluentAssertions;
using MTM_Waitlist_Application.Core.Models.Shared;

namespace MTM_Waitlist_Application.Tests.Unit.Core.Models.Shared.Success;

public class Model_Dao_ResultTests
{
    [Fact]
    public void Success_Generic_ShouldSetSuccessAndData_WhenDataProvided()
    {
        var result = Model_Dao_Result<string>.Success("TEST-data");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("TEST-data");
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Failure_Generic_ShouldSetFailureAndMessage_WhenMessageProvided()
    {
        var result = Model_Dao_Result<string>.Failure("TEST-error");

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorMessage.Should().Be("TEST-error");
    }

    [Fact]
    public void Success_NonGeneric_ShouldSetSuccess_WhenCalled()
    {
        var result = Model_Dao_Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Failure_NonGeneric_ShouldSetFailureAndMessage_WhenMessageProvided()
    {
        var result = Model_Dao_Result.Failure("TEST-error");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("TEST-error");
    }
}