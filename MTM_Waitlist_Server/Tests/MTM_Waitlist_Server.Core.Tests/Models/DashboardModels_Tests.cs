using FluentAssertions;
using MTM_Waitlist_Server.Core.Models.Dashboard;

namespace MTM_Waitlist_Server.Core.Tests.Models;

/// <summary>
/// Verifies the Core dashboard record types defined for DATABASE-02.
/// </summary>
public class DashboardModels_Tests
{
    [Fact]
    public void Model_TableStat_Properties_AreAccessible()
    {
        var ts = new Model_TableStat("WaitlistEntries", 47L, 32_768L, new DateTime(2025, 1, 1, 10, 14, 22));

        ts.TableName.Should().Be("WaitlistEntries");
        ts.EstimatedRows.Should().Be(47);
        ts.DataBytes.Should().Be(32_768);
        ts.LastUpdated.Should().NotBeNull();
    }

    [Fact]
    public void Model_TableStat_NullLastUpdated_IsAllowed()
    {
        var ts = new Model_TableStat("Users", 12L, 8_192L, null);
        ts.LastUpdated.Should().BeNull();
    }

    [Fact]
    public void Model_ActiveConnection_Properties_AreAccessible()
    {
        var conn = new Model_ActiveConnection(42L, "waitlist", "10.0.1.15", "Sleep", 12, null);

        conn.ThreadId.Should().Be(42);
        conn.User.Should().Be("waitlist");
        conn.Host.Should().Be("10.0.1.15");
        conn.Command.Should().Be("Sleep");
        conn.TimeSeconds.Should().Be(12);
        conn.State.Should().BeNull();
    }

    [Fact]
    public void LogEntry_Properties_AreAccessible()
    {
        var ts = new DateTime(2025, 1, 1, 10, 14, 23, DateTimeKind.Utc);
        var entry = new LogEntry(ts, "POST", "/api/waitlist", 201, 45L);

        entry.TimestampUtc.Should().Be(ts);
        entry.Method.Should().Be("POST");
        entry.Path.Should().Be("/api/waitlist");
        entry.StatusCode.Should().Be(201);
        entry.DurationMs.Should().Be(45);
    }

    [Fact]
    public void LogEntry_Equality_BasedOnValues()
    {
        var ts = DateTime.UtcNow;
        var a = new LogEntry(ts, "GET", "/api/waitlist", 200, 10L);
        var b = new LogEntry(ts, "GET", "/api/waitlist", 200, 10L);

        a.Should().Be(b);
    }
}
