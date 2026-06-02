-- =============================================================
-- MTM Waitlist Application — SetupTechDunnageTypeConfig Indexes
-- Domain: SetupTech
-- Description: Supports enabled-type UI queries ordered by DisplayOrder.
-- Depends on: schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL: 5.7 compatible
-- =============================================================
USE `mtm_waitlist`;

-- 1. Create a temporary helper procedure for composite indexes
DELIMITER $$

CREATE PROCEDURE SafeDropAndCreateCompositeIndex(
    IN p_table VARCHAR(255),
    IN p_index VARCHAR(255),
    IN p_columns VARCHAR(500)
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

    -- Dynamically construct and execute the CREATE statement with multiple columns
    SET @create_sql = CONCAT('CREATE INDEX `', p_index, '` ON `', p_table, '` (', p_columns, ')');
    PREPARE stmt FROM @create_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END$$

DELIMITER ;

-- 2. Execute the procedure for the SetupTechDunnageTypeConfig index
CALL SafeDropAndCreateCompositeIndex('SetupTechDunnageTypeConfig', 'idx_SetupTechDunnageTypeConfig_IsEnabled_DisplayOrder', '`IsEnabled`, `DisplayOrder`');

-- 3. Clean up the procedure from the database
DROP PROCEDURE IF EXISTS SafeDropAndCreateCompositeIndex;