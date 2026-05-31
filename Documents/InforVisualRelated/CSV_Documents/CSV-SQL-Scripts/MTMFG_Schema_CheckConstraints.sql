SET NOCOUNT ON;

SELECT
    TABLE_NAME = table_obj.name,
    CONSTRAINT_NAME = check_obj.name
FROM sys.check_constraints AS check_obj
INNER JOIN sys.tables AS table_obj
    ON table_obj.object_id = check_obj.parent_object_id
ORDER BY
    table_obj.name,
    check_obj.name;