namespace MTM_Waitlist_Server.Core.Models.Dashboard;

/// <summary>Table row/size statistics read from information_schema.</summary>
public record Model_TableStat(
    string TableName,
    long EstimatedRows,
    long DataBytes,
    DateTime? LastUpdated)
{
    /// <summary>Human-readable data size (KB or MB).</summary>
    public string DataSizeDisplay =>
        DataBytes >= 1_048_576
            ? $"{DataBytes / 1_048_576.0:F1} MB"
            : $"{DataBytes / 1_024.0:F0} KB";
}
