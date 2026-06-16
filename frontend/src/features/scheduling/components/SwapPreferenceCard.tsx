import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Loader2 } from 'lucide-react';
import { useCallback, useState } from 'react';
import {
  cancelSwapPreference,
  type SwapPreference,
} from '../api/schedulingApi';

interface SwapPreferenceCardProps {
  swap: SwapPreference;
  onCancelled: (swapId: string) => void;
}

function formatDateTime(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

const STATUS_VARIANTS: Record<string, { className: string; label: string }> = {
  Pending: {
    className: 'bg-amber-100 text-amber-800 hover:bg-amber-100',
    label: 'Pending',
  },
  Executed: {
    className: 'bg-green-100 text-green-800 hover:bg-green-100',
    label: 'Swapped',
  },
  Expired: {
    className: 'bg-gray-100 text-gray-800 hover:bg-gray-100',
    label: 'Expired',
  },
  Cancelled: {
    className: 'bg-red-100 text-red-800 hover:bg-red-100',
    label: 'Cancelled',
  },
};

export function SwapPreferenceCard({
  swap,
  onCancelled,
}: SwapPreferenceCardProps) {
  const [cancelling, setCancelling] = useState(false);

  const handleCancel = useCallback(() => {
    setCancelling(true);
    cancelSwapPreference(swap.id)
      .then((result) => {
        if (result.success) {
          onCancelled(swap.id);
        } else {
          setCancelling(false);
        }
      })
      .catch(() => {
        setCancelling(false);
      });
  }, [swap.id, onCancelled]);

  const statusInfo = STATUS_VARIANTS[swap.status] ?? STATUS_VARIANTS.Pending!;

  return (
    <Card role="article">
      <CardContent className="p-4 space-y-3">
        <div className="flex items-center justify-between">
          <span className="font-semibold">{swap.providerName}</span>
          <Badge className={statusInfo.className}>{statusInfo.label}</Badge>
        </div>
        <div className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
          <span className="text-muted-foreground">Preferred Time</span>
          <span className="font-medium">
            {formatDateTime(swap.preferredSlotStartTime)}
          </span>
          <span className="text-muted-foreground">Registered</span>
          <span className="font-medium">{formatDateTime(swap.createdAt)}</span>
        </div>
        {swap.status === 'Pending' && (
          <Button
            variant="outline"
            size="sm"
            onClick={handleCancel}
            disabled={cancelling}
            aria-label={`Cancel swap preference for ${swap.providerName}`}
          >
            {cancelling ? (
              <>
                <Loader2 className="h-3.5 w-3.5 mr-1.5 animate-spin" />
                Cancelling…
              </>
            ) : (
              'Cancel Preference'
            )}
          </Button>
        )}
      </CardContent>
    </Card>
  );
}
