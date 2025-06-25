-- =====================================================
-- Script: 010_CreatePasswordHistoryTable.sql
-- Description: Creates password_history table for tracking user password changes
-- Author: System
-- Date: 2024-01-15
-- =====================================================

-- Create password_history table
CREATE TABLE IF NOT EXISTS password_history (
    id SERIAL PRIMARY KEY,
    tckn VARCHAR(11) NOT NULL,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Constraints
    CONSTRAINT fk_password_history_tckn FOREIGN KEY (tckn) REFERENCES users(tckn) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_password_history_tckn ON password_history(tckn);
CREATE INDEX IF NOT EXISTS idx_password_history_created_at ON password_history(tckn, created_at DESC);

-- Add comment
COMMENT ON TABLE password_history IS 'Stores password history for users to prevent password reuse';
COMMENT ON COLUMN password_history.tckn IS 'Turkish National ID number';
COMMENT ON COLUMN password_history.password_hash IS 'Hashed password using BCrypt';
COMMENT ON COLUMN password_history.created_at IS 'When the password was changed';

-- Display success message
DO $$
BEGIN
    RAISE NOTICE 'Password history table created successfully';
END $$; 