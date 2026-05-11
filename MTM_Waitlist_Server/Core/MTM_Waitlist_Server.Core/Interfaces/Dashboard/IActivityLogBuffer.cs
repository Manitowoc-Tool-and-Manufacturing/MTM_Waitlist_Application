using MTM_Waitlist_Server.Core.Models.Dashboard;
using System.Collections.Generic;

namespace MTM_Waitlist_Server.Core.Interfaces.Dashboard;

/// <summary>
/// In-process ring buffer for the last N HTTP request/response log entries.
/// Shared between the ASP.NET middleware (writes) and the Dashboard ViewModel (reads).
/// </summary>
public interface IActivityLogBuffer
{
    /// <summary>Appends a new entry, evicting the oldest when the buffer is full.</summary>
    void Append(LogEntry entry);

    /// <summary>Returns a snapshot of all buffered entries, oldest first.</summary>
    IReadOnlyList<LogEntry> GetSnapshot();
}
