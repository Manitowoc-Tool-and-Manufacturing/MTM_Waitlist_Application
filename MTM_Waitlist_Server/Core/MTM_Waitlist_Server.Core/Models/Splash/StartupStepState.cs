namespace MTM_Waitlist_Server.Core.Models.Splash;

/// <summary>Represents the execution state of a single startup step.</summary>
public enum StartupStepState
{
    /// <summary>Step has not yet begun.</summary>
    Pending,

    /// <summary>Step is currently executing.</summary>
    InProgress,

    /// <summary>Step completed successfully.</summary>
    Succeeded,

    /// <summary>Step encountered an error.</summary>
    Failed,

    /// <summary>Step was intentionally bypassed (e.g. not applicable to this startup path).</summary>
    Skipped,
}
