import { NextRequest, NextResponse } from 'next/server';
import { SignJWT } from 'jose';
import { supabase } from '@/lib/supabase';

const JWT_SECRET = new TextEncoder().encode(process.env.JWT_SECRET!);

export async function POST(req: NextRequest) {
  const body = await req.json().catch(() => null);
  const deviceId = body?.deviceId;

  if (!deviceId || typeof deviceId !== 'string') {
    return NextResponse.json({ error: 'deviceId required' }, { status: 400 });
  }

  const { data: existing } = await supabase
    .from('players')
    .select('id')
    .eq('device_id', deviceId)
    .single();

  let playerId: string;

  if (existing) {
    playerId = existing.id;
  } else {
    playerId = crypto.randomUUID();
    const { error: insertError } = await supabase
      .from('players')
      .insert({ id: playerId, device_id: deviceId, created_at: new Date().toISOString() });

    if (insertError) {
      return NextResponse.json({ error: insertError.message }, { status: 500 });
    }
  }

  const token = await new SignJWT({ sub: playerId })
    .setProtectedHeader({ alg: 'HS256' })
    .setIssuedAt()
    .setExpirationTime('30d')
    .sign(JWT_SECRET);

  return NextResponse.json({ token, playerId });
}
