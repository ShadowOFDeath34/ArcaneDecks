create table if not exists players (
  id uuid primary key default gen_random_uuid(),
  device_id text unique not null,
  created_at timestamptz default now()
);

create table if not exists player_progress (
  player_id uuid primary key references players(id) on delete cascade,
  gold int default 0,
  highest_floor int default 0,
  cards_unlocked text[] default '{}',
  meta_upgrades jsonb default '{}',
  updated_at timestamptz default now()
);

create table if not exists leaderboard_entries (
  id uuid primary key default gen_random_uuid(),
  leaderboard_id text not null,
  player_id text not null,
  player_name text not null default 'Unknown',
  score int not null default 0,
  floor int not null default 0,
  created_at timestamptz default now()
);

create table if not exists iap_events (
  id uuid primary key default gen_random_uuid(),
  player_id text not null,
  event_type text not null,
  product_id text not null,
  raw_event jsonb,
  created_at timestamptz default now()
);

create index idx_leaderboard_entries_id_score on leaderboard_entries(leaderboard_id, score desc);
create index idx_iap_events_player_id on iap_events(player_id);
