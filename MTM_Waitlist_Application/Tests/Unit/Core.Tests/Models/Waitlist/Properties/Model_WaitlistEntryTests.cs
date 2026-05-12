using FluentAssertions;
using Core.Enums.Waitlist;
using Core.Models.Waitlist;

namespace MTM_Waitlist_Application.Tests.Unit.Core.Models.Waitlist.Properties;

public class Model_WaitlistEntryTests
{
    [Fact]
    public void Constructor_ShouldDefaultWorkcenterNameToEmptyString_WhenCreated()
    {
        var entry = new Model_WaitlistEntry();

        entry.WorkcenterName.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldDefaultStatusToWaiting_WhenCreated()
    {
        var entry = new Model_WaitlistEntry();

        entry.Status.Should().Be(Enum_WaitlistStatus.Waiting);
    }

    [Fact]
    public void Constructor_ShouldDefaultPriorityToFive_WhenCreated()
    {
        var entry = new Model_WaitlistEntry();

        entry.Priority.Should().Be(5);
    }

    [Fact]
    public void Constructor_ShouldDefaultNullableFieldsToNull_WhenCreated()
    {
        var entry = new Model_WaitlistEntry();

        entry.Notes.Should().BeNull();
        entry.ScheduledAt.Should().BeNull();
        entry.CompletedAt.Should().BeNull();
        entry.AssignedToUserId.Should().BeNull();
        entry.CreatedByUserId.Should().BeNull();
        entry.UpdatedByUserId.Should().BeNull();
    }
}