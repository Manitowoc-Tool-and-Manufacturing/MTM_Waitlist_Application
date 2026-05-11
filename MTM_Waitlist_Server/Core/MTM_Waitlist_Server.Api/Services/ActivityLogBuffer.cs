using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Models.Dashboard;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MTM_Waitlist_Server.Api.Services;

/// <summary>
/// Thread-safe in-process ring buffer capped at <paramref name="capacity"/> entries.
/// The ASP.NET request-logging middleware appends entries; the Dashboard ViewModel reads them.
/// </summary>
public sealed class ActivityLogBuffer : IActivityLogBuffer
{
    private readonly int _capacity;
    private readonly ConcurrentQueue<LogEntry> _queue = new();

    /// <summary>Creates a buffer with the given maximum capacity (default 200).</summary>
    public ActivityLogBuffer(int capacity = 200)
    {
        _capacity = capacity;
    }

    /// <inheritdoc/>
    public void Append(LogEntry entry)
    {
        _queue.Enqueue(entry);

        // Evict oldest entries when over capacity.
        while (_queue.Count > _capacity)
        {
            _queue.TryDequeue(out _);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<LogEntry> GetSnapshot() => _queue.ToArray();
}
