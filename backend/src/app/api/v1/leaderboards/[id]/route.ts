import { NextRequest, NextResponse } from 'next/server';
import { supabase } from '@/lib/supabase';

export async function GET(
  _req: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;

  const { data, error } = await supabase
    .from('leaderboard_entries')
    .select('*')
    .eq('leaderboard_id', id)
    .order('score', { ascending: false })
    .limit(100);

  if (error) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }

  return NextResponse.json({ data });
}

export async function POST(
  req: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;
  const body = await req.json().catch(() => null);

  if (!body || typeof body !== 'object' || typeof body.score !== 'number') {
    return NextResponse.json({ error: 'Invalid body' }, { status: 400 });
  }

  const payload = {
    leaderboard_id: id,
    player_id: body.playerId ?? 'anonymous',
    player_name: body.playerName ?? 'Unknown',
    score: body.score,
    floor: body.floor ?? 0,
    created_at: new Date().toISOString(),
  };

  const { error } = await supabase.from('leaderboard_entries').insert(payload);

  if (error) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }

  return NextResponse.json({ success: true });
}
