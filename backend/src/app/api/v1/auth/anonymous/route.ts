import { NextRequest, NextResponse } from "next/server";
import { SignJWT } from "jose";
import { createClient } from "@supabase/supabase-js";

const JWT_SECRET = new TextEncoder().encode(process.env.JWT_SECRET!);
const supabase = createClient(
  process.env.SUPABASE_URL!,
  process.env.SUPABASE_SERVICE_ROLE_KEY!
);

export async function POST(req: NextRequest) {
  try {
    const body = (await req.json().catch(() => ({}))) as { deviceId?: string };
    const deviceId = body.deviceId?.trim();

    if (!deviceId) {
      return NextResponse.json({ error: "deviceId required" }, { status: 400 });
    }

    // Upsert player by device_id
    const { data: existing } = await supabase
      .from("players")
      .select("id")
      .eq("device_id", deviceId)
      .single();

    let playerId: string;

    if (existing?.id) {
      playerId = existing.id;
      await supabase
        .from("players")
        .update({ last_seen_at: new Date().toISOString() })
        .eq("id", playerId);
    } else {
      const { data, error } = await supabase
        .from("players")
        .insert({ device_id: deviceId })
        .select("id")
        .single();
      if (error || !data) {
        return NextResponse.json({ error: "Failed to create player" }, { status: 500 });
      }
      playerId = data.id;
    }

    const token = await new SignJWT({ sub: playerId, did: deviceId })
      .setProtectedHeader({ alg: "HS256" })
      .setIssuedAt()
      .setExpirationTime("30d")
      .sign(JWT_SECRET);

    return NextResponse.json({ token, playerId });
  } catch {
    return NextResponse.json({ error: "Internal error" }, { status: 500 });
  }
}
