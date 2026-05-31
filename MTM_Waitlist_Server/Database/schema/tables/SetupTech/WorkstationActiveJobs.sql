-- =============================================================
-- MTM Waitlist Application — WorkstationActiveJobs Table
-- Domain:      SetupTech
-- Description: Stores the current active job configuration for each
--              workstation. One workstation may have only one active job.
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `WorkstationActiveJobs`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT
                                     COMMENT 'Surrogate primary key.',

    `WorkcenterId`    VARCHAR(100) NOT NULL
                                     COMMENT 'Infor Visual SHOP_RESOURCE.ID for the workstation currently being configured.',

    `WorkOrderId`     VARCHAR(50)  NOT NULL
                                     COMMENT 'Infor Visual work order identifier assigned to the workstation.',

    `SequenceNo`      INT          NOT NULL
                                     COMMENT 'Infor Visual work-order sequence number.',

    `PartId`          VARCHAR(50)  NOT NULL
                                     COMMENT 'Primary part identifier for the active job.',

    `PartType`        VARCHAR(50)  NOT NULL
                                     COMMENT 'FinishedGoods, WIP, OutsideService, or other documented SetupTech part classification.',

    `SetupTechUserId` INT          NOT NULL
                                     COMMENT 'FK -> Users.Id. Setup technician who last saved the active job.',

    `ActiveSince`     DATETIME     NOT NULL
                                     COMMENT 'UTC timestamp when this active job became current on the workstation.',

    `CreatedAt`       DATETIME     NOT NULL
                                     COMMENT 'UTC - set by trg_WorkstationActiveJobs_BeforeInsert.',

    `UpdatedAt`       DATETIME     NOT NULL
                                     COMMENT 'UTC - updated by trg_WorkstationActiveJobs_BeforeUpdate.',

    CONSTRAINT `pk_WorkstationActiveJobs`
        PRIMARY KEY (`Id`),

    CONSTRAINT `uq_WorkstationActiveJobs_Workcenter`
        UNIQUE (`WorkcenterId`),

    CONSTRAINT `fk_WorkstationActiveJobs_User`
        FOREIGN KEY (`SetupTechUserId`)
        REFERENCES `Users` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Current SetupTech job assignment per workstation.';