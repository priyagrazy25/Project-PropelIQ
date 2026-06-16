import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Clock, Loader2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import {
  fetchWaitlistEntries,
  removeFromWaitlist,
  type WaitlistAvailableEvent,
  type WaitlistEntry,
} from '../api/schedulingApi';
import { WaitlistAvailableBanner } from './WaitlistAvailableBanner';

interface WaitlistDashboardSectionProps {
  availableEvent: WaitlistAvailableEvent | null;
  onDismissAvailable: () => void;
}

function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  });
}

const STATUS_BADGES: Record<string, { className: string; label: string }> = {
  Active: {
    className: 'bg-amber-100 text-amber-800 hover:bg-amber-100',
    label: 'Waiting',
  },
  Notified: {
    className: 'bg-blue-100 text-blue-800 hover:bg-blue-100',
    label: 'Slot Available',
  },
  Booked: {
    className: 'bg-green-100 text-green-800 hover:bg-green-100',
    label: 'Booked',
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

export function WaitlistDashboardSection({
  availableEvent,
  onDismissAvailable,
}: WaitlistDashboardSectionProps) {
  const [entries, setEntries] = useState<WaitlistEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [removingId, setRemovingId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    fetchWaitlistEntries()
      .then((result) => {
        if (cancelled) return;
        if (result.success) {
          const now = new Date();
          setEntries(
            result.data.filter(
              (e) =>
                e.status !== 'Expired' || new Date(e.preferredDateEnd) > now,
            ),
          );
        } else {
          setError(result.error.message);
        }
      })
      .catch(() => {
        if (!cancelled) setError('Failed to load waitlist.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const handleRemove = useCallback((entryId: string) => {
    setRemovingId(entryId);
    removeFromWaitlist(entryId)
      .then((result) => {
        if (result.success) {
          setEntries((prev) => prev.filter((e) => e.id !== entryId));
        }
      })
      .catch(() => {
        // Silently fail — entry remains in list
      })
      .finally(() => {
        setRemovingId(null);
      });
  }, []);

  if (loading) {
    return (
      <section className="space-y-4" aria-label="Waitlist status">
        <h2 className="text-xl font-bold">Waitlist Status</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1, 2].map((i) => (
            <Card key={i} aria-hidden="true">
              <CardContent className="p-4 flex items-center gap-3">
                <Skeleton className="h-10 w-10 rounded-full" />
                <div className="space-y-2 flex-1">
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/2" />
                </div>
                <Skeleton className="h-8 w-20" />
              </CardContent>
            </Card>
          ))}
        </div>
      </section>
    );
  }

  if (error) {
    return (
      <section className="space-y-4" aria-label="Waitlist status">
        <h2 className="text-xl font-bold">Waitlist Status</h2>
        <div
          className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive"
          role="alert"
        >
          {error}
        </div>
      </section>
    );
  }

  const activeEntries = entries.filter(
    (e) => e.status === 'Active' || e.status === 'Notified',
  );

  return (
    <section className="space-y-4" aria-label="Waitlist status">
      <h2 className="text-xl font-bold">Waitlist Status</h2>
      <p className="text-sm text-muted-foreground">
        You'll be notified when an earlier slot becomes available.
      </p>

      {availableEvent && (
        <WaitlistAvailableBanner
          event={availableEvent}
          onDismiss={onDismissAvailable}
        />
      )}

      {activeEntries.length === 0 ? (
        <Card>
          <CardContent
            className="flex flex-col items-center py-8 text-center"
            role="status"
          >
            <Clock className="h-10 w-10 text-muted-foreground mb-3" />
            <p className="font-medium">You're not on any waitlists yet.</p>
            <p className="text-sm text-muted-foreground mt-1">
              Join a waitlist from the provider search when slots are fully
              booked.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {activeEntries.map((entry) => {
            const statusInfo =
              STATUS_BADGES[entry.status] ?? STATUS_BADGES.Active!;
            return (
              <Card key={entry.id} role="article">
                <CardContent className="p-4 flex items-start gap-3">
                  <div
                    className="flex items-center justify-center h-10 w-10 rounded-full bg-primary/10 text-primary font-bold text-sm shrink-0"
                    aria-label={`Position ${String(entry.position)}`}
                  >
                    #{entry.position}
                  </div>
                  <div className="flex-1 min-w-0 space-y-1">
                    <p className="font-semibold text-sm truncate">
                      {entry.providerName} · {entry.specialty}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Requested: {formatDate(entry.createdAt)} · Preferred:{' '}
                      {formatDate(entry.preferredDateStart)} –{' '}
                      {formatDate(entry.preferredDateEnd)}
                    </p>
                    <Badge className={statusInfo.className}>
                      {statusInfo.label}
                    </Badge>
                  </div>
                  {entry.status === 'Active' && (
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={removingId === entry.id}
                      aria-label={`Remove from waitlist for ${entry.providerName}`}
                      onClick={() => {
                        handleRemove(entry.id);
                      }}
                    >
                      {removingId === entry.id ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      ) : (
                        'Remove'
                      )}
                    </Button>
                  )}
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </section>
  );
}
