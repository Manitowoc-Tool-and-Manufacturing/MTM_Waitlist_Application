SET NOCOUNT ON;

SELECT
    fk.name AS FK_Name,
    parent_table.name AS [Table],
    parent_column.name AS [Column],
    referenced_table.name AS Referenced_Table,
    referenced_column.name AS Referenced_Column
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc
    ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.tables AS parent_table
    ON parent_table.object_id = fk.parent_object_id
INNER JOIN sys.columns AS parent_column
    ON parent_column.object_id = fkc.parent_object_id
   AND parent_column.column_id = fkc.parent_column_id
INNER JOIN sys.tables AS referenced_table
    ON referenced_table.object_id = fk.referenced_object_id
INNER JOIN sys.columns AS referenced_column
    ON referenced_column.object_id = fkc.referenced_object_id
   AND referenced_column.column_id = fkc.referenced_column_id
ORDER BY
    fk.name,
    fkc.constraint_column_id;