-- =============================================================
-- MTM Waitlist Application — Users Indexes
-- Domain:      Auth
-- Description: Additional indexes on Users. The UNIQUE constraints on
--              Username and WindowsUsername already create implicit indexes
--              for those columns — no duplicates needed here.
-- Depends on:  schema/tables/Auth/Users.sql
-- MySQL:       5.7 compatible
-- =============================================================

USE `mtm_waitlist`;

-- Active user lookups — login validation and authorization checks filter on IsActive.
CREATE INDEX `idx_Users_IsActive`
    ON `Users` (`IsActive`);

-- Role-based queries — fetching all users by role (e.g., all MaterialHandlers for assignment).
CREATE INDEX `idx_Users_Role`
    ON `Users` (`Role`);
