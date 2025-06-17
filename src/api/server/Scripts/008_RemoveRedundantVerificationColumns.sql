-- Migration: Remove Redundant Verification Columns
-- This migration removes is_identity_verified and is_phone_verified columns
-- since both verifications happen during registration flow:
-- - MERNIS verification: Done during registration, no need to store flag
-- - SMS OTP verification: Done during registration, user created only after verification
-- Keep only is_email_verified for post-registration email verification

-- Remove is_identity_verified column (MERNIS verification is done during registration)
ALTER TABLE users DROP COLUMN IF EXISTS is_identity_verified;

-- Remove is_phone_verified column (SMS OTP verification is done during registration)
ALTER TABLE users DROP COLUMN IF EXISTS is_phone_verified;

-- Keep only is_email_verified for post-registration email verification 