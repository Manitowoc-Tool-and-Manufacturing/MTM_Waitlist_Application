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