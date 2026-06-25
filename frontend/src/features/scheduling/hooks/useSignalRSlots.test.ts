import { renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

const { mockConnection } = vi.hoisted(() => ({
  mockConnection: {
    on: vi.fn(),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
  },
}));

vi.mock('@microsoft/signalr', () => {
  class MockHubConnectionBuilder {
    withUrl = vi.fn().mockReturnThis();
    withAutomaticReconnect = vi.fn().mockReturnThis();
    configureLogging = vi.fn().mockReturnThis();
    build = vi.fn(() => mockConnection);
  }

  return {
    LogLevel: { Warning: 2 },
    HubConnectionBuilder: MockHubConnectionBuilder,
  };
});

vi.mock('../../../shared/api/authInterceptor', () => ({
  getAccessToken: vi.fn(() => 'test-token'),
}));

describe('useSignalRSlots', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('connects, joins groups, handles slot updates, and disconnects on cleanup', async () => {
    const { useSignalRSlots } = await import('./useSignalRSlots');

    const handlers = new Map<string, (payload: unknown) => void>();
    mockConnection.on.mockImplementation((eventName: string, handler: (payload: unknown) => void) => {
      handlers.set(eventName, handler);
    });

    const onSlotUpdate = vi.fn();

    const { unmount } = renderHook(() =>
      useSignalRSlots({
        providerIds: ['provider-1'],
        onSlotUpdate,
        enabled: true,
      }),
    );

    await waitFor(() => {
      expect(mockConnection.start).toHaveBeenCalledTimes(1);
    });

    await waitFor(() => {
      expect(mockConnection.invoke).toHaveBeenCalledWith('JoinProviderGroup', 'provider-1');
    });

    const slotUpdatedHandler = handlers.get('SlotUpdated');
    expect(slotUpdatedHandler).toBeDefined();

    slotUpdatedHandler?.({
      providerId: 'provider-1',
      slotId: 'slot-1',
      isAvailable: false,
    });

    expect(onSlotUpdate).toHaveBeenCalledWith({
      providerId: 'provider-1',
      slotId: 'slot-1',
      isAvailable: false,
    });

    unmount();

    await waitFor(() => {
      expect(mockConnection.stop).toHaveBeenCalledTimes(1);
    });
  });
});
