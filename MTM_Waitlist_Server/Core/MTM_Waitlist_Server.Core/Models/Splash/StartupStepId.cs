namespace MTM_Waitlist_Server.Core.Models.Splash;

/// <summary>Identifies each distinct phase of the admin application startup sequence.</summary>
public enum StartupStepId
{
    /// <summary>Building the DI service container.</summary>
    BuildingContainer,

    /// <summary>Loading server-settings.json from disk.</summary>
    LoadingSettings,

    /// <summary>Computing the neverConfigured sentinel from settings.</summary>
    ComputingSentinel,

    /// <summary>Running the three-step MySQL probe (TCP → schema → admin user).</summary>
    ProbingMySQL,

    /// <summary>Evaluating the startup decision tree to determine the launch path.</summary>
    EvaluatingBranch,

    /// <summary>Checking whether the current Windows identity is authorised in the database.</summary>
    CheckingWindowsAuth,

    /// <summary>Starting the in-process Kestrel REST API host.</summary>
    StartingApiHost,

    /// <summary>Starting the nightly backup scheduler service.</summary>
    StartingScheduler,

    /// <summary>All startup work completed — admin shell is about to open.</summary>
    Complete,
}
