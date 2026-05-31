SET NOCOUNT ON;

SELECT
    TABLE_NAME = table_obj.name,
    INDEX_NAME = index_obj.name,
    INDEX_TYPE = index_obj.type_desc,
    IS_UNIQUE = CASE WHEN index_obj.is_unique = 1 THEN 'YES' ELSE 'NO' END,
    IS_PRIMARY_KEY = CASE WHEN index_obj.is_primary_key = 1 THEN 'YES' ELSE 'NO' END,
    COLUMN_NAME = column_obj.name,
    KEY_ORDINAL = index_column.key_ordinal,
    IS_INCLUDED_COLUMN = CASE WHEN index_column.is_included_column = 1 THEN 'YES' ELSE 'NO' END
FROM sys.indexes AS index_obj
INNER JOIN sys.tables AS table_obj
    ON table_obj.object_id = index_obj.object_id
INNER JOIN sys.index_columns AS index_column
    ON index_column.object_id = index_obj.object_id
   AND index_column.index_id = index_obj.index_id
INNER JOIN sys.columns AS column_obj
    ON column_obj.object_id = index_column.object_id
   AND column_obj.column_id = index_column.column_id
WHERE index_obj.name IS NOT NULL
ORDER BY
    table_obj.name,
    index_obj.name,
    index_column.key_ordinal,
    index_column.index_column_id;