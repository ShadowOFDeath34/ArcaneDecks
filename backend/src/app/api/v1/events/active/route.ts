import { NextRequest, NextResponse } from "next/server";
import { createClient } from "@supabase/supabase-js";

const supabase = createClient(
  process.env.SUPABASE_URL!,
  process.env.SUPABASE_SERVICE_ROLE_KEY!
);

export async function GET(_req: NextRequest) {
  const now = new Date().toISOString();

  const { data, error } = await supabase
    .from("seasonal_events")
    .select("id, event_key, name, description, start_at, end_at, rules_json, reward_teeth, reward_card_id")
    .eq("is_active", true)
    .lte("start_at", now)
    .gte("end_at", now)
    .order("start_at", { ascending: false });

  if (error) {
    return NextResponse.json({ error: "Query failed" }, { status: 500 });
  }

  return NextResponse.json({ events: data ?? [] });
}
