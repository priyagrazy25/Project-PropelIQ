import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Loader2 } from 'lucide-react';
import { useCallback, useState } from 'react';
import {
  joinWaitlist,
  type ProviderResult,
  type WaitlistEntry,
} from '../api/schedulingApi';

interface WaitlistJoinDialogProps {
  provider: ProviderResult;
  onJoined: (entry: WaitlistEntry) => void;
  onClose: () => void;
}

function getTomorrowDate(): string {
  const d = new Date();
  d.setDate(d.getDate() + 1);
  return d.toISOString().split('T')[0] ?? '';
}

function getDatePlusDays(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().split('T')[0] ?? '';
}

export function WaitlistJoinDialog({
  provider,
  onJoined,
  onClose,
}: WaitlistJoinDialogProps) {
  const [dateStart, setDateStart] = useState(getTomorrowDate());
  const [dateEnd, setDateEnd] = useState(getDatePlusDays(14));
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = useCallback(() => {
    if (!dateStart || !dateEnd) {
      setError('Please select both start and end dates.');
      return;
    }
    if (new Date(dateEnd) < new Date(dateStart)) {
      setError('End date must be after start date.');
      return;
    }

    setSubmitting(true);
    setError(null);

    joinWaitlist({
      providerId: provider.id,
      preferredDateStart: dateStart,
      preferredDateEnd: dateEnd,
    })
      .then((result) => {
        if (result.success) {
          onJoined(result.data);
        } else {
          setError(result.error.message);
          setSubmitting(false);
        }
      })
      .catch(() => {
        setError('An unexpected error occurred.');
        setSubmitting(false);
      });
  }, [dateStart, dateEnd, provider.id, onJoined]);

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !submitting) onClose();
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Join Waitlist</DialogTitle>
        </DialogHeader>

        <div className="flex items-center gap-3">
          <Avatar className="h-9 w-9">
            <AvatarFallback className="text-xs">
              {provider.fullName
                .split(' ')
                .map((p) => p[0])
                .join('')
                .slice(0, 2)}
            </AvatarFallback>
          </Avatar>
          <div>
            <p className="font-semibold text-sm">{provider.fullName}</p>
            <p className="text-xs text-muted-foreground">
              {provider.specialty} · {provider.location}
            </p>
          </div>
        </div>

        <p className="text-sm text-muted-foreground">
          All slots for this provider are currently booked. Join the waitlist
          and we'll notify you when a slot becomes available in your preferred
          date range.
        </p>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label htmlFor="wl-date-start">Preferred Start Date</Label>
            <Input
              id="wl-date-start"
              type="date"
              value={dateStart}
              min={getTomorrowDate()}
              onChange={(e) => {
                setDateStart(e.target.value);
              }}
              disabled={submitting}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="wl-date-end">Preferred End Date</Label>
            <Input
              id="wl-date-end"
              type="date"
              value={dateEnd}
              min={dateStart || getTomorrowDate()}
              onChange={(e) => {
                setDateEnd(e.target.value);
              }}
              disabled={submitting}
            />
          </div>
        </div>

        {error && (
          <div
            className="rounded-lg border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive"
            role="alert"
          >
            {error}
          </div>
        )}

        <DialogFooter>
          <Button variant="ghost" onClick={onClose} disabled={submitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting}>
            {submitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Joining…
              </>
            ) : (
              'Join Waitlist'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
