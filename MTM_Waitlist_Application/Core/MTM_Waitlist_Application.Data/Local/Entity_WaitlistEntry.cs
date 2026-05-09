using SQLite;

namespace MTM_Waitlist_Application.Data.Local;

/// <summary>
/// SQLite-mapped entity for caching a waitlist entry on-device.
/// Mirrors <see cref="Core.Models.Waitlist.Model_WaitlistEntry"/>
/// and will gain additional columns when the API schema is finalised.
/// </summary>
[Table("WaitlistEntries")]
internal sealed class Entity_WaitlistEntry
{
    [PrimaryKey]
    public int Id { get; set; }

    // Additional columns will be added once the API schema is finalised.
}
