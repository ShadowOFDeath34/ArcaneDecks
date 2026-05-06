import { NextRequest, NextResponse } from "next/server";
import { jwtVerify } from "jose";
import { createClient } from "@supabase/supabase-js";
import { captureEvent } from "@/lib/posthog";

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

export async function GET(req: NextRequest) {
  const playerId = await getPlayerId(req);
  if (!playerId) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  const { data } = await supabase
    .from("player_progress")
    .select("run_state, meta_progress, updated_at")
    .eq("player_id", playerId)
    .single();

  captureEvent(playerId, "progress_loaded", { has_data: !!data });

  return NextResponse.json(data ?? { run_state: {}, meta_progress: {} });
}

export async function POST(req: NextRequest) {
  const playerId = await getPlayerId(req);
  if (!playerId) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  const body = (await req.json().catch(() => ({}))) as {
    run_state?: Record<string, unknown>;
    meta_progress?: Record<string, unknown>;
  };

  const { error } = await supabase
    .from("player_progress")
    .upsert(
      {
        player_id: playerId,
        run_state: body.run_state ?? {},
        meta_progress: body.meta_progress ?? {},
        updated_at: new Date().toISOString(),
      },
      { onConflict: "player_id" }
    );

  if (error) {
    return NextResponse.json({ error: "Save failed" }, { status: 500 });
  }

  captureEvent(playerId, "progress_saved", {
    has_run_state: !!body.run_state,
    has_meta_progress: !!body.meta_progress,
  });

  return NextResponse.json({ success: true });
}
