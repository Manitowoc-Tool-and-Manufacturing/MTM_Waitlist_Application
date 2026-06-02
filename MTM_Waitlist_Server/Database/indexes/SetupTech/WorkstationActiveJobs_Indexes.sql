-- =============================================================
-- MTM Waitlist Application — WorkstationActiveJobs Indexes
-- Domain: SetupTech
-- Description: Supports workstation current-job and audit queries.
-- Depends on: schema/tables/SetupTech/WorkstationActiveJobs.sql
-- MySQL: 5.7 compatible
-- =============================================================
USE `mtm_waitlist`;

-- 1. Temporarily disable foreign key checks
SET FOREIGN_KEY_CHECKS = 0;

-- 2. Create a temporary helper procedure that handles columns safely
DELIMITER $$

CREATE PROCEDURE SafeDropAndCreateActiveJobsIndex(
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
        -- CLONE WORKAROUND (Prevents Error 1553 for Foreign Keys)
        -- Create a temporary duplicate index so the FK constraint never breaks
        SET @clone_sql = CONCAT('CREATE INDEX `temp_clone_', p_index, '` ON `', p_table, '` (', p_columns, ')');
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
    SET @create_sql = CONCAT('CREATE INDEX `', p_index, '` ON `', p_table, '` (', p_columns, ')');
    PREPARE stmt FROM @create_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;

    -- Clean up and drop the temporary clone index if it was built
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

-- 3. Execute the procedure for both single and composite indexes
CALL SafeDropAndCreateActiveJobsIndex('WorkstationActiveJobs', 'idx_WorkstationActiveJobs_SetupTechUserId', '`SetupTechUserId`');
CALL SafeDropAndCreateActiveJobsIndex('WorkstationActiveJobs', 'idx_WorkstationActiveJobs_WorkOrder_Sequence', '`WorkOrderId`, `SequenceNo`');

-- 4. Clean up the procedure from the database
DROP PROCEDURE IF EXISTS SafeDropAndCreateActiveJobsIndex;

-- 5. Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1;
