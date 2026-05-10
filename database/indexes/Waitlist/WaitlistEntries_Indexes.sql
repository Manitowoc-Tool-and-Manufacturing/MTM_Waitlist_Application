-- =============================================================
-- MTM Waitlist Application — WaitlistEntries Indexes
-- Domain:      Waitlist
-- Description: Indexes for the most common query patterns: status filters,
--              request type filters, priority ordering, and FK lookups.
-- Depends on:  schema/tables/Waitlist/WaitlistEntries.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

-- Status filter — the most common WHERE clause in usp_Waitlist_GetAll.
CREATE INDEX `idx_WaitlistEntries_Status`
    ON `WaitlistEntries` (`Status`);

-- Priority + Status composite — the ORDER BY / WHERE combination in usp_Waitlist_GetAll.
CREATE INDEX `idx_WaitlistEntries_Priority_Status`
    ON `WaitlistEntries` (`Priority`, `Status`);

-- RequestType filter — dispatching Material Handlers by request category.
CREATE INDEX `idx_WaitlistEntries_RequestType`
    ON `WaitlistEntries` (`RequestType`);

-- Workcenter filter — viewing all requests from a specific workcenter.
CREATE INDEX `idx_WaitlistEntries_WorkcenterName`
    ON `WaitlistEntries` (`WorkcenterName`);

-- Date-range queries — reporting views filtering by RequestedAt range.
CREATE INDEX `idx_WaitlistEntries_RequestedAt`
    ON `WaitlistEntries` (`RequestedAt`);

-- FK columns — MySQL requires explicit indexes for FK performance.
CREATE INDEX `idx_WaitlistEntries_AssignedToUserId`
    ON `WaitlistEntries` (`AssignedToUserId`);

CREATE INDEX `idx_WaitlistEntries_CreatedByUserId`
    ON `WaitlistEntries` (`CreatedByUserId`);

CREATE INDEX `idx_WaitlistEntries_UpdatedByUserId`
    ON `WaitlistEntries` (`UpdatedByUserId`);
