-- =============================================================
-- MTM Waitlist Application — WorkOrderSubordinateParts Table
-- Domain:      SetupTech
-- Description: Caches subordinate/component parts returned from Infor Visual
--              for a work-order and sequence pair.
-- Depends on:  `mtm_waitlist` already created and selected.
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `WorkOrderSubordinateParts`
(
    `Id`            INT            NOT NULL AUTO_INCREMENT
                                       COMMENT 'Surrogate primary key.',

    `WorkOrderId`   VARCHAR(50)    NOT NULL
                                       COMMENT 'Infor Visual work order identifier.',

    `SequenceNo`    INT            NOT NULL
                                       COMMENT 'Infor Visual work-order sequence number.',

    `SubPartId`     VARCHAR(50)    NOT NULL
                                       COMMENT 'Infor Visual subordinate part identifier.',

    `SubPartDesc`   VARCHAR(200)   NULL
                                       COMMENT 'Human-readable subordinate part description.',

    `RequiredQty`   DECIMAL(10, 4) NOT NULL DEFAULT 1.0000
                                       COMMENT 'Required quantity for the subordinate part on this work-order sequence.',

    `QtyOnHand`     DECIMAL(10, 4) NOT NULL DEFAULT 0.0000
                                       COMMENT 'On-hand quantity captured from Infor Visual at cache time.',

    `CachedAt`      DATETIME       NOT NULL
                                       COMMENT 'UTC timestamp when the subordinate part snapshot was cached.',

    `CreatedAt`     DATETIME       NOT NULL
                                       COMMENT 'UTC - set by trg_WorkOrderSubordinateParts_BeforeInsert.',

    `UpdatedAt`     DATETIME       NOT NULL
                                       COMMENT 'UTC - updated by trg_WorkOrderSubordinateParts_BeforeUpdate.',

    CONSTRAINT `pk_WorkOrderSubordinateParts`
        PRIMARY KEY (`Id`),

    CONSTRAINT `uq_WorkOrderSubordinateParts_WO_Seq_Part`
        UNIQUE (`WorkOrderId`, `SequenceNo`, `SubPartId`)
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Cached subordinate parts per work order and sequence for SetupTech validation.';