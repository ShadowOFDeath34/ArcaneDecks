import { NextRequest, NextResponse } from 'next/server';
import { jwtVerify } from 'jose';
import { supabase } from '@/lib/supabase';

const JWT_SECRET = new TextEncoder().encode(process.env.JWT_SECRET!);

async function verifyToken(req: NextRequest) {
  const auth = req.headers.get('authorization');
  if (!auth?.startsWith('Bearer ')) return null;
  try {
    const { payload } = await jwtVerify(auth.slice(7), JWT_SECRET);
    return payload.sub ?? null;
  } catch {
    return null;
  }
}

export async function GET(req: NextRequest) {
  const playerId = await verifyToken(req);
  if (!playerId) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const { data, error } = await supabase
    .from('player_progress')
    .select('*')
    .eq('player_id', playerId)
    .single();

  if (error && error.code !== 'PGRST116') {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }

  return NextResponse.json({ data: data ?? null });
}

export async function POST(req: NextRequest) {
  const playerId = await verifyToken(req);
  if (!playerId) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const body = await req.json().catch(() => null);
  if (!body || typeof body !== 'object') {
    return NextResponse.json({ error: 'Invalid body' }, { status: 400 });
  }

  const payload = {
    player_id: playerId,
    gold: body.gold ?? 0,
    highest_floor: body.highestFloor ?? 0,
    cards_unlocked: body.cardsUnlocked ?? [],
    meta_upgrades: body.metaUpgrades ?? {},
    updated_at: new Date().toISOString(),
  };

  const { error } = await supabase
    .from('player_progress')
    .upsert(payload, { onConflict: 'player_id' });

  if (error) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }

  return NextResponse.json({ success: true });
}
