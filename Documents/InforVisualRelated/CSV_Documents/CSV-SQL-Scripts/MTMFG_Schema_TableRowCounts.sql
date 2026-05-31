SET NOCOUNT ON;

CREATE TABLE #RowCounts
(
    TABLE_NAME SYSNAME NOT NULL,
    ROW_COUNT BIGINT NULL
);

DECLARE @tableSchema SYSNAME;
DECLARE @tableName SYSNAME;
DECLARE @rowCount BIGINT;
DECLARE @sql NVARCHAR(MAX);

DECLARE table_cursor CURSOR FAST_FORWARD FOR
SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;

OPEN table_cursor;

FETCH NEXT FROM table_cursor INTO @tableSchema, @tableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @rowCount = NULL;
    SET @sql = N'SELECT @RowCountOut = COUNT_BIG(*) FROM '
        + QUOTENAME(@tableSchema)
        + N'.'
        + QUOTENAME(@tableName)
        + N';';

    BEGIN TRY
        EXEC sys.sp_executesql
            @sql,
            N'@RowCountOut BIGINT OUTPUT',
            @RowCountOut = @rowCount OUTPUT;
    END TRY
    BEGIN CATCH
        SET @rowCount = NULL;
    END CATCH;

    INSERT INTO #RowCounts (TABLE_NAME, ROW_COUNT)
    VALUES (@tableName, @rowCount);

    FETCH NEXT FROM table_cursor INTO @tableSchema, @tableName;
END;

CLOSE table_cursor;
DEALLOCATE table_cursor;

SELECT
    TABLE_NAME,
    ROW_COUNT
FROM #RowCounts
ORDER BY
    ROW_COUNT DESC,
    TABLE_NAME;

DROP TABLE #RowCounts;