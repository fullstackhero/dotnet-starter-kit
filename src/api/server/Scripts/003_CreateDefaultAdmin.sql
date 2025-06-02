-- Migration: 003_CreateDefaultAdmin
-- Date: 2024-12-XX
-- Description: Create default admin user for bootstrapping

-- Insert default admin user
-- Password: Admin123! (BCrypt hashed)
-- TCKN: 24256590788 (Default admin TCKN)
INSERT INTO users (id, email, phone_number, tckn, password_hash, first_name, last_name, birth_date, is_identity_verified, is_phone_verified, is_email_verified, status, created_at, updated_at)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'admin@system.com',
    '00000000000',
    '24256590788',
    '$2a$11$8Z9Qz4K7n5pX2a8W1lY5yOzQ3nJmM2K5L4oP9bV8cR7dU1xS6tN3y', -- BCrypt hash of 'Admin123!'
    'System',
    'Administrator',
    '1990-01-01',
    true,
    true,
    true,
    'ACTIVE',
    now(),
    now()
) ON CONFLICT (email) DO NOTHING;

-- Assign admin role to default admin user
INSERT INTO user_roles (user_id, role_id, assigned_at)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'admin',
    now()
) ON CONFLICT (user_id, role_id) DO NOTHING; 