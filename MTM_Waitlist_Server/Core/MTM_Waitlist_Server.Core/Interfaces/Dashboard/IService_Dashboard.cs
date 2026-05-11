using MTM_Waitlist_Server.Core.Models.Dashboard;

namespace MTM_Waitlist_Server.Core.Interfaces.Dashboard;

/// <summary>Provides MySQL status data for the admin dashboard.</summary>
public interface IService_Dashboard
{
    /// <summary>Returns the total size of the mtm_waitlist database in bytes.</summary>
    Task<long> GetDatabaseSizeBytesAsync(CancellationToken ct = default);

    /// <summary>Returns the number of currently open MySQL connections and the server maximum.</summary>
    Task<(int Active, int Max)> GetConnectionCountsAsync(CancellationToken ct = default);

    /// <summary>Returns table-level row/size statistics from information_schema.</summary>
    Task<IReadOnlyList<Model_TableStat>> GetTableStatsAsync(CancellationToken ct = default);

    /// <summary>Returns exact row counts by running COUNT(*) on each table.</summary>
    Task<IReadOnlyList<Model_TableStat>> GetExactTableStatsAsync(CancellationToken ct = default);

    /// <summary>Returns the live process list from SHOW FULL PROCESSLIST.</summary>
    Task<IReadOnlyList<Model_ActiveConnection>> GetActiveConnectionsAsync(CancellationToken ct = default);

    /// <summary>Issues a MySQL KILL for the given thread.</summary>
    Task KillConnectionAsync(long threadId, CancellationToken ct = default);

    /// <summary>Returns today's total request count and open/overdue counts from WaitlistEntries.</summary>
    Task<(int Total, int Open, int Overdue)> GetWaitlistCountsAsync(CancellationToken ct = default);

    /// <summary>Returns a snapshot of the most recent in-process API activity log entries.</summary>
    IReadOnlyList<LogEntry> GetRecentActivity();
}
