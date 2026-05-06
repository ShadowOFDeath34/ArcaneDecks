-- Migration: 0002_iap_receipts_enhanced
-- Created: 2026-05-06
-- Purpose: Expand iap_receipts with RevenueCat validation fields and fix player_id nullability risk.

-- Allow player_id to be nullable so unknown devices can still log receipts (orphan receipts tracked separately)
ALTER TABLE iap_receipts
    ALTER COLUMN player_id DROP NOT NULL;

-- RevenueCat REST API validation result
ALTER TABLE iap_receipts
    ADD COLUMN IF NOT EXISTS is_validated BOOLEAN NOT NULL DEFAULT false;

-- Monetary details from RevenueCat response
ALTER TABLE iap_receipts
    ADD COLUMN IF NOT EXISTS price DECIMAL(10, 2);

ALTER TABLE iap_receipts
    ADD COLUMN IF NOT EXISTS currency TEXT;

-- Environment: SANDBOX or PRODUCTION
ALTER TABLE iap_receipts
    ADD COLUMN IF NOT EXISTS environment TEXT;

-- Parsed RevenueCat API response payload (not the raw webhook body)
ALTER TABLE iap_receipts
    ADD COLUMN IF NOT EXISTS revcat_payload JSONB;

-- Index for validation status analytics
CREATE INDEX IF NOT EXISTS idx_iap_receipts_is_validated ON iap_receipts(is_validated);
