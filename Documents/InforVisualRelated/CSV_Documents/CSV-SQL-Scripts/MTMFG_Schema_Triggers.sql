SET NOCOUNT ON;

SELECT
    TABLE_NAME = table_obj.name,
    TRIGGER_NAME = trigger_obj.name,
    TYPE_DESC = trigger_obj.type_desc,
    IS_INSTEAD_OF_TRIGGER = CASE WHEN trigger_obj.is_instead_of_trigger = 1 THEN 'YES' ELSE 'NO' END,
    IS_DISABLED = CASE WHEN trigger_obj.is_disabled = 1 THEN 'YES' ELSE 'NO' END,
    TRIGGER_EVENT = trigger_event.type_desc
FROM sys.triggers AS trigger_obj
INNER JOIN sys.tables AS table_obj
    ON table_obj.object_id = trigger_obj.parent_id
LEFT JOIN sys.trigger_events AS trigger_event
    ON trigger_event.object_id = trigger_obj.object_id
ORDER BY
    table_obj.name,
    trigger_obj.name,
    trigger_event.type_desc;