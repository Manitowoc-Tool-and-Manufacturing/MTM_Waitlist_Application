-- =============================================================
-- MTM Waitlist Application — WorkOrderDunnageAssignments Table
-- Domain:      SetupTech
-- Description: Caches the dunnage assignment list for a work-order and
--              sequence pair. No quantity is stored; only the association.
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `WorkOrderDunnageAssignments`
(
    `Id`                   INT          NOT NULL AUTO_INCREMENT
                                          COMMENT 'Surrogate primary key.',

    `WorkOrderId`          VARCHAR(50)  NOT NULL
                                          COMMENT 'Infor Visual work order identifier.',

    `SequenceNo`           INT          NOT NULL
                                          COMMENT 'Infor Visual work-order sequence number.',

    `DunnagePartId`        INT          NOT NULL
                                          COMMENT 'Mirror of mtm_receiving_application.dunnage_parts.id.',

    `DunnagePartName`      VARCHAR(200) NOT NULL
                                          COMMENT 'Denormalized receiving-app part name for offline display.',

    `DunnageTypeId`        INT          NOT NULL
                                          COMMENT 'Mirror of mtm_receiving_application.dunnage_types.id.',

    `DunnageTypeName`      VARCHAR(100) NOT NULL
                                          COMMENT 'Denormalized receiving-app type name for offline display.',

    `LastModifiedByUserId` INT          NOT NULL
                                          COMMENT 'FK -> Users.Id. Last setup tech to change this assignment line.',

    `CreatedAt`            DATETIME     NOT NULL
                                          COMMENT 'UTC - set by trg_WorkOrderDunnageAssignments_BeforeInsert.',

    `UpdatedAt`            DATETIME     NOT NULL
                                          COMMENT 'UTC - updated by trg_WorkOrderDunnageAssignments_BeforeUpdate.',

    CONSTRAINT `pk_WorkOrderDunnageAssignments`
        PRIMARY KEY (`Id`),

    CONSTRAINT `uq_WorkOrderDunnageAssignments_WO_Seq_Part`
        UNIQUE (`WorkOrderId`, `SequenceNo`, `DunnagePartId`),

    CONSTRAINT `fk_WorkOrderDunnageAssignments_User`
        FOREIGN KEY (`LastModifiedByUserId`)
        REFERENCES `Users` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Cached SetupTech dunnage assignment lines per work order and sequence.';