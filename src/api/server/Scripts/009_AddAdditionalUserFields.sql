-- Migration: Add Additional User Fields
-- This migration adds fields that will be collected after registration
-- for compliance with MASAK regulations and security requirements

-- İkamet Adresi (MASAK yükümlülükleri için)
ALTER TABLE users ADD COLUMN address TEXT;

-- Banka Hesap Bilgileri - Para çekme işlemleri için (MASAK, 5549 sayılı Kanun)
ALTER TABLE users ADD COLUMN iban VARCHAR(34);

-- IP adresi ve cihaz bilgisi - Dolandırıcılık tespiti ve güvenlik için (BTK düzenlemeleri)
ALTER TABLE users ADD COLUMN registration_ip INET;
ALTER TABLE users ADD COLUMN last_login_ip INET;
ALTER TABLE users ADD COLUMN device_info JSONB;

-- Güvenlik ve izleme için ek alanlar
ALTER TABLE users ADD COLUMN last_login_at TIMESTAMP;
ALTER TABLE users ADD COLUMN failed_login_attempts INTEGER DEFAULT 0;
ALTER TABLE users ADD COLUMN account_locked_until TIMESTAMP;

-- İndeksler - performans için
CREATE INDEX idx_users_registration_ip ON users(registration_ip);
CREATE INDEX idx_users_last_login_ip ON users(last_login_ip);
CREATE INDEX idx_users_last_login_at ON users(last_login_at);
CREATE INDEX idx_users_account_locked_until ON users(account_locked_until); 