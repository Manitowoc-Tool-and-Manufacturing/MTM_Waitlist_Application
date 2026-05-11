namespace MTM_Waitlist_Server.Core.Models.KillSwitch;

/// <summary>Scope of a kill-switch shutdown signal.</summary>
public enum ShutdownTarget
{
    All,
    ByMachine,
    ByUser
}
