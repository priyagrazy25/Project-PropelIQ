import * as signalR from '@microsoft/signalr';
import { useCallback, useEffect, useRef, useState } from 'react';
import { getAccessToken } from '../../../shared/api/authInterceptor';
import type { QueueEntry } from '../api/schedulingApi';

export interface QueueUpdateEvent {
  entry: QueueEntry;
  action: 'added' | 'updated' | 'removed';
}

interface UseQueueSignalROptions {
  onQueueUpdate: (event: QueueUpdateEvent) => void;
  onReconnected?: () => void;
  enabled: boolean;
}

type ConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';

const HUB_URL = '/hubs/appointments';
const MAX_RECONNECT_ATTEMPTS = 5;

export function useQueueSignalR({
  onQueueUpdate,
  onReconnected,
  enabled,
}: UseQueueSignalROptions): { connectionState: ConnectionState } {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [connectionState, setConnectionState] =
    useState<ConnectionState>('disconnected');

  const onQueueUpdateRef = useRef(onQueueUpdate);
  useEffect(() => {
    onQueueUpdateRef.current = onQueueUpdate;
  }, [onQueueUpdate]);

  const onReconnectedRef = useRef(onReconnected);
  useEffect(() => {
    onReconnectedRef.current = onReconnected;
  }, [onReconnected]);

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
          return Math.min(
            1000 * Math.pow(2, retryContext.previousRetryCount),
            30000,
          );
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }, []);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    let aborted = false;
    const connection = buildConnection();
    connectionRef.current = connection;

    connection.on('QueueEntryAdded', (entry: QueueEntry) => {
      onQueueUpdateRef.current({ entry, action: 'added' });
    });

    connection.on('QueueEntryUpdated', (entry: QueueEntry) => {
      onQueueUpdateRef.current({ entry, action: 'updated' });
    });

    connection.on('QueueEntryRemoved', (entry: QueueEntry) => {
      onQueueUpdateRef.current({ entry, action: 'removed' });
    });

    connection.onreconnecting(() => {
      setConnectionState('reconnecting');
    });

    connection.onreconnected(() => {
      setConnectionState('connected');
      void connection.invoke('JoinQueueGroup');
      onReconnectedRef.current?.();
    });

    connection.onclose(() => {
      setConnectionState('disconnected');
    });

    void connection
      .start()
      .then(async () => {
        if (!aborted) {
          setConnectionState('connected');
          await connection.invoke('JoinQueueGroup');
        }
      })
      .catch(() => {
        // Connection failed or aborted — silently handled
      });

    return () => {
      aborted = true;
      void connection.invoke('LeaveQueueGroup').catch(() => {});
      void connection.stop();
      connectionRef.current = null;
    };
  }, [enabled, buildConnection]);

  return { connectionState };
}
