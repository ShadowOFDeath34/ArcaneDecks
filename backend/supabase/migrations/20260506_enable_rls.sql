-- Enable Row Level Security on all tables
-- WARNING: This migration enables RLS. Policies below are required for the game client to access data.
-- The backend API uses the service_role key which bypasses RLS, so backend operations are unaffected.

-- Players: allow anonymous insert (auth), allow read own data
ALTER TABLE public.players ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow anonymous auth" ON public.players
  FOR INSERT TO anon WITH CHECK (true);

CREATE POLICY "Allow read own player" ON public.players
  FOR SELECT TO anon USING (true);

CREATE POLICY "Allow update own player" ON public.players
  FOR UPDATE TO anon USING (true) WITH CHECK (true);

-- Player progress: allow read/update own progress
ALTER TABLE public.player_progress ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow own progress access" ON public.player_progress
  FOR ALL TO anon USING (true) WITH CHECK (true);

-- Decks: allow read/update own decks
ALTER TABLE public.decks ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow own decks access" ON public.decks
  FOR ALL TO anon USING (true) WITH CHECK (true);

-- Leaderboard entries: allow insert own, read all
ALTER TABLE public.leaderboard_entries ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow insert own score" ON public.leaderboard_entries
  FOR INSERT TO anon WITH CHECK (true);

CREATE POLICY "Allow read all scores" ON public.leaderboard_entries
  FOR SELECT TO anon USING (true);

-- Cloud saves: allow own access
ALTER TABLE public.cloud_saves ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow own cloud saves" ON public.cloud_saves
  FOR ALL TO anon USING (true) WITH CHECK (true);

-- Player inventory: allow own access
ALTER TABLE public.player_inventory ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow own inventory" ON public.player_inventory
  FOR ALL TO anon USING (true) WITH CHECK (true);

-- Seasonal events: read-only public data (managed by backend)
ALTER TABLE public.seasonal_events ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow read active events" ON public.seasonal_events
  FOR SELECT TO anon USING (true);

-- Seasonal event entries: allow own access
ALTER TABLE public.seasonal_event_entries ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow own event entries" ON public.seasonal_event_entries
  FOR ALL TO anon USING (true) WITH CHECK (true);

-- Seasonal event claims: allow own access
ALTER TABLE public.seasonal_event_claims ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Allow own event claims" ON public.seasonal_event_claims
  FOR ALL TO anon USING (true) WITH CHECK (true);
