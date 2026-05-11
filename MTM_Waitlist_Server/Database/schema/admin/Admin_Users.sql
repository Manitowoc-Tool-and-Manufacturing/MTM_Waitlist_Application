-- =============================================================
-- MTM Waitlist Application -- Admin MySQL Users
-- Description: Creates the two dedicated MySQL users required by
--              the Server Admin application.
--              Run ONCE manually as a MySQL root-level user on
--              the server (172.16.1.104).
--              Passwords must be set before running — never commit
--              real passwords to source control.
-- MySQL:       5.7 compatible
-- =============================================================

-- App user: used by the REST API for all client-facing requests.
-- Minimal privileges: EXECUTE (stored procedures) + SELECT on app tables.
CREATE USER IF NOT EXISTS 'waitlist_admin_dbappuser'@'localhost'
    IDENTIFIED BY '*** SET STRONG PASSWORD HERE ***';

GRANT EXECUTE, SELECT
    ON `mtm_waitlist`.* TO 'waitlist_admin_dbappuser'@'localhost';

-- Updater user: used by the admin app for dashboard, backup, and migrations.
-- Elevated privileges required for monitoring and maintenance operations.
CREATE USER IF NOT EXISTS 'waitlist_admin_dbupdater'@'localhost'
    IDENTIFIED BY '*** SET STRONG PASSWORD HERE ***';

-- PROCESS    : required for SHOW FULL PROCESSLIST (dashboard active connections)
-- REPLICATION CLIENT : required for SHOW MASTER STATUS (backup metadata)
-- KILL       : required for session termination from dashboard
GRANT SELECT, PROCESS, REPLICATION CLIENT, KILL
    ON *.* TO 'waitlist_admin_dbupdater'@'localhost';

GRANT ALL PRIVILEGES
    ON `mtm_waitlist`.* TO 'waitlist_admin_dbupdater'@'localhost';

FLUSH PRIVILEGES;
