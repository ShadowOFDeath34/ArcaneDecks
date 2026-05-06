-- Migration: 0001_init
-- Created: 2026-05-06

-- Players table (anonymous auth)
CREATE TABLE IF NOT EXISTS players (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id TEXT NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_seen_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_players_device_id ON players(device_id);

-- Player progress (cloud save)
CREATE TABLE IF NOT EXISTS player_progress (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    run_state JSONB NOT NULL DEFAULT '{}',
    meta_progress JSONB NOT NULL DEFAULT '{}',
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_player_progress_player_id ON player_progress(player_id);

-- Leaderboard entries
CREATE TABLE IF NOT EXISTS leaderboard_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    leaderboard_id TEXT NOT NULL,
    player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    score INTEGER NOT NULL,
    run_data JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_leaderboard_entries_leaderboard_id_score ON leaderboard_entries(leaderboard_id, score DESC);
CREATE INDEX IF NOT EXISTS idx_leaderboard_entries_player_id ON leaderboard_entries(player_id);

-- IAP receipts (idempotency + validation log)
CREATE TABLE IF NOT EXISTS iap_receipts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    product_id TEXT NOT NULL,
    transaction_id TEXT NOT NULL UNIQUE,
    platform TEXT NOT NULL,
    raw_payload JSONB,
    validated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_iap_receipts_player_id ON iap_receipts(player_id);
