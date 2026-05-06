import { NextRequest, NextResponse } from "next/server";
import { createClient } from "@supabase/supabase-js";
import { captureEvent } from "@/lib/posthog";

const supabase = createClient(
  process.env.SUPABASE_URL!,
  process.env.SUPABASE_SERVICE_ROLE_KEY!
);

const RC_API_KEY = process.env.REVENUECAT_API_KEY;
const RC_VERSION = process.env.REVENUECAT_API_VERSION ?? "v2";

const VALID_EVENT_TYPES = [
  "INITIAL_PURCHASE",
  "RENEWAL",
  "NON_RENEWING_PURCHASE",
];

interface RevenueCatTransaction {
  id?: string;
  type?: string;
  attributes?: {
    product_id?: string;
    price?: number;
    currency?: string;
    store?: string;
    environment?: string;
    purchased_at_ms?: number;
  };
}

/**
 * Validate a transaction against RevenueCat REST API v2.
 * Returns parsed transaction data if valid, null if invalid or no API key configured.
 */
async function validateTransaction(
  transactionId: string,
  expectedProductId: string
): Promise<{ valid: boolean; data?: RevenueCatTransaction; error?: string }> {
  if (!RC_API_KEY) {
    // No API key configured: accept webhook on trust (not recommended for production)
    return { valid: true };
  }

  const rcUrl = `https://api.revenuecat.com/${RC_VERSION}/transactions/${encodeURIComponent(transactionId)}`;
  const rcRes = await fetch(rcUrl, {
    headers: {
      Authorization: `Bearer ${RC_API_KEY}`,
      "Content-Type": "application/json",
    },
  });

  if (!rcRes.ok) {
    if (rcRes.status === 404) {
      return { valid: false, error: "Transaction not found in RevenueCat" };
    }
    return {
      valid: false,
      error: `RevenueCat API error (${rcRes.status})`,
    };
  }

  let rcData: RevenueCatTransaction;
  try {
    rcData = (await rcRes.json()) as RevenueCatTransaction;
  } catch {
    return { valid: false, error: "Invalid RevenueCat response JSON" };
  }

  const attr = rcData.attributes ?? {};
  const rcProductId = attr.product_id ?? "";

  if (rcProductId !== expectedProductId) {
    return {
      valid: false,
      error: `Product ID mismatch: expected ${expectedProductId}, got ${rcProductId}`,
    };
  }

  return { valid: true, data: rcData };
}

/**
 * Ensure a player row exists for the given device/app_user_id.
 * Returns the player UUID.
 */
async function ensurePlayer(deviceId: string): Promise<string> {
  const { data: existing } = await supabase
    .from("players")
    .select("id")
    .eq("device_id", deviceId)
    .maybeSingle();

  if (existing?.id) {
    // Update last_seen
    await supabase
      .from("players")
      .update({ last_seen_at: new Date().toISOString() })
      .eq("id", existing.id);
    return existing.id;
  }

  // Create orphan player for this device
  const { data: created, error } = await supabase
    .from("players")
    .insert({ device_id: deviceId, last_seen_at: new Date().toISOString() })
    .select("id")
    .single();

  if (error || !created) {
    throw new Error(`Failed to ensure player: ${error?.message ?? "unknown"}`);
  }

  return created.id;
}

export async function POST(req: NextRequest) {
  try {
    const payload = (await req.json().catch(() => ({}))) as Record<
      string,
      unknown
    >;

    const event = (payload.event ?? {}) as Record<string, unknown>;
    const eventType = String(event.type ?? "");
    const productId = String(event.product_id ?? "");
    const transactionId = String(event.transaction_id ?? "");
    const appUserId = String(event.app_user_id ?? "");

    if (!VALID_EVENT_TYPES.includes(eventType)) {
      return NextResponse.json({ ignored: true, reason: "event type" });
    }

    if (!transactionId || !productId) {
      return NextResponse.json(
        { error: "Missing transaction_id or product_id" },
        { status: 400 }
      );
    }

    // Idempotency: skip if already recorded
    const { data: existing } = await supabase
      .from("iap_receipts")
      .select("id")
      .eq("transaction_id", transactionId)
      .maybeSingle();

    if (existing) {
      return NextResponse.json({ success: true, idempotent: true });
    }

    // Validate against RevenueCat REST API v2
    const validation = await validateTransaction(transactionId, productId);
    if (!validation.valid) {
      return NextResponse.json(
        { error: validation.error ?? "Validation failed" },
        { status: 502 }
      );
    }

    // Resolve or create player
    let playerId: string | null = null;
    if (appUserId) {
      try {
        playerId = await ensurePlayer(appUserId);
      } catch {
        playerId = null; // orphan receipt, still recorded
      }
    }

    const attr = validation.data?.attributes ?? {};
    const rawStore = String(event.store ?? attr.store ?? "unknown");
    const rawEnvironment = String(
      event.environment ?? attr.environment ?? "unknown"
    );

    const { error } = await supabase.from("iap_receipts").insert({
      player_id: playerId,
      product_id: productId,
      transaction_id: transactionId,
      platform: rawStore,
      environment: rawEnvironment,
      price: attr.price ?? null,
      currency: attr.currency ?? null,
      is_validated: !!RC_API_KEY, // true only if we actually called RevenueCat
      raw_payload: payload,
      revcat_payload: validation.data ?? null,
      validated_at: new Date().toISOString(),
    });

    if (error) {
      return NextResponse.json(
        { error: "Insert failed", detail: error.message },
        { status: 500 }
      );
    }

    const receiptPlayerId = playerId || "unknown";
    captureEvent(receiptPlayerId, "iap_purchase", {
      product_id: productId,
      transaction_id: transactionId,
      platform: rawStore,
      environment: rawEnvironment,
      validated: !!RC_API_KEY,
    });

    return NextResponse.json({ success: true, validated: !!RC_API_KEY });
  } catch (err) {
    const message = err instanceof Error ? err.message : "Internal error";
    return NextResponse.json(
      { error: "Internal error", detail: message },
      { status: 500 }
    );
  }
}
