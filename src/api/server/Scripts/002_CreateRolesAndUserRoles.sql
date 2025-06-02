-- Migration: 002_CreateRolesAndUserRoles
-- Date: 2024-12-XX
-- Description: Create roles and user_roles tables

-- Create roles table
CREATE TABLE IF NOT EXISTS roles (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP DEFAULT now()
);

-- Create user_roles junction table
CREATE TABLE IF NOT EXISTS user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id VARCHAR(50) NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMP DEFAULT now(),
    assigned_by UUID REFERENCES users(id),
    PRIMARY KEY (user_id, role_id)
);

-- Insert predefined roles
INSERT INTO roles (id, name, description) VALUES 
('admin', 'Administrator', 'Full system access with all permissions'),
('customer_admin', 'Customer Administrator', 'Customer organization admin with user management'),
('customer_support', 'Customer Support', 'Customer support with limited user assistance'),
('base_user', 'Base User', 'Standard user with basic access')
ON CONFLICT (id) DO NOTHING;

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON user_roles(role_id); 