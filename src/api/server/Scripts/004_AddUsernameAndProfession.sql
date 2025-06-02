-- Migration: 004_AddUsernameAndProfession
-- Date: 2024-12-XX
-- Description: Add username and profession fields to users table

-- Add username and profession columns
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS username VARCHAR(50) UNIQUE,
ADD COLUMN IF NOT EXISTS profession VARCHAR(100);

-- Create index for username
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- Update existing users to have username based on email prefix
UPDATE users 
SET username = split_part(email, '@', 1) 
WHERE username IS NULL;

-- Make username NOT NULL after setting values
-- ALTER TABLE users ALTER COLUMN username SET NOT NULL;

-- Add constraint to ensure username is unique and not empty
-- ALTER TABLE users ADD CONSTRAINT chk_username_not_empty CHECK (username != ''); 