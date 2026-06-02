namespace Core.Models.SetupTech;

/// <summary>
/// Represents a single dunnage item associated with a work order and sequence.
/// </summary>
public sealed class Model_SetupTech_DunnageAssignment
{
    /// <summary>The server-side identifier for the dunnage assignment row.</summary>
    public int AssignmentId { get; set; }

    /// <summary>The work order identifier the dunnage assignment belongs to.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The sequence number the dunnage assignment belongs to.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The dunnage catalog part identifier from the source system.</summary>
    public int DunnagePartId { get; set; }

    /// <summary>The display name for the selected dunnage part.</summary>
    public string DunnagePartName { get; set; } = string.Empty;

    /// <summary>The dunnage type identifier mirrored from the receiving catalog.</summary>
    public int DunnageTypeId { get; set; }

    /// <summary>The display name for the dunnage type.</summary>
    public string DunnageTypeName { get; set; } = string.Empty;

    /// <summary>The user identifier that last modified the assignment row.</summary>
    public int LastModifiedByUserId { get; set; }
}