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

  const { data, error } = await supabase
    .from("leaderboard_entries")
    .select("score, run_data, created_at, players(device_id)")
    .eq("leaderboard_id", id)
    .order("score", { ascending: false })
    .limit(limit);

  if (error) {
    return NextResponse.json({ error: "Query failed" }, { status: 500 });
  }

  return NextResponse.json({ leaderboardId: id, entries: data ?? [] });
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
    run_data?: Record<string, unknown>;
  };

  const score = typeof body.score === "number" ? Math.max(0, Math.floor(body.score)) : 0;

  const { error } = await supabase.from("leaderboard_entries").insert({
    leaderboard_id: id,
    player_id: playerId,
    score,
    run_data: body.run_data ?? {},
  });

  if (error) {
    return NextResponse.json({ error: "Submit failed" }, { status: 500 });
  }

  return NextResponse.json({ success: true, score });
}
