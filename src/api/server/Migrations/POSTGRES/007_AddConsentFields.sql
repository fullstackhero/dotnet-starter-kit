-- Migration: Add consent fields to users table
-- Date: 2024-12-19
-- Description: Add marketing consent, electronic communication consent, and membership agreement approval fields

ALTER TABLE users 
ADD COLUMN marketing_consent BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN electronic_communication_consent BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN membership_agreement_consent BOOLEAN NOT NULL DEFAULT FALSE;

-- Add comments for clarity
COMMENT ON COLUMN users.marketing_consent IS 'User consent for marketing communications';
COMMENT ON COLUMN users.electronic_communication_consent IS 'User consent for electronic communications (email, SMS)';
COMMENT ON COLUMN users.membership_agreement_consent IS 'User acceptance of membership agreement terms'; 