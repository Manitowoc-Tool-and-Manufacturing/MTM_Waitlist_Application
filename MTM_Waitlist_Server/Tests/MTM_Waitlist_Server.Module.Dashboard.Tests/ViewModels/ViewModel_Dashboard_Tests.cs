using FluentAssertions;
using Moq;
using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Models.Dashboard;
using MTM_Waitlist_Server.Module.Dashboard.ViewModels;

namespace MTM_Waitlist_Server.Module.Dashboard.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="ViewModel_Dashboard"/>.
/// All MySQL calls are replaced by mocked <see cref="IService_Dashboard"/>.
/// </summary>
public class ViewModel_Dashboard_Tests
{
    private static Mock<IService_Dashboard> BuildFullMock(
        int active = 3,
        int max = 100,
        long sizeBytes = 14_891_008L,
        int total = 47,
        int open = 2,
        int overdue = 1)
    {
        var mock = new Mock<IService_Dashboard>();

        mock.Setup(s => s.GetConnectionCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((active, max));

        mock.Setup(s => s.GetDatabaseSizeBytesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizeBytes);

        mock.Setup(s => s.GetTableStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Model_TableStat>
            {
                new("WaitlistEntries", 47, 32_768, DateTime.UtcNow),
                new("Users", 12, 8_192, DateTime.UtcNow)
            });

        mock.Setup(s => s.GetActiveConnectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Model_ActiveConnection>
            {
                new(42L, "waitlist", "10.0.1.15", "Sleep", 12, null)
            });

        mock.Setup(s => s.GetWaitlistCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((total, open, overdue));

        return mock;
    }

    // ── Initial state ──────────────────────────────────────────────────────────

    [Fact]
    public void ViewModel_InitialState_IsNotRefreshing()
    {
        var vm = new ViewModel_Dashboard(new Mock<IService_Dashboard>().Object);
        vm.IsRefreshing.Should().BeFalse();
    }

    [Fact]
    public void ViewModel_InitialState_CollectionsAreEmpty()
    {
        var vm = new ViewModel_Dashboard(new Mock<IService_Dashboard>().Object);
        vm.TableStats.Should().BeEmpty();
        vm.ActiveConnections.Should().BeEmpty();
        vm.RecentActivity.Should().BeEmpty();
    }

    // ── RefreshAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_PopulatesConnectionCounts()
    {
        var vm = new ViewModel_Dashboard(BuildFullMock(active: 3, max: 100).Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.ActiveConnectionCount.Should().Be(3);
        vm.MaxConnections.Should().Be(100);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesDbSizeMb()
    {
        // 14_891_008 bytes ÷ 1_048_576 = ~14.2 MB
        var vm = new ViewModel_Dashboard(BuildFullMock(sizeBytes: 14_891_008L).Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.DbSizeMb.Should().StartWith("14.");
        vm.DbSizeMb.Should().EndWith("MB");
    }

    [Fact]
    public async Task RefreshAsync_PopulatesTableStats()
    {
        var vm = new ViewModel_Dashboard(BuildFullMock().Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.TableStats.Should().HaveCount(2);
        vm.TableStats[0].TableName.Should().Be("WaitlistEntries");
    }

    [Fact]
    public async Task RefreshAsync_PopulatesActiveConnections()
    {
        var vm = new ViewModel_Dashboard(BuildFullMock().Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.ActiveConnections.Should().HaveCount(1);
        vm.ActiveConnections[0].ThreadId.Should().Be(42);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesWaitlistCounts()
    {
        var vm = new ViewModel_Dashboard(BuildFullMock(total: 47, open: 2, overdue: 1).Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.TotalRequestsToday.Should().Be(47);
        vm.OpenRequestCount.Should().Be(2);
        vm.OverdueRequestCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_SetsLastRefreshedAt_ToNow()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var vm = new ViewModel_Dashboard(BuildFullMock().Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.LastRefreshedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task RefreshAsync_IsRefreshing_IsFalseAfterCompletion()
    {
        var vm = new ViewModel_Dashboard(BuildFullMock().Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.IsRefreshing.Should().BeFalse();
    }

    // ── KillConnectionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task KillConnectionAsync_CallsServiceKill_WithCorrectThreadId()
    {
        var mock = BuildFullMock();
        mock.Setup(s => s.KillConnectionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var vm = new ViewModel_Dashboard(mock.Object);
        var connection = new Model_ActiveConnection(42L, "waitlist", "10.0.1.15", "Sleep", 12, null);

        await vm.KillConnectionCommand.ExecuteAsync(connection);

        mock.Verify(s => s.KillConnectionAsync(42L, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Guard: no concurrent refresh ──────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_SecondCall_WhileRefreshing_IsIgnored()
    {
        var callCount = 0;
        var tcs = new TaskCompletionSource<(int, int)>();

        var mock = new Mock<IService_Dashboard>();
        mock.Setup(s => s.GetConnectionCountsAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken _) =>
            {
                callCount++;
                return await tcs.Task;
            });
        mock.Setup(s => s.GetDatabaseSizeBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
        mock.Setup(s => s.GetTableStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Model_TableStat>());
        mock.Setup(s => s.GetActiveConnectionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Model_ActiveConnection>());
        mock.Setup(s => s.GetWaitlistCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((0, 0, 0));

        var vm = new ViewModel_Dashboard(mock.Object);

        // Start first refresh (will block at GetConnectionCountsAsync).
        var first = vm.RefreshCommand.ExecuteAsync(null);

        // Attempt second refresh immediately — should be a no-op.
        await vm.RefreshCommand.ExecuteAsync(null);

        // Unblock the first.
        tcs.SetResult((3, 100));
        await first;

        callCount.Should().Be(1);
    }
}
