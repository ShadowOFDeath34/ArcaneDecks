create table if not exists player_progress (
  player_id text primary key,
  gold int not null default 0,
  highest_floor int not null default 0,
  cards_unlocked jsonb not null default '[]'::jsonb,
  meta_upgrades jsonb not null default '{}'::jsonb,
  updated_at timestamptz not null default now()
);

create table if not exists leaderboard_entries (
  id bigserial primary key,
  leaderboard_id text not null,
  player_id text not null,
  player_name text not null,
  score int not null,
  floor int not null default 0,
  created_at timestamptz not null default now()
);

create table if not exists iap_events (
  id bigserial primary key,
  player_id text not null,
  event_type text not null,
  product_id text not null,
  raw_event jsonb not null,
  created_at timestamptz not null default now()
);

create index if not exists idx_leaderboard_entries_leaderboard_id_score
  on leaderboard_entries (leaderboard_id, score desc);

create index if not exists idx_leaderboard_entries_player_id
  on leaderboard_entries (player_id);

create index if not exists idx_iap_events_player_id
  on iap_events (player_id);

create index if not exists idx_iap_events_created_at
  on iap_events (created_at);
