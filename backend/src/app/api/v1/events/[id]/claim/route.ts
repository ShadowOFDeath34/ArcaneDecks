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

export async function POST(
  req: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const playerId = await getPlayerId(req);
  if (!playerId) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  const { id } = await params;

  // Verify event exists and is active
  const now = new Date().toISOString();
  const { data: eventData, error: eventError } = await supabase
    .from("seasonal_events")
    .select("id, reward_teeth, reward_card_id")
    .eq("id", id)
    .eq("is_active", true)
    .lte("start_at", now)
    .gte("end_at", now)
    .single();

  if (eventError || !eventData) {
    return NextResponse.json({ error: "Event not found or inactive" }, { status: 400 });
  }

  // Check if already claimed
  const { data: existingClaim } = await supabase
    .from("seasonal_event_claims")
    .select("id")
    .eq("player_id", playerId)
    .eq("event_id", id)
    .single();

  if (existingClaim) {
    return NextResponse.json({ error: "Reward already claimed" }, { status: 409 });
  }

  // Record claim
  const { error: claimError } = await supabase
    .from("seasonal_event_claims")
    .insert({ player_id: playerId, event_id: id, claimed_at: now });

  if (claimError) {
    return NextResponse.json({ error: "Claim failed" }, { status: 500 });
  }

  return NextResponse.json({
    success: true,
    rewardTeeth: eventData.reward_teeth,
    rewardCardId: eventData.reward_card_id,
  });
}
