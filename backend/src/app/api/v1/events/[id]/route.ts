import { NextRequest, NextResponse } from "next/server";
import { jwtVerify } from "jose";
import { createClient } from "@supabase/supabase-js";

const JWT_SECRET = new TextEncoder().encode(process.env.JWT_SECRET!);
const supabase = createClient(
  process.env.SUPABASE_URL!,
  process.env.SUPABASE_SERVICE_ROLE_KEY!
);

async function getPlayerId(req: NextRequest): Promise<string | null> {
  const auth = req.headers.get("authorization");
  const token = auth?.startsWith("Bearer ") ? auth.slice(7) : null;
  if (!token) return null;
  try {
    const { payload } = await jwtVerify(token, JWT_SECRET, { clockTolerance: 60 });
    return payload.sub as string;
  } catch {
    return null;
  }
}

export async function GET(
  req: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;
  const { searchParams } = new URL(req.url);
  const limit = Math.min(parseInt(searchParams.get("limit") ?? "50", 10), 100);

  // Fetch event details
  const { data: eventData, error: eventError } = await supabase
    .from("seasonal_events")
    .select("id, event_key, name, description, start_at, end_at, rules_json, reward_teeth, reward_card_id")
    .eq("id", id)
    .single();

  if (eventError || !eventData) {
    return NextResponse.json({ error: "Event not found" }, { status: 404 });
  }

  // Fetch leaderboard for this event
  const { data: entries, error: entriesError } = await supabase
    .from("seasonal_event_entries")
    .select("best_score, best_floor, runs_completed, updated_at, players(device_id)")
    .eq("event_id", id)
    .order("best_score", { ascending: false })
    .limit(limit);

  if (entriesError) {
    return NextResponse.json({ error: "Leaderboard query failed" }, { status: 500 });
  }

  return NextResponse.json({
    event: eventData,
    leaderboard: entries ?? [],
  });
}

export async function POST(
  req: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const playerId = await getPlayerId(req);
  if (!playerId) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  const { id } = await params;
  const body = (await req.json().catch(() => ({}))) as {
    score?: number;
    floor?: number;
  };

  const score = typeof body.score === "number" ? Math.max(0, Math.floor(body.score)) : 0;
  const floor = typeof body.floor === "number" ? Math.max(0, Math.floor(body.floor)) : 0;

  // Verify event exists and is active
  const now = new Date().toISOString();
  const { data: eventData, error: eventError } = await supabase
    .from("seasonal_events")
    .select("id")
    .eq("id", id)
    .eq("is_active", true)
    .lte("start_at", now)
    .gte("end_at", now)
    .single();

  if (eventError || !eventData) {
    return NextResponse.json({ error: "Event not found or inactive" }, { status: 400 });
  }

  // Upsert entry: keep best score / best floor, increment runs_completed
  const { data: existing } = await supabase
    .from("seasonal_event_entries")
    .select("best_score, best_floor, runs_completed")
    .eq("player_id", playerId)
    .eq("event_id", id)
    .single();

  const newScore = Math.max(existing?.best_score ?? 0, score);
  const newFloor = Math.max(existing?.best_floor ?? 0, floor);
  const newRuns = (existing?.runs_completed ?? 0) + 1;

  const { error } = await supabase
    .from("seasonal_event_entries")
    .upsert(
      {
        player_id: playerId,
        event_id: id,
        best_score: newScore,
        best_floor: newFloor,
        runs_completed: newRuns,
        updated_at: now,
      },
      { onConflict: "player_id, event_id" }
    );

  if (error) {
    return NextResponse.json({ error: "Submit failed" }, { status: 500 });
  }

  return NextResponse.json({
    success: true,
    bestScore: newScore,
    bestFloor: newFloor,
    runsCompleted: newRuns,
  });
}
