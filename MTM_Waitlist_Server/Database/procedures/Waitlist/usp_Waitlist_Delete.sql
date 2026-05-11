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
