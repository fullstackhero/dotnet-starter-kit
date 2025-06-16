-- Migration: 005_AddMemberNumber
-- Date: 2024-12-XX
-- Description: Add member_number field to users table

-- Add member_number column
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS member_number VARCHAR(20) UNIQUE;

-- Create index for member_number for performance
CREATE INDEX IF NOT EXISTS idx_users_member_number ON users(member_number);

-- Create a function to generate member number
CREATE OR REPLACE FUNCTION generate_member_number()
RETURNS VARCHAR(20) AS $$
DECLARE
    today_date VARCHAR(8);
    daily_count INTEGER;
    member_number VARCHAR(20);
BEGIN
    -- Get today's date in YYYYMMDD format
    today_date := TO_CHAR(CURRENT_DATE, 'YYYYMMDD');
    
    -- Count how many members registered today
    SELECT COALESCE(COUNT(*), 0) + 1 
    INTO daily_count
    FROM users 
    WHERE users.member_number LIKE today_date || '%'
    OR (users.member_number IS NULL AND DATE(users.created_at) = CURRENT_DATE);
    
    -- Generate member number: YYYYMMDDNNNN (e.g., 202412150001)
    member_number := today_date || LPAD(daily_count::TEXT, 4, '0');
    
    RETURN member_number;
END;
$$ LANGUAGE plpgsql;

-- Update existing users to have member numbers (optional - if you want to assign to existing users)
-- UPDATE users 
-- SET member_number = generate_member_number()
-- WHERE member_number IS NULL;

-- Create a trigger to automatically generate member number for new users
CREATE OR REPLACE FUNCTION set_member_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.member_number IS NULL THEN
        NEW.member_number := generate_member_number();
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_set_member_number
    BEFORE INSERT ON users
    FOR EACH ROW
    EXECUTE FUNCTION set_member_number(); 