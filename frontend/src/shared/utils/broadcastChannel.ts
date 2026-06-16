/**
 * Cross-tab session timer synchronization via BroadcastChannel API.
 * Keeps inactivity timers in sync so any tab's activity resets all timers.
 */

type SessionMessage =
  | { type: 'ACTIVITY_DETECTED' }
  | { type: 'SESSION_EXTENDED' }
  | { type: 'LOGOUT' };

type MessageHandler = (message: SessionMessage) => void;

const CHANNEL_NAME = 'upap-session-sync';

let channel: BroadcastChannel | null = null;
const listeners = new Set<MessageHandler>();

function ensureChannel(): BroadcastChannel | null {
  if (channel) return channel;

  if (typeof BroadcastChannel === 'undefined') return null;

  channel = new BroadcastChannel(CHANNEL_NAME);
  channel.onmessage = (event: MessageEvent<SessionMessage>) => {
    for (const handler of listeners) {
      handler(event.data);
    }
  };

  return channel;
}

export function postSessionMessage(message: SessionMessage): void {
  ensureChannel()?.postMessage(message);
}

export function subscribeSessionChannel(handler: MessageHandler): () => void {
  ensureChannel();
  listeners.add(handler);

  return () => {
    listeners.delete(handler);
    if (listeners.size === 0 && channel) {
      channel.close();
      channel = null;
    }
  };
}
