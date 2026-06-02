-- =============================================================
-- MTM Waitlist Application — usp_Auth_CheckSharedWorkstation
-- Domain:      Auth
-- Description: Checks whether a given Windows username belongs to a shared
--              workstation that requires manual credential login.
--              Returns the SharedWorkstations row if found and IsActive = 1;
--              returns no rows if the workstation is personal (auto-login).
--
-- Login flow:
--   Row returned  → show login form → call usp_Auth_ValidateCredentials
--   No row        → call usp_Auth_GetUserByWindowsUsername for auto-login
--
-- Called by:   IService_Auth.DetermineLoginModeAsync() via the REST API
-- Depends on:  schema/tables/Auth/SharedWorkstations.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_CheckSharedWorkstation`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_CheckSharedWorkstation`
(
    IN p_WindowsUsername VARCHAR(100)
)
BEGIN
    SELECT
        `Id`,
        `WindowsUsername`,
        `MachineName`,
        `IsActive`
    FROM  `SharedWorkstations`
    WHERE `WindowsUsername` = p_WindowsUsername
      AND `IsActive`        = 1
    LIMIT 1;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Auth_GetRefreshToken
-- Domain:      Auth
-- Description: Looks up an active refresh token by its hash and returns the
--              token record joined with the owning user's account details.
--              Returns no rows if the token is expired, revoked, or unknown.
--              The API hashes the incoming raw token and passes the hash here.
-- Called by:   IService_Auth.RefreshTokenAsync() via the REST API
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_GetRefreshToken`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_GetRefreshToken`
(
    IN p_TokenHash VARCHAR(256)
)
BEGIN
    SELECT
        `rt`.`Id`          AS `TokenId`,
        `rt`.`UserId`,
        `rt`.`ExpiresAt`,
        `rt`.`RevokedAt`,
        `u`.`Username`,
        `u`.`DisplayName`,
        `u`.`Role`,
        `u`.`IsActive`
    FROM  `RefreshTokens` `rt`
    INNER JOIN `Users`    `u`  ON `u`.`Id` = `rt`.`UserId`
    WHERE `rt`.`TokenHash` = p_TokenHash
      AND `rt`.`RevokedAt`  IS NULL
      AND `rt`.`ExpiresAt`  > UTC_TIMESTAMP()
    LIMIT 1;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Auth_GetUserByWindowsUsername
-- Domain:      Auth
-- Description: Looks up an active user by their Windows domain username for
--              automatic login on personal (non-shared) workstations.
--              Returns no rows if the Windows username is not mapped to any
--              user or if the account is inactive.
--              No password check is performed — Windows authentication
--              already verified identity before this is called.
-- Called by:   IService_Auth.AutoLoginAsync() via the REST API
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_GetUserByWindowsUsername`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_GetUserByWindowsUsername`
(
    IN p_WindowsUsername VARCHAR(100)
)
BEGIN
    SELECT
        `Id`,
        `Username`,
        `DisplayName`,
        `Role`,
        `IsActive`
    FROM  `Users`
    WHERE `WindowsUsername` = CONVERT(p_WindowsUsername USING utf8mb4) COLLATE utf8mb4_unicode_ci
      AND `IsActive`        = 1
    LIMIT 1;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Auth_RecordLogin
