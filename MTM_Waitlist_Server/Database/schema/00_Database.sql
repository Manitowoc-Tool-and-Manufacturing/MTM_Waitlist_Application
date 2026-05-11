-- =============================================================
-- MTM Waitlist Application — Database Initialization
-- Server:   172.16.1.104 (internal work network only — not public)
-- Version:  MySQL 5.7
-- Charset:  utf8mb4 / utf8mb4_unicode_ci
-- IMPORTANT: All MySQL database names are lowercase to prevent
--            case-sensitivity issues across platforms.
--
-- Execution order (run each group in order):
--   1. schema/00_Database.sql                            ← this file
--   2. schema/tables/Auth/Users.sql
--   3. schema/tables/Auth/SharedWorkstations.sql
--   4. schema/tables/Auth/RefreshTokens.sql
--   5. schema/tables/Waitlist/WaitlistEntries.sql
--   6. indexes/Auth/Users_Indexes.sql
--   7. indexes/Auth/SharedWorkstations_Indexes.sql
--   8. indexes/Auth/RefreshTokens_Indexes.sql
--   9. indexes/Waitlist/WaitlistEntries_Indexes.sql
--  10. triggers/Auth/*.sql
--  11. triggers/Waitlist/*.sql
--  12. procedures/Auth/*.sql
--  13. procedures/Waitlist/*.sql
--  14. seed/*.sql                                        ← development only
--
-- All-in-one: migrations/V001__Initial_Schema.sql
-- =============================================================

CREATE DATABASE IF NOT EXISTS `mtm_waitlist`
    CHARACTER SET utf8mb4
    COLLATE      utf8mb4_unicode_ci
    COMMENT      'MTM Waitlist Application — internal work-network database';

USE `mtm_waitlist`;
