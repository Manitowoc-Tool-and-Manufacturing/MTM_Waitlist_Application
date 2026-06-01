namespace MTM_Waitlist_Server.Core.Models.Migration;

/// <summary>
/// Identifies the exact type of table mismatch detected during deterministic validation.
/// </summary>
public enum TableMismatchKind
{
    TableMissing,
    ColumnMissing,
    ColumnUnexpected,
    ColumnTypeMismatch,
    ColumnNullabilityMismatch,
    ColumnDefaultMismatch,
    ColumnExtraMismatch,
    PrimaryKeyMismatch,
    UniqueConstraintMissing,
    UniqueConstraintUnexpected,
    ForeignKeyMissing,
    ForeignKeyUnexpected,
    ForeignKeyReferencedTableMismatch,
    ForeignKeyReferencedColumnsMismatch,
    ForeignKeyDeleteRuleMismatch,
    ForeignKeyUpdateRuleMismatch,
    ParseError,
    MetadataReadError,
    RestoreIncompatible,
}