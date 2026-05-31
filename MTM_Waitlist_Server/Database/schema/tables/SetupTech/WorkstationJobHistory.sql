-- =============================================================
-- MTM Waitlist Application — WorkstationJobHistory Table
-- Domain:      SetupTech
-- Description: Archives prior workstation job configurations whenever a new
--              active job replaces an existing one.
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `WorkstationJobHistory`
(
    `Id`              INT          NOT NULL AUTO_INCREMENT
                                     COMMENT 'Surrogate primary key.',

    `WorkcenterId`    VARCHAR(100) NOT NULL
                                     COMMENT 'Infor Visual SHOP_RESOURCE.ID for the workstation.',

    `WorkOrderId`     VARCHAR(50)  NOT NULL
                                     COMMENT 'Archived work order identifier.',

    `SequenceNo`      INT          NOT NULL
                                     COMMENT 'Archived work-order sequence number.',

    `PartId`          VARCHAR(50)  NOT NULL
                                     COMMENT 'Primary part identifier for the archived job.',

    `PartType`        VARCHAR(50)  NOT NULL
                                     COMMENT 'FinishedGoods, WIP, OutsideService, or other documented SetupTech part classification.',

    `SetupTechUserId` INT          NOT NULL
                                     COMMENT 'FK -> Users.Id. Setup technician who saved the archived job state.',

    `ActiveFrom`      DATETIME     NOT NULL
                                     COMMENT 'UTC timestamp when the job became active on the workstation.',

    `ActiveUntil`     DATETIME     NOT NULL
                                     COMMENT 'UTC timestamp when the job stopped being the active workstation job.',

    `CreatedAt`       DATETIME     NOT NULL
                                     COMMENT 'UTC - set by trg_WorkstationJobHistory_BeforeInsert.',

    `UpdatedAt`       DATETIME     NOT NULL
                                     COMMENT 'UTC - updated by trg_WorkstationJobHistory_BeforeUpdate.',

    CONSTRAINT `pk_WorkstationJobHistory`
        PRIMARY KEY (`Id`),

    CONSTRAINT `fk_WorkstationJobHistory_User`
        FOREIGN KEY (`SetupTechUserId`)
        REFERENCES `Users` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Archived SetupTech workstation job history for analytics and audit.';