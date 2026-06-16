import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { AlertTriangle, Loader2 } from 'lucide-react';
import { useCallback, useState } from 'react';
import { cancelAppointment } from '../api/schedulingApi';

interface CancelAppointmentDialogProps {
  appointmentId: string;
  providerName: string;
  slotStartTime: string;
  onCancelled: () => void;
  onClose: () => void;
}

const CANCEL_REASONS = [
  'Schedule conflict',
  'Found another provider',
  'No longer needed',
  'Personal reasons',
  'Other',
] as const;

function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

export function CancelAppointmentDialog({
  appointmentId,
  providerName,
  slotStartTime,
  onCancelled,
  onClose,
}: CancelAppointmentDialogProps) {
  const [reason, setReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleConfirm = useCallback(() => {
    setSubmitting(true);
    setError(null);

    cancelAppointment(appointmentId, {
      cancellationReason: reason || undefined,
    })
      .then((result) => {
        if (result.success) {
          onCancelled();
        } else {
          setError(result.error.message);
          setSubmitting(false);
        }
      })
      .catch(() => {
        setError('An unexpected error occurred.');
        setSubmitting(false);
      });
  }, [appointmentId, reason, onCancelled]);

  return (
    <AlertDialog
      open
      onOpenChange={(open) => {
        if (!open && !submitting) onClose();
      }}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-destructive" />
            Cancel Appointment?
          </AlertDialogTitle>
          <AlertDialogDescription>
            This will cancel your appointment with{' '}
            <strong>{providerName}</strong> on {formatDate(slotStartTime)}. This
            action cannot be undone. You may need to rebook.
          </AlertDialogDescription>
        </AlertDialogHeader>

        <div className="space-y-2">
          <Label htmlFor="cancel-reason">Reason (optional)</Label>
          <select
            id="cancel-reason"
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
            value={reason}
            onChange={(e) => {
              setReason(e.target.value);
            }}
            disabled={submitting}
          >
            <option value="">Select a reason</option>
            {CANCEL_REASONS.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
        </div>

        {error && (
          <div
            className="rounded-lg border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive"
            role="alert"
          >
            {error}
          </div>
        )}

        <AlertDialogFooter>
          <Button variant="outline" onClick={onClose} disabled={submitting}>
            Keep Appointment
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={submitting}
          >
            {submitting ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Cancelling…
              </>
            ) : (
              'Yes, Cancel'
            )}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
