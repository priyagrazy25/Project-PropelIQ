import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Loader2 } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import {
  bookAppointment,
  type BookingResponse,
  type ProviderResult,
  type ProviderSlot,
} from '../api/schedulingApi';
import { PreferredSlotPicker } from './PreferredSlotPicker';

export interface ConfirmBookingDialogProps {
  provider: ProviderResult;
  slot: ProviderSlot;
  onConfirmed: (
    booking: BookingResponse,
    preferredSlotId: string | null,
  ) => void;
  onConflict: () => void;
  onClose: () => void;
}

function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function formatTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function getInitials(name: string): string {
  return name
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

export function ConfirmBookingDialog({
  provider,
  slot,
  onConfirmed,
  onConflict,
  onClose,
}: ConfirmBookingDialogProps) {
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPreferred, setShowPreferred] = useState(false);
  const [preferredSlotId, setPreferredSlotId] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const idempotencyKeyRef = useRef(crypto.randomUUID());

  const handleConfirm = useCallback(() => {
    setSubmitting(true);
    setError(null);

    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    bookAppointment(
      {
        providerId: provider.id,
        slotId: slot.id,
        idempotencyKey: idempotencyKeyRef.current,
        preferredSlotId: preferredSlotId ?? undefined,
      },
      controller.signal,
    )
      .then((result) => {
        if (controller.signal.aborted) return;
        if (result.success) {
          onConfirmed(result.data, preferredSlotId);
        } else if (result.error.status === 409) {
          onConflict();
        } else {
          setError(result.error.message);
          setSubmitting(false);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError('An unexpected error occurred.');
          setSubmitting(false);
        }
      });
  }, [provider.id, slot.id, preferredSlotId, onConfirmed, onConflict]);

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
    };
  }, []);

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !submitting) onClose();
      }}
    >
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Confirm Your Appointment</DialogTitle>
        </DialogHeader>

        <div className="flex items-center gap-3">
          <Avatar>
            <AvatarFallback>{getInitials(provider.fullName)}</AvatarFallback>
          </Avatar>
          <div>
            <p className="font-semibold">{provider.fullName}</p>
            <p className="text-sm text-muted-foreground">
              {provider.specialty} · {provider.location}
            </p>
          </div>
        </div>

        <div className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
          <span className="text-muted-foreground">Date</span>
          <span className="font-medium">{formatDate(slot.startTime)}</span>
          <span className="text-muted-foreground">Time</span>
          <span className="font-medium">
            {formatTime(slot.startTime)} – {formatTime(slot.endTime)}
          </span>
          <span className="text-muted-foreground">Location</span>
          <span className="font-medium">{provider.location}</span>
        </div>

        <div className="flex items-start space-x-3 rounded-lg border p-4">
          <Checkbox
            id="opt-preferred"
            checked={showPreferred}
            onCheckedChange={(checked) => {
              setShowPreferred(checked === true);
              if (!checked) setPreferredSlotId(null);
            }}
            aria-describedby="opt-preferred-desc"
          />
          <div className="grid gap-1.5 leading-none">
            <Label htmlFor="opt-preferred" className="flex items-center gap-2">
              Request Preferred Slot
              <Badge variant="secondary">Optional</Badge>
            </Label>
            <p
              className="text-sm text-muted-foreground"
              id="opt-preferred-desc"
            >
              Get notified if an earlier slot opens up. Your appointment will be
              automatically swapped.
            </p>
          </div>
        </div>

        {showPreferred && (
          <PreferredSlotPicker
            slots={provider.availableSlots}
            selectedPreferredSlotId={preferredSlotId}
            onSelectPreferred={setPreferredSlotId}
            bookedSlotId={slot.id}
          />
        )}

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
          <Button
            onClick={handleConfirm}
            disabled={submitting}
            aria-label="Confirm booking"
          >
            {submitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Booking…
              </>
            ) : (
              'Confirm Booking'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
