namespace MTM_Waitlist_Server.Core.Models.Dashboard;

/// <summary>In-process ring-buffer log entry for a single HTTP request/response.</summary>
public record LogEntry(
    DateTime TimestampUtc,
    string Method,
    string Path,
    int StatusCode,
    long DurationMs);
