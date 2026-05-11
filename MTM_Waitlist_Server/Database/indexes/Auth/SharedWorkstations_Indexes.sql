-- =============================================================
-- MTM Waitlist Application — SharedWorkstations Indexes
-- Domain:      Auth
-- Description: Indexes for shared workstation lookups. The UNIQUE constraint
--              on WindowsUsername already creates an implicit index; the
--              IsActive index supports filtering to only active workstations.
-- Depends on:  schema/tables/Auth/SharedWorkstations.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

-- Active workstation filter — the API checks IsActive = 1 when deciding
-- whether to enforce credential login.
CREATE INDEX `idx_SharedWorkstations_IsActive`
    ON `SharedWorkstations` (`IsActive`);
