-- Migration: 0003_seasonal_events
-- Created: 2026-05-06
-- Purpose: Seasonal event system — time-boxed challenges with modifiers, leaderboards, and rewards.

-- Event definitions (managed by admin/ops)
CREATE TABLE IF NOT EXISTS seasonal_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_key TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    description TEXT,
    start_at TIMESTAMPTZ NOT NULL,
    end_at TIMESTAMPTZ NOT NULL,
    rules_json JSONB NOT NULL DEFAULT '{}',
    reward_teeth INTEGER NOT NULL DEFAULT 0,
    reward_card_id TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_seasonal_events_active_time
    ON seasonal_events (is_active, start_at, end_at);

-- Player participation / best scores per event
CREATE TABLE IF NOT EXISTS seasonal_event_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    event_id UUID NOT NULL REFERENCES seasonal_events(id) ON DELETE CASCADE,
    best_score INTEGER NOT NULL DEFAULT 0,
    best_floor INTEGER NOT NULL DEFAULT 0,
    runs_completed INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (player_id, event_id)
);

CREATE INDEX IF NOT EXISTS idx_seasonal_event_entries_event_score
    ON seasonal_event_entries (event_id, best_score DESC);

CREATE INDEX IF NOT EXISTS idx_seasonal_event_entries_player
    ON seasonal_event_entries (player_id);

-- Reward claims (prevents double-claiming)
CREATE TABLE IF NOT EXISTS seasonal_event_claims (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    event_id UUID NOT NULL REFERENCES seasonal_events(id) ON DELETE CASCADE,
    claimed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (player_id, event_id)
);

CREATE INDEX IF NOT EXISTS idx_seasonal_event_claims_player
    ON seasonal_event_claims (player_id);
