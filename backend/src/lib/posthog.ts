import { PostHog } from "posthog-node";

const posthog = new PostHog(process.env.POSTHOG_API_KEY || "", {
  host: process.env.POSTHOG_HOST || "https://eu.i.posthog.com",
  flushAt: 1,
  flushInterval: 0,
});

export function captureEvent(
  distinctId: string,
  event: string,
  properties?: Record<string, unknown>
) {
  if (!process.env.POSTHOG_API_KEY) return;
  posthog.capture({ distinctId, event, properties });
}

export function shutdownPostHog() {
  return posthog.shutdown();
}

export default posthog;
