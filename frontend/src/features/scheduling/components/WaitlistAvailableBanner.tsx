import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PartyPopper } from 'lucide-react';
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import type { WaitlistAvailableEvent } from '../api/schedulingApi';

interface WaitlistAvailableBannerProps {
  event: WaitlistAvailableEvent;
  onDismiss: () => void;
}

function formatDateTime(isoString: string): string {
  return new Date(isoString).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

export function WaitlistAvailableBanner({
  event,
  onDismiss,
}: WaitlistAvailableBannerProps) {
  const navigate = useNavigate();

  const handleQuickBook = useCallback(() => {
    void navigate('/search', {
      state: {
        quickBook: {
          providerId: event.providerId,
          slotId: event.slotId,
        },
      },
    });
  }, [navigate, event.providerId, event.slotId]);

  return (
    <Card
      className="border-green-200 bg-green-50"
      role="alert"
      aria-live="assertive"
    >
      <CardContent className="flex items-start gap-3 p-4">
        <PartyPopper className="h-5 w-5 text-green-600 shrink-0 mt-0.5" />
        <div className="flex-1">
          <p className="font-semibold text-sm text-green-900">
            A slot is now available!
          </p>
          <p className="text-sm text-green-800 mt-1">
            {event.providerName} has an opening on{' '}
            <strong>{formatDateTime(event.slotStartTime)}</strong>. Act quickly
            — this slot may be claimed by others.
          </p>
        </div>
        <div className="flex gap-2 shrink-0">
          <Button
            size="sm"
            onClick={handleQuickBook}
            aria-label={`Quick book with ${event.providerName}`}
          >
            Quick Book
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={onDismiss}
            aria-label="Dismiss notification"
          >
            Dismiss
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
