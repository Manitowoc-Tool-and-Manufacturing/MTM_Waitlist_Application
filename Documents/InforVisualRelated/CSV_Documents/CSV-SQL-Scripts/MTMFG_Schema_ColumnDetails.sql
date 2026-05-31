SET NOCOUNT ON;

SELECT
    TABLE_NAME = table_obj.name,
    ORDINAL_POSITION = column_obj.column_id,
    COLUMN_NAME = column_obj.name,
    DATA_TYPE = type_obj.name,
    CHARACTER_MAXIMUM_LENGTH = column_obj.max_length,
    NUMERIC_PRECISION = column_obj.precision,
    NUMERIC_SCALE = column_obj.scale,
    IS_NULLABLE = CASE WHEN column_obj.is_nullable = 1 THEN 'YES' ELSE 'NO' END,
    IS_IDENTITY = CASE WHEN column_obj.is_identity = 1 THEN 'YES' ELSE 'NO' END,
    IS_COMPUTED = CASE WHEN column_obj.is_computed = 1 THEN 'YES' ELSE 'NO' END
FROM sys.tables AS table_obj
INNER JOIN sys.columns AS column_obj
    ON column_obj.object_id = table_obj.object_id
INNER JOIN sys.types AS type_obj
    ON type_obj.user_type_id = column_obj.user_type_id
WHERE type_obj.is_user_defined = 0
ORDER BY
    table_obj.name,
    column_obj.column_id;