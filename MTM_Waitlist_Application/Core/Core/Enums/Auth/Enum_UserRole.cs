namespace Core.Enums.Auth;

/// <summary>
/// Application roles that control which actions and views are available to a user.
/// Maps to the <c>Role</c> ENUM column in the <c>Users</c> MySQL table.
/// </summary>
public enum Enum_UserRole
{
    /// <summary>Operates a press. Can view and submit waitlist requests.</summary>
    PressOperation,

    /// <summary>Sets up presses and tooling. Can manage die and coil requests.</summary>
    SetupTech,

    /// <summary>Supervises production floor operations. Can manage and assign requests.</summary>
    ProductionSupervisor,

    /// <summary>Manages production department. Full access to waitlist and reporting.</summary>
    ProductionManager,

    /// <summary>Quality assurance inspector. Read access to waitlist entries.</summary>
    Quality,

    /// <summary>Receiving department staff. Manages incoming goods requests.</summary>
    Receiving,

    /// <summary>Handles material logistics on the floor. Assigned to fulfill requests.</summary>
    MaterialHandler,

    /// <summary>System administrator. Full access to all features including user management.</summary>
    Admin,

    /// <summary>Application developer. Full access for development and diagnostics.</summary>
    Developer,
}
