import { NextRequest, NextResponse } from 'next/server';
import { supabase } from '@/lib/supabase';

export async function POST(req: NextRequest) {
  const secret = req.headers.get('authorization');
  if (secret !== `Bearer ${process.env.REVENUECAT_WEBHOOK_SECRET}`) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const body = await req.json().catch(() => null);
  if (!body || typeof body !== 'object') {
    return NextResponse.json({ error: 'Invalid body' }, { status: 400 });
  }

  const eventType = body.event?.type ?? 'unknown';
  const playerId = body.event?.app_user_id ?? 'unknown';
  const productId = body.event?.product_id ?? 'unknown';

  const payload = {
    player_id: playerId,
    event_type: eventType,
    product_id: productId,
    raw_event: body,
    created_at: new Date().toISOString(),
  };

  const { error } = await supabase.from('iap_events').insert(payload);

  if (error) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }

  return NextResponse.json({ success: true });
}