-- Domain:      Auth
-- Description: Updates LastLoginAt to the current UTC timestamp after a
--              successful authentication (credential login or auto-login).
--              Called once per successful login — not on token refresh.
-- Called by:   IService_Auth.LoginAsync() via the REST API
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_RecordLogin`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_RecordLogin`
(
    IN p_UserId INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `Users`
    SET    `LastLoginAt` = UTC_TIMESTAMP()
    WHERE  `Id`         = p_UserId;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Auth_RevokeAllUserTokens
-- Domain:      Auth
-- Description: Marks ALL active refresh tokens for a given user as revoked.
--              Used when an admin deactivates an account, a user changes their
--              password, or a security event requires forced sign-out.
-- Called by:   IService_Auth.LogoutAsync() (full sign-out) or admin revocation
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_RevokeAllUserTokens`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_RevokeAllUserTokens`
(
    IN p_UserId INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `RefreshTokens`
    SET    `RevokedAt` = UTC_TIMESTAMP()
    WHERE  `UserId`    = p_UserId
      AND  `RevokedAt`  IS NULL;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Auth_RevokeRefreshToken
-- Domain:      Auth
-- Description: Marks a single refresh token as revoked by setting RevokedAt
--              to the current UTC timestamp. Used on logout when the client
--              supplies its current refresh token.
-- Called by:   IService_Auth.LogoutAsync() via the REST API
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_RevokeRefreshToken`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_RevokeRefreshToken`
(
    IN p_TokenHash VARCHAR(256)
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `RefreshTokens`
    SET    `RevokedAt` = UTC_TIMESTAMP()
    WHERE  `TokenHash` = p_TokenHash
      AND  `RevokedAt`  IS NULL;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Auth_SaveRefreshToken
-- Domain:      Auth
-- Description: Persists a new refresh token hash for a user.
--              The API generates the raw token, SHA-256 hashes it, then calls
--              this procedure — plaintext tokens never reach MySQL.
-- Called by:   IService_Auth.LoginAsync() and IService_Auth.RefreshTokenAsync()
-- Depends on:  schema/tables/Auth/RefreshTokens.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_SaveRefreshToken`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_SaveRefreshToken`
(
    IN p_UserId    INT,
    IN p_TokenHash VARCHAR(256),
    IN p_ExpiresAt DATETIME
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    INSERT INTO `RefreshTokens`
        (`UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`)
    VALUES
        (p_UserId, p_TokenHash, p_ExpiresAt, UTC_TIMESTAMP());
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Auth_ValidateCredentials
-- Domain:      Auth
-- Description: Returns the stored password hash and account details for a
--              given Username so the API backend can perform bcrypt comparison.
--              Used for credential-based login on shared workstations.
--              Password comparison MUST happen in the API — never in SQL.
--              Returns no rows if the Username does not exist or IsActive = 0.
-- Called by:   IService_Auth.LoginAsync() via the REST API
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Auth_ValidateCredentials`;

DELIMITER $$

CREATE PROCEDURE `usp_Auth_ValidateCredentials`
(
    IN  p_Username    VARCHAR(100)
)
BEGIN
    -- Return the hash and metadata; the API performs bcrypt.VerifyHash(plaintext, hash).
    SELECT
        `Id`,
        `PasswordHash`,
        `DisplayName`,
        `Role`,
        `IsActive`
    FROM  `Users`
    WHERE `Username` = p_Username
      AND `IsActive` = 1
    LIMIT 1;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_DeleteDunnageAssignment
-- Domain:      SetupTech
-- Description: Deletes one cached dunnage assignment line by Id.
-- Called by:   IRepository_WorkOrderDunnage.DeleteDunnageAssignmentAsync()
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_DeleteDunnageAssignment`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_DeleteDunnageAssignment`
(
    IN p_Id INT
)
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
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetActiveJob
-- Domain:      SetupTech
-- Description: Returns the current active job row for a workstation.
-- Called by:   IRepository_SetupTechActiveJob.GetActiveJobAsync()
-- Depends on:  schema/tables/SetupTech/WorkstationActiveJobs.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetActiveJob`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_GetActiveJob`
(
    IN p_WorkcenterId VARCHAR(100)
)
BEGIN
    SELECT
        `Id`,
        `WorkcenterId`,
        `WorkOrderId`,
        `SequenceNo`,
        `PartId`,
        `PartType`,
        `SetupTechUserId`,
        `ActiveSince`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `WorkstationActiveJobs`
    WHERE `WorkcenterId` = p_WorkcenterId
    LIMIT 1;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetDunnageAssignment
-- Domain:      SetupTech
-- Description: Returns cached dunnage assignment rows for a work-order and sequence.
-- Called by:   IRepository_WorkOrderDunnage.GetDunnageAssignmentAsync()
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetDunnageAssignment`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_GetDunnageAssignment`
(
    IN p_WorkOrderId VARCHAR(50),
    IN p_SequenceNo  INT
)
BEGIN
    SELECT
        `Id`,
        `WorkOrderId`,
        `SequenceNo`,
        `DunnagePartId`,
        `DunnagePartName`,
        `DunnageTypeId`,
        `DunnageTypeName`,
        `LastModifiedByUserId`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `WorkOrderDunnageAssignments`
    WHERE `WorkOrderId` = p_WorkOrderId
      AND `SequenceNo`  = p_SequenceNo
    ORDER BY `DunnageTypeName`, `DunnagePartName`, `Id`;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetEnabledDunnageTypes
-- Domain:      SetupTech
-- Description: Returns enabled SetupTech dunnage type config rows in display order.
-- Called by:   IRepository_SetupTechDunnageTypeConfig.GetEnabledDunnageTypesAsync()
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_SetupTech_GetEnabledDunnageTypes`;

DELIMITER $$

CREATE PROCEDURE `usp_SetupTech_GetEnabledDunnageTypes`()
BEGIN
    SELECT
        `Id`,
        `DunnageTypeId`,
        `DunnageTypeName`,
        `IsEnabled`,
        `DisplayOrder`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `SetupTechDunnageTypeConfig`
    WHERE `IsEnabled` = 1
    ORDER BY `DisplayOrder`, `DunnageTypeName`, `Id`;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_GetJobHistory
-- Domain:      SetupTech
-- Description: Returns archived workstation job history in reverse chronological order.
-- Called by:   IRepository_SetupTechActiveJob.GetJobHistoryAsync()
-- Depends on:  schema/tables/SetupTech/WorkstationJobHistory.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

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

    SELECT
        `Id`,
        `WorkcenterId`,
        `WorkOrderId`,
        `SequenceNo`,
        `PartId`,
        `PartType`,
        `SetupTechUserId`,
        `ActiveFrom`,
        `ActiveUntil`,
        `CreatedAt`,
        `UpdatedAt`
    FROM `WorkstationJobHistory`
    WHERE `WorkcenterId` = p_WorkcenterId
    ORDER BY `ActiveFrom` DESC, `Id` DESC
    LIMIT v_PageSize;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_SetActiveJob
-- Domain:      SetupTech
-- Description: Archives an existing workstation active job to history and
--              inserts the new active job in a single transaction.
-- Called by:   IRepository_SetupTechActiveJob.SetActiveJobAsync()
-- Depends on:  schema/tables/SetupTech/WorkstationActiveJobs.sql,
--              schema/tables/SetupTech/WorkstationJobHistory.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

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

    SELECT `Id`
    INTO v_ExistingId
    FROM `WorkstationActiveJobs`
    WHERE `WorkcenterId` = p_WorkcenterId
    LIMIT 1;

    IF v_ExistingId IS NOT NULL THEN
        INSERT INTO `WorkstationJobHistory`
        (
            `WorkcenterId`,
            `WorkOrderId`,
            `SequenceNo`,
            `PartId`,
            `PartType`,
            `SetupTechUserId`,
            `ActiveFrom`,
            `ActiveUntil`
        )
        SELECT
            `WorkcenterId`,
            `WorkOrderId`,
            `SequenceNo`,
            `PartId`,
            `PartType`,
            `SetupTechUserId`,
            `ActiveSince`,
            UTC_TIMESTAMP()
        FROM `WorkstationActiveJobs`
        WHERE `Id` = v_ExistingId;

        DELETE FROM `WorkstationActiveJobs`
        WHERE `Id` = v_ExistingId;
    END IF;

    INSERT INTO `WorkstationActiveJobs`
    (
        `WorkcenterId`,
        `WorkOrderId`,
        `SequenceNo`,
        `PartId`,
        `PartType`,
        `SetupTechUserId`,
        `ActiveSince`
    )
    VALUES
    (
        p_WorkcenterId,
        p_WorkOrderId,
        p_SequenceNo,
        p_PartId,
        p_PartType,
        p_SetupTechUserId,
        IFNULL(p_ActiveSince, UTC_TIMESTAMP())
    );

    COMMIT;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_UpsertDunnageAssignment
-- Domain:      SetupTech
-- Description: Inserts or updates one dunnage assignment line for a
--              work-order and sequence pair.
-- Called by:   IRepository_WorkOrderDunnage.UpsertDunnageAssignmentAsync()
-- Depends on:  schema/tables/SetupTech/WorkOrderDunnageAssignments.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

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
    (
        `WorkOrderId`,
        `SequenceNo`,
        `DunnagePartId`,
        `DunnagePartName`,
        `DunnageTypeId`,
        `DunnageTypeName`,
        `LastModifiedByUserId`
    )
    VALUES
    (
        p_WorkOrderId,
        p_SequenceNo,
        p_DunnagePartId,
        p_DunnagePartName,
        p_DunnageTypeId,
        p_DunnageTypeName,
        p_LastModifiedByUserId
    )
    ON DUPLICATE KEY UPDATE
        `DunnagePartName`      = VALUES(`DunnagePartName`),
        `DunnageTypeId`        = VALUES(`DunnageTypeId`),
        `DunnageTypeName`      = VALUES(`DunnageTypeName`),
        `LastModifiedByUserId` = VALUES(`LastModifiedByUserId`);

    COMMIT;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_UpsertDunnageTypeConfig
-- Domain:      SetupTech
-- Description: Inserts or updates a SetupTech dunnage-type config row.
-- Called by:   Admin configuration sync for SetupTech dunnage type filters.
-- Depends on:  schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

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
    (
        `DunnageTypeId`,
        `DunnageTypeName`,
        `IsEnabled`,
        `DisplayOrder`
    )
    VALUES
    (
        p_DunnageTypeId,
        p_DunnageTypeName,
        IFNULL(p_IsEnabled, 1),
        IFNULL(p_DisplayOrder, 99)
    )
    ON DUPLICATE KEY UPDATE
        `DunnageTypeName` = VALUES(`DunnageTypeName`),
        `IsEnabled`       = VALUES(`IsEnabled`),
        `DisplayOrder`    = VALUES(`DisplayOrder`);

    COMMIT;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_SetupTech_UpsertSubordinateParts
-- Domain:      SetupTech
-- Description: Inserts or updates one subordinate part cache row for a
--              work-order and sequence pair.
-- Called by:   IRepository_SetupTechActiveJob.SetSubordinatePartsAsync()
-- Depends on:  schema/tables/SetupTech/WorkOrderSubordinateParts.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

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
    (
        `WorkOrderId`,
        `SequenceNo`,
        `SubPartId`,
        `SubPartDesc`,
        `RequiredQty`,
        `QtyOnHand`,
        `CachedAt`
    )
    VALUES
    (
        p_WorkOrderId,
        p_SequenceNo,
        p_SubPartId,
        p_SubPartDesc,
        IFNULL(p_RequiredQty, 1.0000),
        IFNULL(p_QtyOnHand, 0.0000),
        IFNULL(p_CachedAt, UTC_TIMESTAMP())
    )
    ON DUPLICATE KEY UPDATE
        `SubPartDesc` = VALUES(`SubPartDesc`),
        `RequiredQty` = VALUES(`RequiredQty`),
        `QtyOnHand`   = VALUES(`QtyOnHand`),
        `CachedAt`    = VALUES(`CachedAt`);

    COMMIT;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_Delete
-- Domain:      Waitlist
-- Description: Hard-deletes a waitlist entry by Id.
--              Prefer usp_Waitlist_Update with Status='Cancelled' to preserve
--              audit history unless a hard delete is explicitly required.
-- Called by:   IRepository_WaitlistEntry.DeleteWaitlistEntryAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Delete`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_Delete`
(
    IN p_Id INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    DELETE FROM `WaitlistEntries`
    WHERE `Id` = p_Id;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_GetAll
-- Domain:      Waitlist
-- Description: Returns all waitlist entries, optionally filtered by Status
--              and/or RequestType. Results are ordered by Priority ASC then
--              RequestedAt ASC (earliest high-priority entries first).
--              Optional p_Limit / p_Offset support pagination from the API.
--              All filter parameters are nullable — pass NULL to skip.
-- Called by:   IRepository_WaitlistEntry.GetAllWaitlistEntriesAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_GetAll`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_GetAll`
(
    IN p_Status      VARCHAR(20),    -- NULL = all statuses
    IN p_RequestType VARCHAR(30),    -- NULL = all request types
    IN p_Limit       INT,            -- 0 or NULL = no limit
    IN p_Offset      INT             -- 0 or NULL = start from beginning
)
BEGIN
    -- Default no-limit so callers that pass NULL still get all rows.
    SET p_Limit  = IF(p_Limit  IS NULL OR p_Limit  = 0, 2147483647, p_Limit);
    SET p_Offset = IF(p_Offset IS NULL,                 0,          p_Offset);

    SELECT
        `Id`,
        `WorkcenterName`,
        `RequestType`,
        `Status`,
        `Priority`,
        `Notes`,
        `RequestedAt`,
        `ScheduledAt`,
        `CompletedAt`,
        `AssignedToUserId`,
        `CreatedAt`,
        `UpdatedAt`,
        `CreatedByUserId`,
        `UpdatedByUserId`
    FROM  `WaitlistEntries`
    WHERE (p_Status      IS NULL OR `Status`      = p_Status)
      AND (p_RequestType IS NULL OR `RequestType` = p_RequestType)
    ORDER BY `Priority`    ASC,
             `RequestedAt` ASC
    LIMIT  p_Limit
    OFFSET p_Offset;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_GetById
-- Domain:      Waitlist
-- Description: Returns a single waitlist entry by its primary key.
--              Returns no rows if the Id does not exist.
-- Called by:   IRepository_WaitlistEntry.GetWaitlistEntryByIdAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_GetById`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_GetById`
(
    IN p_Id INT
)
BEGIN
    SELECT
        `Id`,
        `WorkcenterName`,
        `RequestType`,
        `Status`,
        `Priority`,
        `Notes`,
        `RequestedAt`,
        `ScheduledAt`,
        `CompletedAt`,
        `AssignedToUserId`,
        `CreatedAt`,
        `UpdatedAt`,
        `CreatedByUserId`,
        `UpdatedByUserId`
    FROM  `WaitlistEntries`
    WHERE `Id` = p_Id
    LIMIT 1;
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_Insert
-- Domain:      Waitlist
-- Description: Inserts a new waitlist entry and returns the generated Id.
--              CreatedAt and UpdatedAt are set by trg_WaitlistEntries_BeforeInsert.
--              RequestedAt defaults to UTC_TIMESTAMP() if not supplied.
--              Status defaults to 'Waiting' and Priority defaults to 5 if not supplied.
-- Called by:   IRepository_WaitlistEntry.InsertWaitlistEntryAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Insert`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_Insert`
(
    IN  p_WorkcenterName   VARCHAR(100),
    IN  p_RequestType      VARCHAR(30),
    IN  p_Status           VARCHAR(20),
    IN  p_Priority         TINYINT UNSIGNED,
    IN  p_Notes            TEXT,
    IN  p_RequestedAt      DATETIME,
    IN  p_ScheduledAt      DATETIME,
    IN  p_AssignedToUserId INT,
    IN  p_CreatedByUserId  INT,
    OUT p_NewId            INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        SET p_NewId = NULL;
        ROLLBACK;
        RESIGNAL;
    END;

    INSERT INTO `WaitlistEntries`
    (
        `WorkcenterName`,
        `RequestType`,
        `Status`,
        `Priority`,
        `Notes`,
        `RequestedAt`,
        `ScheduledAt`,
        `AssignedToUserId`,
        `CreatedByUserId`,
        `UpdatedByUserId`
    )
    VALUES
    (
        p_WorkcenterName,
        p_RequestType,
        IFNULL(p_Status,   'Waiting'),
        IFNULL(p_Priority, 5),
        p_Notes,
        IFNULL(p_RequestedAt, UTC_TIMESTAMP()),
        p_ScheduledAt,
        p_AssignedToUserId,
        p_CreatedByUserId,
        p_CreatedByUserId   -- same user on initial insert
    );

    SET p_NewId = LAST_INSERT_ID();
END$$

DELIMITER ;
-- =============================================================
-- MTM Waitlist Application — usp_Waitlist_Update
-- Domain:      Waitlist
-- Description: Updates all mutable fields of an existing waitlist entry.
--              UpdatedAt is refreshed automatically by trg_WaitlistEntries_BeforeUpdate.
--              CompletedAt is auto-set by the trigger when Status transitions
--              to 'Completed' or 'Cancelled'.
--              Pass all fields; use current values for unchanged columns.
-- Called by:   IRepository_WaitlistEntry.UpdateWaitlistEntryAsync()
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

DROP PROCEDURE IF EXISTS `usp_Waitlist_Update`;

DELIMITER $$

CREATE PROCEDURE `usp_Waitlist_Update`
(
    IN p_Id               INT,
    IN p_WorkcenterName   VARCHAR(100),
    IN p_RequestType      VARCHAR(30),
    IN p_Status           VARCHAR(20),
    IN p_Priority         TINYINT UNSIGNED,
    IN p_Notes            TEXT,
    IN p_RequestedAt      DATETIME,
    IN p_ScheduledAt      DATETIME,
    IN p_CompletedAt      DATETIME,
    IN p_AssignedToUserId INT,
    IN p_UpdatedByUserId  INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    UPDATE `WaitlistEntries`
    SET
        `WorkcenterName`   = p_WorkcenterName,
        `RequestType`      = p_RequestType,
        `Status`           = p_Status,
        `Priority`         = p_Priority,
        `Notes`            = p_Notes,
        `RequestedAt`      = p_RequestedAt,
        `ScheduledAt`      = p_ScheduledAt,
        `CompletedAt`      = p_CompletedAt,
        `AssignedToUserId` = p_AssignedToUserId,
        `UpdatedByUserId`  = p_UpdatedByUserId
        -- UpdatedAt is set by trg_WaitlistEntries_BeforeUpdate automatically.
    WHERE `Id` = p_Id;
END$$

DELIMITER ;
