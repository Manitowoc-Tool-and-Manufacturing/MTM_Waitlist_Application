-- =============================================================
-- MTM Waitlist Application — WaitlistEntries Table
-- Domain:      Waitlist
-- Description: A workcenter submits a logistics/material-handling request.
--              One row per active or historical request.
--              The RequestType ENUM captures what the workcenter needs;
--              the Status ENUM tracks the request through its lifecycle.
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

CREATE TABLE IF NOT EXISTS `WaitlistEntries`
(
    `Id`               INT              NOT NULL AUTO_INCREMENT
                                        COMMENT 'Surrogate primary key.',

    `WorkcenterName`   VARCHAR(100)     NOT NULL
                                        COMMENT 'Name of the workcenter submitting the request (e.g., Press 3, Line 7). Consider normalising to a Workcenters lookup table in a future migration.',

    `RequestType`      ENUM(
                           'Coil',
                           'Dunnage',
                           'PickUpFinishedGoods',
                           'PickUpUnusedGoods',
                           'PickUpDunnage',
                           'BringPartsToPress',
                           'RemoveCoilFromPress',
                           'BringPickUpDie'
                       )               NOT NULL
                                        COMMENT 'The category of logistics request. Maps to Enum_WaitlistRequestType in C#.',

    `Status`           ENUM(
                           'Waiting',
                           'Active',
                           'Late',
                           'LowImportance',
                           'Project',
                           'Completed',
                           'Cancelled'
                       )               NOT NULL
                                        DEFAULT 'Waiting'
                                        COMMENT 'Lifecycle state. Waiting=queued, Active=being handled, Late=overdue, LowImportance=deprioritised, Project=planned work, Completed/Cancelled=resolved. Maps to Enum_WaitlistStatus in C#.',

    `Priority`         TINYINT UNSIGNED NOT NULL
                                        DEFAULT 5
                                        COMMENT '1 = highest priority, 10 = lowest. Default 5 = normal. Drives ORDER BY in usp_Waitlist_GetAll.',

    `Notes`            TEXT             NULL
                                        COMMENT 'Free-text remarks visible to supervisors and material handlers.',

    `RequestedAt`      DATETIME         NOT NULL
                                        COMMENT 'UTC — when the request was submitted. Defaults to UTC_TIMESTAMP() in usp_Waitlist_Insert if not supplied.',

    `ScheduledAt`      DATETIME         NULL
                                        COMMENT 'UTC — estimated or confirmed time the request will be fulfilled.',

    `CompletedAt`      DATETIME         NULL
                                        COMMENT 'UTC — set automatically by trg_WaitlistEntries_BeforeUpdate when Status transitions to Completed or Cancelled.',

    `AssignedToUserId` INT              NULL
                                        COMMENT 'FK → Users.Id. The MaterialHandler or other user currently responsible for fulfilling this request. NULL = unassigned.',

    `CreatedAt`        DATETIME         NOT NULL
                                        COMMENT 'UTC — set by trg_WaitlistEntries_BeforeInsert.',

    `UpdatedAt`        DATETIME         NOT NULL
                                        COMMENT 'UTC — updated by trg_WaitlistEntries_BeforeUpdate on every change.',

    `CreatedByUserId`  INT              NULL
                                        COMMENT 'FK → Users.Id. The user who created this record. SET NULL if the user is deleted.',

    `UpdatedByUserId`  INT              NULL
                                        COMMENT 'FK → Users.Id. The user who last modified this record. SET NULL if the user is deleted.',

    CONSTRAINT `pk_WaitlistEntries`
        PRIMARY KEY (`Id`),

    CONSTRAINT `fk_WaitlistEntries_AssignedToUser`
        FOREIGN KEY (`AssignedToUserId`)
        REFERENCES `Users` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE,

    CONSTRAINT `fk_WaitlistEntries_CreatedByUser`
        FOREIGN KEY (`CreatedByUserId`)
        REFERENCES `Users` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE,

    CONSTRAINT `fk_WaitlistEntries_UpdatedByUser`
        FOREIGN KEY (`UpdatedByUserId`)
        REFERENCES `Users` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE
)
ENGINE  = InnoDB
DEFAULT CHARSET  = utf8mb4
COLLATE = utf8mb4_unicode_ci
COMMENT = 'Workcenter logistics/material-handling requests queued for fulfillment.';
