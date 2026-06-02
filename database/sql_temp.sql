-- Update user password to valid bcrypt hash for testing
UPDATE mtm_waitlist.Users 
SET PasswordHash = '$2a$12$Em6ezTVFDiU8oUhyNmESU.FAUCmW6e23GgwtYuSThKJgSJg8HQFwy'
WHERE Username = 'johnk';