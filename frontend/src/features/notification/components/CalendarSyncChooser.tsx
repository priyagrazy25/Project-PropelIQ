import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { Calendar, Loader2 } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { authenticatedFetch } from '../../../shared/api/authInterceptor';
import type {
  CalendarProvider,
  CalendarSyncState,
} from '../../scheduling/api/schedulingApi';
import { syncCalendar } from '../../scheduling/api/schedulingApi';
import { CalendarSyncStatus } from './CalendarSyncStatus';

const OAUTH_POPUP_WIDTH = 500;
const OAUTH_POPUP_HEIGHT = 600;

const CONNECT_URLS: Record<CalendarProvider, string> = {
  Google: '/api/notification/calendar/connect/google',
  Outlook: '/api/notification/calendar/connect/outlook',
};

interface CalendarSyncChooserProps {
  appointmentId: string;
  onSkip?: () => void;
}

export function CalendarSyncChooser({
  appointmentId,
  onSkip,
}: CalendarSyncChooserProps) {
  const [selectedProvider, setSelectedProvider] =
    useState<CalendarProvider>('Google');
  const [syncState, setSyncState] = useState<CalendarSyncState>('idle');
  const [syncMessage, setSyncMessage] = useState<string | null>(null);
  const [isConnecting, setIsConnecting] = useState(false);
  const oauthWindowRef = useRef<Window | null>(null);
  const connectingRef = useRef(false);
  const messageReceivedRef = useRef(false);

  useEffect(() => {
    connectingRef.current = isConnecting;
  }, [isConnecting]);

  const handleProviderSelect = useCallback(
    (provider: CalendarProvider) => {
      if (syncState === 'synced') return;
      setSelectedProvider(provider);
    },
    [syncState],
  );

  const handleOAuthCallback = useCallback(
    async (token: string) => {
      // If no appointmentId (pre-booking configure), just mark as connected
      if (!appointmentId) {
        setSyncState('synced');
        setSyncMessage(
          'Calendar connected successfully. It will sync automatically when you book.',
        );
        setIsConnecting(false);
        return;
      }

      const result = await syncCalendar({
        appointmentId,
        provider: selectedProvider,
        oauthToken: token,
      });

      if (result.success) {
        setSyncState(result.data.status);
        setSyncMessage(result.data.message);
      } else {
        if (result.error.status === 503) {
          setSyncState('pending');
          setSyncMessage(result.error.message);
        } else {
          setSyncState('failed');
          setSyncMessage(result.error.message);
        }
      }
      setIsConnecting(false);
    },
    [appointmentId, selectedProvider],
  );

  const handleConnect = useCallback(() => {
    if (isConnecting || syncState === 'synced') return;

    setIsConnecting(true);
    setSyncState('idle');
    setSyncMessage(null);
    messageReceivedRef.current = false;

    // Fetch the OAuth authorization URL from the backend first
    authenticatedFetch(CONNECT_URLS[selectedProvider])
      .then(async (response) => {
        if (!response.ok) {
          setSyncState('failed');
          setSyncMessage('Failed to initiate calendar connection.');
          setIsConnecting(false);
          return;
        }

        const data = (await response.json()) as { authUrl: string };

        const left =
          window.screenX + (window.outerWidth - OAUTH_POPUP_WIDTH) / 2;
        const top =
          window.screenY + (window.outerHeight - OAUTH_POPUP_HEIGHT) / 2;

        const popup = window.open(
          data.authUrl,
          'calendar-oauth',
          `width=${OAUTH_POPUP_WIDTH},height=${OAUTH_POPUP_HEIGHT},left=${left},top=${top},scrollbars=yes`,
        );

        oauthWindowRef.current = popup;

        const handleMessage = (event: MessageEvent) => {
          const msgData = event.data as
            | { type: string; token?: string; error?: string }
            | undefined;

          if (!msgData || msgData.type !== 'calendar-oauth-callback') return;

          messageReceivedRef.current = true;
          window.removeEventListener('message', handleMessage);

          if (msgData.token) {
            void handleOAuthCallback(msgData.token);
          } else {
            setSyncState('failed');
            setSyncMessage(msgData.error ?? 'Authorization was cancelled.');
            setIsConnecting(false);
          }
        };

        window.addEventListener('message', handleMessage);

        const pollTimer = window.setInterval(() => {
          if (popup && popup.closed) {
            window.clearInterval(pollTimer);
            window.removeEventListener('message', handleMessage);
            if (connectingRef.current && !messageReceivedRef.current) {
              setSyncState('failed');
              setSyncMessage('Authorization window was closed.');
              setIsConnecting(false);
            }
          }
        }, 500);
      })
      .catch(() => {
        setSyncState('failed');
        setSyncMessage('Failed to connect to calendar service.');
        setIsConnecting(false);
      });
  }, [isConnecting, syncState, selectedProvider, handleOAuthCallback]);

  const handleRetry = useCallback(() => {
    setSyncState('idle');
    setSyncMessage(null);
    setIsConnecting(false);
  }, []);

  return (
    <section className="space-y-6" aria-label="Calendar sync">
      <div>
        <h2 className="text-2xl font-bold text-foreground">Sync to Calendar</h2>
        <p className="text-sm text-muted-foreground mt-1">
          Connect your calendar to automatically add appointment events.
        </p>
      </div>

      <div
        className="grid grid-cols-1 sm:grid-cols-2 gap-5"
        role="radiogroup"
        aria-label="Select calendar provider"
      >
        <button
          type="button"
          role="radio"
          aria-checked={selectedProvider === 'Google'}
          aria-label="Google Calendar"
          className={cn(
            'flex flex-col items-center gap-3 rounded-xl border-2 p-6 text-center transition-all',
            'hover:border-primary hover:shadow-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
            selectedProvider === 'Google'
              ? 'border-primary bg-blue-50/60'
              : 'border-border bg-card',
            syncState === 'synced' && 'pointer-events-none opacity-60',
          )}
          onClick={() => handleProviderSelect('Google')}
          disabled={syncState === 'synced'}
        >
          <Calendar className="h-12 w-12 text-primary" aria-hidden="true" />
          <span className="text-lg font-semibold">Google Calendar</span>
          <span className="text-xs text-muted-foreground">
            Sync via Google OAuth
          </span>
        </button>

        <button
          type="button"
          role="radio"
          aria-checked={selectedProvider === 'Outlook'}
          aria-label="Outlook Calendar"
          className={cn(
            'flex flex-col items-center gap-3 rounded-xl border-2 p-6 text-center transition-all',
            'hover:border-primary hover:shadow-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
            selectedProvider === 'Outlook'
              ? 'border-primary bg-blue-50/60'
              : 'border-border bg-card',
            syncState === 'synced' && 'pointer-events-none opacity-60',
          )}
          onClick={() => handleProviderSelect('Outlook')}
          disabled={syncState === 'synced'}
        >
          <Calendar className="h-12 w-12 text-primary" aria-hidden="true" />
          <span className="text-lg font-semibold">Outlook Calendar</span>
          <span className="text-xs text-muted-foreground">
            Sync via Microsoft OAuth
          </span>
        </button>
      </div>

      <CalendarSyncStatus
        state={syncState}
        message={syncMessage}
        onRetry={handleRetry}
      />

      <div className="flex gap-3">
        <Button
          onClick={handleConnect}
          disabled={isConnecting || syncState === 'synced'}
          aria-label="Connect Calendar"
        >
          {isConnecting && (
            <Loader2 className="h-4 w-4 mr-2 animate-spin" aria-hidden="true" />
          )}
          {syncState === 'synced' ? 'Connected' : 'Connect Calendar'}
        </Button>
        {onSkip && (
          <Button variant="ghost" onClick={onSkip}>
            Skip for Now
          </Button>
        )}
      </div>
    </section>
  );
}
