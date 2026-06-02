-- =============================================================
-- MTM Waitlist Application — WorkOrderDunnageAssignments Indexes
-- Domain: SetupTech
-- Description: Supports dunnage assignment lookup and modification queries.
-- Depends on: schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL: 5.7 compatible
-- =============================================================
USE `mtm_waitlist`;

-- 1. Temporarily disable foreign key checks
SET FOREIGN_KEY_CHECKS = 0;

-- 2. Create the temporary helper procedure
DELIMITER $$

CREATE PROCEDURE SafeDropAndCreateDunnageIndex(
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
        -- CLONE WORKAROUND (Prevents Error 1553 for Foreign Keys)
        -- Create a temporary duplicate index so the FK constraint never breaks
        SET @clone_sql = CONCAT('CREATE INDEX `temp_clone_', p_index, '` ON `', p_table, '` (`', p_column, '`)');
        PREPARE stmt FROM @clone_sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;

        -- Now safely drop the original index
        SET @drop_sql = CONCAT('DROP INDEX `', p_index, '` ON `', p_table, '`');
        PREPARE stmt FROM @drop_sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;

    -- Re-create the optimized original index
    SET @create_sql = CONCAT('CREATE INDEX `', p_index, '` ON `', p_table, '` (`', p_column, '`)');
    PREPARE stmt FROM @create_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;

    -- Clean up and drop the temporary clone index
    IF EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.STATISTICS 
        WHERE TABLE_SCHEMA = DATABASE() 
          AND TABLE_NAME = p_table 
          AND INDEX_NAME = CONCAT('temp_clone_', p_index)
    ) THEN
        SET @drop_clone_sql = CONCAT('DROP INDEX `temp_clone_', p_index, '` ON `', p_table, '`');
        PREPARE stmt FROM @drop_clone_sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$

DELIMITER ;

-- 3. Execute the procedure for the WorkOrderDunnageAssignments indexes
CALL SafeDropAndCreateDunnageIndex('WorkOrderDunnageAssignments', 'idx_WorkOrderDunnageAssignments_LastModifiedByUserId', 'LastModifiedByUserId');
CALL SafeDropAndCreateDunnageIndex('WorkOrderDunnageAssignments', 'idx_WorkOrderDunnageAssignments_DunnageTypeId', 'DunnageTypeId');

-- 4. Clean up the procedure from the database
DROP PROCEDURE IF EXISTS SafeDropAndCreateDunnageIndex;

-- 5. Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;