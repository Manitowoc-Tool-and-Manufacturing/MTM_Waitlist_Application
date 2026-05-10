namespace MTM_Waitlist_Application.Core.Enums.Waitlist;

/// <summary>
/// The type of logistics or material-handling request submitted by a workcenter.
/// Maps to the <c>RequestType</c> ENUM column in the <c>WaitlistEntries</c> MySQL table.
/// </summary>
public enum Enum_WaitlistRequestType
{
    /// <summary>A coil needs to be delivered to the workcenter.</summary>
    Coil,

    /// <summary>Dunnage needs to be delivered to the workcenter.</summary>
    Dunnage,

    /// <summary>Finished goods need to be picked up from the workcenter.</summary>
    PickUpFinishedGoods,

    /// <summary>Unused goods need to be picked up from the workcenter.</summary>
    PickUpUnusedGoods,

    /// <summary>Dunnage needs to be picked up from the workcenter.</summary>
    PickUpDunnage,

    /// <summary>Parts need to be brought to the press.</summary>
    BringPartsToPress,

    /// <summary>A coil needs to be removed from the press.</summary>
    RemoveCoilFromPress,

    /// <summary>A die needs to be brought to or picked up from the workcenter.</summary>
    BringPickUpDie,
}
