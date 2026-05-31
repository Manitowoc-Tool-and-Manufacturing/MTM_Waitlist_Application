SET NOCOUNT ON;

SELECT
    TABLE_NAME = table_obj.name,
    COLUMN_NAME = column_obj.name,
    CONSTRAINT_NAME = default_obj.name
FROM sys.default_constraints AS default_obj
INNER JOIN sys.tables AS table_obj
    ON table_obj.object_id = default_obj.parent_object_id
INNER JOIN sys.columns AS column_obj
    ON column_obj.object_id = default_obj.parent_object_id
   AND column_obj.column_id = default_obj.parent_column_id
ORDER BY
    table_obj.name,
    column_obj.column_id;