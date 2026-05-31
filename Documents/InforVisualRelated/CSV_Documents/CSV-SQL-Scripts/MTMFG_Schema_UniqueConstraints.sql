SET NOCOUNT ON;

SELECT
    TABLE_NAME = table_obj.name,
    CONSTRAINT_NAME = index_obj.name,
    COLUMN_NAME = column_obj.name,
    KEY_ORDINAL = index_column.key_ordinal
FROM sys.indexes AS index_obj
INNER JOIN sys.tables AS table_obj
    ON table_obj.object_id = index_obj.object_id
INNER JOIN sys.index_columns AS index_column
    ON index_column.object_id = index_obj.object_id
   AND index_column.index_id = index_obj.index_id
INNER JOIN sys.columns AS column_obj
    ON column_obj.object_id = index_column.object_id
   AND column_obj.column_id = index_column.column_id
WHERE index_obj.is_unique = 1
  AND index_obj.is_primary_key = 0
  AND index_obj.name IS NOT NULL
ORDER BY
    table_obj.name,
    index_obj.name,
    index_column.key_ordinal,
    index_column.index_column_id;