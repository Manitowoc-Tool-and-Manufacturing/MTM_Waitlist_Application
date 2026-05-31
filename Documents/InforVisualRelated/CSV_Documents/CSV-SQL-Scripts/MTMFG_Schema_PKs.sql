SET NOCOUNT ON;

SELECT
    table_name = table_obj.name,
    PrimaryKeyColumn = column_obj.name
FROM sys.key_constraints AS kc
INNER JOIN sys.tables AS table_obj
    ON table_obj.object_id = kc.parent_object_id
INNER JOIN sys.index_columns AS ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
INNER JOIN sys.columns AS column_obj
    ON column_obj.object_id = ic.object_id
   AND column_obj.column_id = ic.column_id
WHERE kc.type = 'PK'
ORDER BY
    table_obj.name,
    ic.key_ordinal;