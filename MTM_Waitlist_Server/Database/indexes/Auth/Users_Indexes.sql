USE `mtm_waitlist`;

-- 1. Create a temporary helper procedure
DELIMITER $$

CREATE PROCEDURE SafeDropAndCreateIndexUsers(
    IN p_table VARCHAR(255),
    IN p_index VARCHAR(255),
    IN p_column VARCHAR(255)
)
BEGIN
    -- Check if the index already exists on the table
    IF EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.STATISTICS 
        WHERE TABLE_SCHEMA = DATABASE() 
          AND TABLE_NAME = p_table 
          AND INDEX_NAME = p_index
    ) THEN
        -- Safely drop the index if it exists
        SET @drop_sql = CONCAT('DROP INDEX `', p_index, '` ON `', p_table, '`');
        PREPARE stmt FROM @drop_sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;

    -- Dynamically construct and execute the CREATE statement
    SET @create_sql = CONCAT('CREATE INDEX `', p_index, '` ON `', p_table, '` (`', p_column, '`)');
    PREPARE stmt FROM @create_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END$$

DELIMITER ;

-- 2. Execute the procedure for the Users indexes
CALL SafeDropAndCreateIndexUsers('Users', 'idx_Users_IsActive', 'IsActive');
CALL SafeDropAndCreateIndexUsers('Users', 'idx_Users_Role', 'Role');

-- 3. Clean up the procedure from the database
DROP PROCEDURE IF EXISTS SafeDropAndCreateIndexUsers;
