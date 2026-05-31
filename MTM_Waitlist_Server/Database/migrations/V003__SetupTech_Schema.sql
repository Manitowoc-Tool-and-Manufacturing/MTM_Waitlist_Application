-- =============================================================
-- MTM Waitlist Application — V003__SetupTech_Schema
-- Version:     V003
-- Description: Adds the SetupTech server schema: active-job storage,
--              archived job history, cached dunnage assignments,
--              SetupTech dunnage type filtering, subordinate-part cache,
--              and the matching indexes, triggers, and procedures.
--
-- Usage:
--   mysql -h 172.16.1.104 -u <admin_user> -p < migrations/V003__SetupTech_Schema.sql
--
-- ROLLBACK: Drop only the SetupTech objects after confirming no production
--           data is required. This migration intentionally avoids any
--           destructive operation against existing Auth/Waitlist data.
-- =============================================================

USE `mtm_waitlist`;

-- ─────────────────────────────────────────────────────────────
-- SECTION 1 — Tables
-- ─────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS `WorkstationActiveJobs`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT,
    `WorkcenterId`    VARCHAR(100) NOT NULL,
    `WorkOrderId`     VARCHAR(50)  NOT NULL,
    `SequenceNo`      INT          NOT NULL,
    `PartId`          VARCHAR(50)  NOT NULL,
    `PartType`        VARCHAR(50)  NOT NULL,
    `SetupTechUserId` INT          NOT NULL,
    `ActiveSince`     DATETIME     NOT NULL,
    `CreatedAt`       DATETIME     NOT NULL,
    `UpdatedAt`       DATETIME     NOT NULL,
    CONSTRAINT `pk_WorkstationActiveJobs` PRIMARY KEY (`Id`),
    CONSTRAINT `uq_WorkstationActiveJobs_Workcenter` UNIQUE (`WorkcenterId`),
    CONSTRAINT `fk_WorkstationActiveJobs_User`
        FOREIGN KEY (`SetupTechUserId`) REFERENCES `Users` (`Id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `WorkstationJobHistory`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT,
    `WorkcenterId`    VARCHAR(100) NOT NULL,
    `WorkOrderId`     VARCHAR(50)  NOT NULL,
    `SequenceNo`      INT          NOT NULL,
    `PartId`          VARCHAR(50)  NOT NULL,
    `PartType`        VARCHAR(50)  NOT NULL,
    `SetupTechUserId` INT          NOT NULL,
    `ActiveFrom`      DATETIME     NOT NULL,
    `ActiveUntil`     DATETIME     NOT NULL,
    `CreatedAt`       DATETIME     NOT NULL,
    `UpdatedAt`       DATETIME     NOT NULL,
    CONSTRAINT `pk_WorkstationJobHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `fk_WorkstationJobHistory_User`
        FOREIGN KEY (`SetupTechUserId`) REFERENCES `Users` (`Id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `WorkOrderDunnageAssignments`
(
    `Id`                   INT          NOT NULL AUTO_INCREMENT,
    `WorkOrderId`          VARCHAR(50)  NOT NULL,
    `SequenceNo`           INT          NOT NULL,
    `DunnagePartId`        INT          NOT NULL,
    `DunnagePartName`      VARCHAR(200) NOT NULL,
    `DunnageTypeId`        INT          NOT NULL,
    `DunnageTypeName`      VARCHAR(100) NOT NULL,
    `LastModifiedByUserId` INT          NOT NULL,
    `CreatedAt`            DATETIME     NOT NULL,
    `UpdatedAt`            DATETIME     NOT NULL,
    CONSTRAINT `pk_WorkOrderDunnageAssignments` PRIMARY KEY (`Id`),
    CONSTRAINT `uq_WorkOrderDunnageAssignments_WO_Seq_Part`
        UNIQUE (`WorkOrderId`, `SequenceNo`, `DunnagePartId`),
    CONSTRAINT `fk_WorkOrderDunnageAssignments_User`
        FOREIGN KEY (`LastModifiedByUserId`) REFERENCES `Users` (`Id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `SetupTechDunnageTypeConfig`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT,
    `DunnageTypeId`   INT          NOT NULL,
    `DunnageTypeName` VARCHAR(100) NOT NULL,
    `IsEnabled`       TINYINT(1)   NOT NULL DEFAULT 1,
    `DisplayOrder`    INT          NOT NULL DEFAULT 99,
    `CreatedAt`       DATETIME     NOT NULL,
    `UpdatedAt`       DATETIME     NOT NULL,
    CONSTRAINT `pk_SetupTechDunnageTypeConfig` PRIMARY KEY (`Id`),
    CONSTRAINT `uq_SetupTechDunnageTypeConfig_TypeId` UNIQUE (`DunnageTypeId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `WorkOrderSubordinateParts`
(
    `Id`          INT            NOT NULL AUTO_INCREMENT,
    `WorkOrderId` VARCHAR(50)    NOT NULL,
    `SequenceNo`  INT            NOT NULL,
    `SubPartId`   VARCHAR(50)    NOT NULL,
    `SubPartDesc` VARCHAR(200)   NULL,
    `RequiredQty` DECIMAL(10, 4) NOT NULL DEFAULT 1.0000,
    `QtyOnHand`   DECIMAL(10, 4) NOT NULL DEFAULT 0.0000,
    `CachedAt`    DATETIME       NOT NULL,
    `CreatedAt`   DATETIME       NOT NULL,
    `UpdatedAt`   DATETIME       NOT NULL,
    CONSTRAINT `pk_WorkOrderSubordinateParts` PRIMARY KEY (`Id`),
    CONSTRAINT `uq_WorkOrderSubordinateParts_WO_Seq_Part`
        UNIQUE (`WorkOrderId`, `SequenceNo`, `SubPartId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────
-- SECTION 2 — Indexes
-- ─────────────────────────────────────────────────────────────

DROP INDEX IF EXISTS `idx_WorkstationActiveJobs_SetupTechUserId` ON `WorkstationActiveJobs`;
CREATE INDEX `idx_WorkstationActiveJobs_SetupTechUserId` ON `WorkstationActiveJobs` (`SetupTechUserId`);
DROP INDEX IF EXISTS `idx_WorkstationActiveJobs_WorkOrder_Sequence` ON `WorkstationActiveJobs`;
CREATE INDEX `idx_WorkstationActiveJobs_WorkOrder_Sequence` ON `WorkstationActiveJobs` (`WorkOrderId`, `SequenceNo`);

DROP INDEX IF EXISTS `idx_WorkstationJobHistory_Workcenter_ActiveFrom` ON `WorkstationJobHistory`;
CREATE INDEX `idx_WorkstationJobHistory_Workcenter_ActiveFrom` ON `WorkstationJobHistory` (`WorkcenterId`, `ActiveFrom`);
DROP INDEX IF EXISTS `idx_WorkstationJobHistory_SetupTechUserId` ON `WorkstationJobHistory`;
CREATE INDEX `idx_WorkstationJobHistory_SetupTechUserId` ON `WorkstationJobHistory` (`SetupTechUserId`);
DROP INDEX IF EXISTS `idx_WorkstationJobHistory_WorkOrder_Sequence` ON `WorkstationJobHistory`;
CREATE INDEX `idx_WorkstationJobHistory_WorkOrder_Sequence` ON `WorkstationJobHistory` (`WorkOrderId`, `SequenceNo`);

DROP INDEX IF EXISTS `idx_WorkOrderDunnageAssignments_LastModifiedByUserId` ON `WorkOrderDunnageAssignments`;
CREATE INDEX `idx_WorkOrderDunnageAssignments_LastModifiedByUserId` ON `WorkOrderDunnageAssignments` (`LastModifiedByUserId`);
DROP INDEX IF EXISTS `idx_WorkOrderDunnageAssignments_DunnageTypeId` ON `WorkOrderDunnageAssignments`;
CREATE INDEX `idx_WorkOrderDunnageAssignments_DunnageTypeId` ON `WorkOrderDunnageAssignments` (`DunnageTypeId`);

DROP INDEX IF EXISTS `idx_SetupTechDunnageTypeConfig_IsEnabled_DisplayOrder` ON `SetupTechDunnageTypeConfig`;
CREATE INDEX `idx_SetupTechDunnageTypeConfig_IsEnabled_DisplayOrder` ON `SetupTechDunnageTypeConfig` (`IsEnabled`, `DisplayOrder`);

DROP INDEX IF EXISTS `idx_WorkOrderSubordinateParts_CachedAt` ON `WorkOrderSubordinateParts`;
CREATE INDEX `idx_WorkOrderSubordinateParts_CachedAt` ON `WorkOrderSubordinateParts` (`CachedAt`);

-- ─────────────────────────────────────────────────────────────
-- SECTION 3 — Triggers
-- ─────────────────────────────────────────────────────────────

DROP TRIGGER IF EXISTS `trg_WorkstationActiveJobs_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_WorkstationActiveJobs_BeforeInsert`
BEFORE INSERT ON `WorkstationActiveJobs`
FOR EACH ROW
BEGIN
    IF NEW.`ActiveSince` IS NULL THEN
        SET NEW.`ActiveSince` = UTC_TIMESTAMP();
    END IF;
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WorkstationActiveJobs_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_WorkstationActiveJobs_BeforeUpdate`
BEFORE UPDATE ON `WorkstationActiveJobs`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WorkstationJobHistory_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_WorkstationJobHistory_BeforeInsert`
BEFORE INSERT ON `WorkstationJobHistory`
FOR EACH ROW
BEGIN
    IF NEW.`ActiveFrom` IS NULL THEN
        SET NEW.`ActiveFrom` = UTC_TIMESTAMP();
    END IF;
    IF NEW.`ActiveUntil` IS NULL THEN
        SET NEW.`ActiveUntil` = UTC_TIMESTAMP();
    END IF;
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WorkstationJobHistory_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_WorkstationJobHistory_BeforeUpdate`
BEFORE UPDATE ON `WorkstationJobHistory`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WorkOrderDunnageAssignments_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_WorkOrderDunnageAssignments_BeforeInsert`
BEFORE INSERT ON `WorkOrderDunnageAssignments`
FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WorkOrderDunnageAssignments_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_WorkOrderDunnageAssignments_BeforeUpdate`
BEFORE UPDATE ON `WorkOrderDunnageAssignments`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_SetupTechDunnageTypeConfig_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_SetupTechDunnageTypeConfig_BeforeInsert`
BEFORE INSERT ON `SetupTechDunnageTypeConfig`
FOR EACH ROW
BEGIN
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_SetupTechDunnageTypeConfig_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_SetupTechDunnageTypeConfig_BeforeUpdate`
BEFORE UPDATE ON `SetupTechDunnageTypeConfig`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WorkOrderSubordinateParts_BeforeInsert`;
DELIMITER $$
CREATE TRIGGER `trg_WorkOrderSubordinateParts_BeforeInsert`
BEFORE INSERT ON `WorkOrderSubordinateParts`
FOR EACH ROW
BEGIN
    IF NEW.`CachedAt` IS NULL THEN
        SET NEW.`CachedAt` = UTC_TIMESTAMP();
    END IF;
    SET NEW.`CreatedAt` = UTC_TIMESTAMP();
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS `trg_WorkOrderSubordinateParts_BeforeUpdate`;
DELIMITER $$
CREATE TRIGGER `trg_WorkOrderSubordinateParts_BeforeUpdate`
BEFORE UPDATE ON `WorkOrderSubordinateParts`
FOR EACH ROW
BEGIN
    SET NEW.`UpdatedAt` = UTC_TIMESTAMP();
END$$
DELIMITER ;

-- ─────────────────────────────────────────────────────────────
-- SECTION 4 — Stored Procedures
-- ─────────────────────────────────────────────────────────────

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetActiveJob`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_GetActiveJob`(IN p_WorkcenterId VARCHAR(100))
BEGIN
    SELECT `Id`, `WorkcenterId`, `WorkOrderId`, `SequenceNo`, `PartId`, `PartType`, `SetupTechUserId`, `ActiveSince`, `CreatedAt`, `UpdatedAt`
    FROM `WorkstationActiveJobs`
    WHERE `WorkcenterId` = p_WorkcenterId
    LIMIT 1;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_SetActiveJob`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_SetActiveJob`
(
    IN p_WorkcenterId    VARCHAR(100),
    IN p_WorkOrderId     VARCHAR(50),
    IN p_SequenceNo      INT,
    IN p_PartId          VARCHAR(50),
    IN p_PartType        VARCHAR(50),
    IN p_SetupTechUserId INT,
    IN p_ActiveSince     DATETIME
)
BEGIN
    DECLARE v_ExistingId INT DEFAULT NULL;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;
    START TRANSACTION;
    SELECT `Id` INTO v_ExistingId
    FROM `WorkstationActiveJobs`
    WHERE `WorkcenterId` = p_WorkcenterId
    LIMIT 1;
    IF v_ExistingId IS NOT NULL THEN
        INSERT INTO `WorkstationJobHistory`
            (`WorkcenterId`, `WorkOrderId`, `SequenceNo`, `PartId`, `PartType`, `SetupTechUserId`, `ActiveFrom`, `ActiveUntil`)
        SELECT `WorkcenterId`, `WorkOrderId`, `SequenceNo`, `PartId`, `PartType`, `SetupTechUserId`, `ActiveSince`, UTC_TIMESTAMP()
        FROM `WorkstationActiveJobs`
        WHERE `Id` = v_ExistingId;
        DELETE FROM `WorkstationActiveJobs` WHERE `Id` = v_ExistingId;
    END IF;
    INSERT INTO `WorkstationActiveJobs`
        (`WorkcenterId`, `WorkOrderId`, `SequenceNo`, `PartId`, `PartType`, `SetupTechUserId`, `ActiveSince`)
    VALUES
        (p_WorkcenterId, p_WorkOrderId, p_SequenceNo, p_PartId, p_PartType, p_SetupTechUserId, IFNULL(p_ActiveSince, UTC_TIMESTAMP()));
    COMMIT;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetDunnageAssignment`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_GetDunnageAssignment`
(
    IN p_WorkOrderId VARCHAR(50),
    IN p_SequenceNo  INT
)
BEGIN
    SELECT `Id`, `WorkOrderId`, `SequenceNo`, `DunnagePartId`, `DunnagePartName`, `DunnageTypeId`, `DunnageTypeName`, `LastModifiedByUserId`, `CreatedAt`, `UpdatedAt`
    FROM `WorkOrderDunnageAssignments`
    WHERE `WorkOrderId` = p_WorkOrderId
      AND `SequenceNo` = p_SequenceNo
    ORDER BY `DunnageTypeName`, `DunnagePartName`, `Id`;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_UpsertDunnageAssignment`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_UpsertDunnageAssignment`
(
    IN p_WorkOrderId          VARCHAR(50),
    IN p_SequenceNo           INT,
    IN p_DunnagePartId        INT,
    IN p_DunnagePartName      VARCHAR(200),
    IN p_DunnageTypeId        INT,
    IN p_DunnageTypeName      VARCHAR(100),
    IN p_LastModifiedByUserId INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;
    START TRANSACTION;
    INSERT INTO `WorkOrderDunnageAssignments`
        (`WorkOrderId`, `SequenceNo`, `DunnagePartId`, `DunnagePartName`, `DunnageTypeId`, `DunnageTypeName`, `LastModifiedByUserId`)
    VALUES
        (p_WorkOrderId, p_SequenceNo, p_DunnagePartId, p_DunnagePartName, p_DunnageTypeId, p_DunnageTypeName, p_LastModifiedByUserId)
    ON DUPLICATE KEY UPDATE
        `DunnagePartName` = VALUES(`DunnagePartName`),
        `DunnageTypeId` = VALUES(`DunnageTypeId`),
        `DunnageTypeName` = VALUES(`DunnageTypeName`),
        `LastModifiedByUserId` = VALUES(`LastModifiedByUserId`);
    COMMIT;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_DeleteDunnageAssignment`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_DeleteDunnageAssignment`(IN p_Id INT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;
    START TRANSACTION;
    DELETE FROM `WorkOrderDunnageAssignments`
    WHERE `Id` = p_Id;
    COMMIT;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_UpsertSubordinateParts`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_UpsertSubordinateParts`
(
    IN p_WorkOrderId VARCHAR(50),
    IN p_SequenceNo  INT,
    IN p_SubPartId   VARCHAR(50),
    IN p_SubPartDesc VARCHAR(200),
    IN p_RequiredQty DECIMAL(10, 4),
    IN p_QtyOnHand   DECIMAL(10, 4),
    IN p_CachedAt    DATETIME
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;
    START TRANSACTION;
    INSERT INTO `WorkOrderSubordinateParts`
        (`WorkOrderId`, `SequenceNo`, `SubPartId`, `SubPartDesc`, `RequiredQty`, `QtyOnHand`, `CachedAt`)
    VALUES
        (p_WorkOrderId, p_SequenceNo, p_SubPartId, p_SubPartDesc, IFNULL(p_RequiredQty, 1.0000), IFNULL(p_QtyOnHand, 0.0000), IFNULL(p_CachedAt, UTC_TIMESTAMP()))
    ON DUPLICATE KEY UPDATE
        `SubPartDesc` = VALUES(`SubPartDesc`),
        `RequiredQty` = VALUES(`RequiredQty`),
        `QtyOnHand` = VALUES(`QtyOnHand`),
        `CachedAt` = VALUES(`CachedAt`);
    COMMIT;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetJobHistory`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_GetJobHistory`
(
    IN p_WorkcenterId VARCHAR(100),
    IN p_PageSize     INT
)
BEGIN
    DECLARE v_PageSize INT DEFAULT 50;
    SET v_PageSize = IFNULL(p_PageSize, 50);
    SELECT `Id`, `WorkcenterId`, `WorkOrderId`, `SequenceNo`, `PartId`, `PartType`, `SetupTechUserId`, `ActiveFrom`, `ActiveUntil`, `CreatedAt`, `UpdatedAt`
    FROM `WorkstationJobHistory`
    WHERE `WorkcenterId` = p_WorkcenterId
    ORDER BY `ActiveFrom` DESC, `Id` DESC
    LIMIT v_PageSize;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetEnabledDunnageTypes`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_GetEnabledDunnageTypes`()
BEGIN
    SELECT `Id`, `DunnageTypeId`, `DunnageTypeName`, `IsEnabled`, `DisplayOrder`, `CreatedAt`, `UpdatedAt`
    FROM `SetupTechDunnageTypeConfig`
    WHERE `IsEnabled` = 1
    ORDER BY `DisplayOrder`, `DunnageTypeName`, `Id`;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS `usp_SetupTech_UpsertDunnageTypeConfig`;
DELIMITER $$
CREATE PROCEDURE `usp_SetupTech_UpsertDunnageTypeConfig`
(
    IN p_DunnageTypeId   INT,
    IN p_DunnageTypeName VARCHAR(100),
    IN p_IsEnabled       TINYINT(1),
    IN p_DisplayOrder    INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;
    START TRANSACTION;
    INSERT INTO `SetupTechDunnageTypeConfig`
        (`DunnageTypeId`, `DunnageTypeName`, `IsEnabled`, `DisplayOrder`)
    VALUES
        (p_DunnageTypeId, p_DunnageTypeName, IFNULL(p_IsEnabled, 1), IFNULL(p_DisplayOrder, 99))
    ON DUPLICATE KEY UPDATE
        `DunnageTypeName` = VALUES(`DunnageTypeName`),
        `IsEnabled` = VALUES(`IsEnabled`),
        `DisplayOrder` = VALUES(`DisplayOrder`);
    COMMIT;
END$$
DELIMITER ;