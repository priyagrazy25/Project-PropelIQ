import { useEffect, useRef, useCallback, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { getAccessToken } from '../../../shared/api/authInterceptor';
import type { ProviderSlot, SwapExecutedEvent, WaitlistAvailableEvent } from '../api/schedulingApi';

export interface SlotUpdate {
  providerId: string;
  slotId: string;
  isAvailable: boolean;
}

interface UseSignalRSlotsOptions {
  providerIds: string[];
  onSlotUpdate: (update: SlotUpdate) => void;
  onSwapExecuted?: (event: SwapExecutedEvent) => void;
  onWaitlistAvailable?: (event: WaitlistAvailableEvent) => void;
  onReconnected?: () => void;
  enabled: boolean;
}

type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

const HUB_URL = '/hubs/appointments';
const MAX_RECONNECT_ATTEMPTS = 5;

export function useSignalRSlots({ providerIds, onSlotUpdate, onSwapExecuted, onWaitlistAvailable, onReconnected, enabled }: UseSignalRSlotsOptions): {
  connectionState: ConnectionState;
} {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const joinedGroupsRef = useRef<Set<string>>(new Set());
  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected');
  const onSlotUpdateRef = useRef(onSlotUpdate);
  useEffect(() => {
    onSlotUpdateRef.current = onSlotUpdate;
  }, [onSlotUpdate]);
  const onReconnectedRef = useRef(onReconnected);
  useEffect(() => {
    onReconnectedRef.current = onReconnected;
  }, [onReconnected]);
  const onSwapExecutedRef = useRef(onSwapExecuted);
  useEffect(() => {
    onSwapExecutedRef.current = onSwapExecuted;
  }, [onSwapExecuted]);
  const onWaitlistAvailableRef = useRef(onWaitlistAvailable);
  useEffect(() => {
    onWaitlistAvailableRef.current = onWaitlistAvailable;
  }, [onWaitlistAvailable]);

  const buildConnection = useCallback(() => {
    return new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= MAX_RECONNECT_ATTEMPTS) {
            return null;
          }
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }, []);

  useEffect(() => {
    if (!enabled || providerIds.length === 0) {
      return;
    }

    const connection = buildConnection();
    connectionRef.current = connection;

    connection.on('SlotUpdated', (update: SlotUpdate) => {
      onSlotUpdateRef.current(update);
    });

    connection.on('SlotBooked', (data: { providerId: string; slotId: string }) => {
      onSlotUpdateRef.current({ ...data, isAvailable: false });
    });

    connection.on('SlotReleased', (data: { providerId: string; slotId: string }) => {
      onSlotUpdateRef.current({ ...data, isAvailable: true });
    });

    connection.on('SwapExecuted', (data: SwapExecutedEvent) => {
      onSwapExecutedRef.current?.(data);
    });

    connection.on('WaitlistAvailable', (data: WaitlistAvailableEvent) => {
      onWaitlistAvailableRef.current?.(data);
    });

    connection.onreconnecting(() => {
      setConnectionState('reconnecting');
    });

    connection.onreconnected(() => {
      setConnectionState('connected');
      // Rejoin groups after reconnect
      for (const id of joinedGroupsRef.current) {
        void connection.invoke('JoinProviderGroup', id);
      }
      onReconnectedRef.current?.();
    });

    connection.onclose(() => {
      setConnectionState('disconnected');
    });

    const groups = joinedGroupsRef.current;

    void connection
      .start()
      .then(async () => {
        for (const id of providerIds) {
          await connection.invoke('JoinProviderGroup', id);
          groups.add(id);
        }
      })
      .catch(() => {
        // Connection failed — state handled by onclose
      });

    return () => {
      for (const id of groups) {
        void connection.invoke('LeaveProviderGroup', id).catch(() => {});
      }
      groups.clear();
      void connection.stop();
      connectionRef.current = null;
    };
  }, [enabled, providerIds, buildConnection]);

  return { connectionState };
}

// Re-export slot type for consumers
export type { ProviderSlot };
