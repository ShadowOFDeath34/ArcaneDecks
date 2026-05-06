import { test, expect } from '@playwright/test';

test.describe('Arcane Decks API Flow', () => {
  const deviceId = `e2e-device-${Date.now()}`;
  let token = '';
  let playerId = '';

  test('POST /api/v1/auth/anonymous — returns token and playerId', async ({ request }) => {
    const res = await request.post('/api/v1/auth/anonymous', {
      data: { deviceId },
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.token).toBeDefined();
    expect(body.playerId).toBeDefined();
    token = body.token;
    playerId = body.playerId;
  });

  test('POST /api/v1/auth/anonymous — rejects missing deviceId', async ({ request }) => {
    const res = await request.post('/api/v1/auth/anonymous', {
      data: {},
    });
    expect(res.status()).toBe(400);
    const body = await res.json();
    expect(body.error).toBe('deviceId required');
  });

  test('POST /api/v1/progress — saves run and meta', async ({ request }) => {
    const res = await request.post('/api/v1/progress', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        run_state: { floor: 3, gold: 42 },
        meta_progress: { goblinTeeth: 7 },
      },
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.success).toBe(true);
  });

  test('GET /api/v1/progress — returns saved state', async ({ request }) => {
    const res = await request.get('/api/v1/progress', {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.run_state).toEqual({ floor: 3, gold: 42 });
    expect(body.meta_progress).toEqual({ goblinTeeth: 7 });
  });

  test('GET /api/v1/progress — rejects unauthenticated', async ({ request }) => {
    const res = await request.get('/api/v1/progress');
    expect(res.status()).toBe(401);
  });

  test('POST /api/v1/leaderboards/global — submits score', async ({ request }) => {
    const res = await request.post('/api/v1/leaderboards/global', {
      headers: { Authorization: `Bearer ${token}` },
      data: { score: 1500, run_data: { floor: 5, durationSeconds: 120 } },
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.success).toBe(true);
    expect(body.score).toBe(1500);
  });

  test('GET /api/v1/leaderboards/global — returns entries', async ({ request }) => {
    const res = await request.get('/api/v1/leaderboards/global');
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.leaderboardId).toBe('global');
    expect(Array.isArray(body.entries)).toBe(true);
    const ours = body.entries.find((e: any) => e.players?.device_id === deviceId);
    expect(ours).toBeDefined();
    expect(ours.score).toBe(1500);
  });

  test('GET /api/v1/events/active — returns list (may be empty)', async ({ request }) => {
    const res = await request.get('/api/v1/events/active');
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.events)).toBe(true);
  });

  test('GET /api/v1/events/[id] — returns 404 for unknown event', async ({ request }) => {
    const res = await request.get('/api/v1/events/00000000-0000-0000-0000-000000000000');
    expect(res.status()).toBe(404);
  });

  test('POST /api/v1/events/[id] — rejects inactive event', async ({ request }) => {
    const res = await request.post('/api/v1/events/00000000-0000-0000-0000-000000000000', {
      headers: { Authorization: `Bearer ${token}` },
      data: { score: 999, floor: 3 },
    });
    expect(res.status()).toBe(400);
    const body = await res.json();
    expect(body.error).toBe('Event not found or inactive');
  });

  test('POST /api/v1/events/[id]/claim — rejects inactive event', async ({ request }) => {
    const res = await request.post('/api/v1/events/00000000-0000-0000-0000-000000000000/claim', {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(res.status()).toBe(400);
    const body = await res.json();
    expect(body.error).toBe('Event not found or inactive');
  });
});
