-- =============================================================
-- MTM Waitlist Application — Seed: Users
-- Domain:      Auth
-- Environment: DEVELOPMENT ONLY — DO NOT RUN IN PRODUCTION
--
-- Creates one seed user per role for local development and testing.
-- All seed users use the SAME plaintext password: Admin@MTM2026
--
-- ⚠️ The PasswordHash values below are PLACEHOLDER STRINGS.
--    They will not authenticate. Before using this file:
--    1. Generate real bcrypt hashes in your API project or a bcrypt tool.
--    2. Replace every PLACEHOLDER string with the real hash.
--    3. Never commit real production hashes to source control.
--
-- Example (Node.js):  bcrypt.hashSync('Admin@MTM2026', 12)
-- Example (C#):       BCrypt.Net.BCrypt.HashPassword("Admin@MTM2026", 12)
-- =============================================================

USE `MTM_Waitlist`;

INSERT INTO `Users`
    (`Username`, `PasswordHash`,                      `DisplayName`,       `Role`,    `IsActive`)
VALUES
    ('admin',    '$2a$12$REPLACE_WITH_REAL_BCRYPT_HASH_ADMIN___', 'MTM Administrator', 'Admin',   1),
    ('manager',  '$2a$12$REPLACE_WITH_REAL_BCRYPT_HASH_MANAGER_', 'MTM Manager',       'Manager', 1),
    ('staff1',   '$2a$12$REPLACE_WITH_REAL_BCRYPT_HASH_STAFF1__', 'Staff User One',    'User',    1);
