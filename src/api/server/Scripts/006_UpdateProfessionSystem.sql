-- Migration: Update Profession System from String to ID-based
-- This migration converts the profession system from string-based to ID-based with foreign key relationships

-- Create professions table
CREATE TABLE IF NOT EXISTS professions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Add profession_id column to users table
ALTER TABLE users ADD COLUMN IF NOT EXISTS profession_id INTEGER;

-- Create foreign key constraint
ALTER TABLE users ADD CONSTRAINT fk_users_profession 
    FOREIGN KEY (profession_id) REFERENCES professions(id);

-- Create index for better performance
CREATE INDEX IF NOT EXISTS idx_users_profession_id ON users(profession_id);

-- Drop old profession column (if exists)
ALTER TABLE users DROP COLUMN IF EXISTS profession; 