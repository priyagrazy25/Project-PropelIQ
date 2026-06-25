import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { Loader2 } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import {
  fetchProviderSlots,
  rescheduleAppointment,
  type MyAppointment,
  type ProviderSlot,
} from '../api/schedulingApi';
import { CancelAppointmentDialog } from '../components/CancelAppointmentDialog';

interface RescheduleLocationState {
  appointment: MyAppointment;
}

function isRescheduleState(state: unknown): state is RescheduleLocationState {
  if (typeof state !== 'object' || state === null) return false;
  const s = state as Record<string, unknown>;
  return typeof s.appointment === 'object' && s.appointment !== null;
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

function formatEndTime(isoString: string, durationMinutes: number): string {
  const d = new Date(isoString);
  d.setMinutes(d.getMinutes() + durationMinutes);
  return d.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function getTodayDate(): string {
  return new Date().toISOString().split('T')[0] ?? '';
}

export function RescheduleAppointmentPage() {
  const location = useLocation();
  const navigate = useNavigate();

  const validState = isRescheduleState(location.state);
  const appointment: MyAppointment | null = validState
    ? (location.state as RescheduleLocationState).appointment
    : null;

  const [selectedDate, setSelectedDate] = useState('');
  const [slots, setSlots] = useState<ProviderSlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<ProviderSlot | null>(null);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [conflictError, setConflictError] = useState(false);
  const [showCancelDialog, setShowCancelDialog] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const prevDateRef = useRef('');

  const providerId = appointment?.providerId ?? '';

  useEffect(() => {
    if (!selectedDate || !providerId) {
      if (prevDateRef.current) {
        prevDateRef.current = '';
      }
      return;
    }

    if (selectedDate === prevDateRef.current) return;
    prevDateRef.current = selectedDate;

    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoadingSlots(true);
    setError(null);
    setSelectedSlot(null);
    setConflictError(false);

    fetchProviderSlots(providerId, selectedDate)
      .then((result) => {
        if (controller.signal.aborted) return;
        if (result.success) {
          setSlots(result.data);
        } else {
          setError(result.error.message);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError('Failed to load available slots.');
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoadingSlots(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [selectedDate, providerId]);

  const handleConfirmReschedule = useCallback(() => {
    if (!selectedSlot || !appointment) return;

    setSubmitting(true);
    setError(null);
    setConflictError(false);

    rescheduleAppointment(
      appointment.appointmentId,
      selectedSlot.id,
      crypto.randomUUID(),
      'Rescheduled to new time',
    )
      .then((result) => {
        if (result.success) {
          toast.success('Appointment rescheduled successfully.');
          void navigate('/dashboard', {
            replace: true,
            state: { refreshDashboard: Date.now() },
          });
        } else if (result.error.status === 409) {
          setConflictError(true);
          setSlots((prev) =>
            prev.map((s) =>
              s.id === selectedSlot.id ? { ...s, isAvailable: false } : s,
            ),
          );
          setSelectedSlot(null);
          setSubmitting(false);
        } else {
          setError(result.error.message);
          setSubmitting(false);
        }
      })
      .catch(() => {
        setError('An unexpected error occurred.');
        setSubmitting(false);
      });
  }, [selectedSlot, appointment, navigate]);

  if (!appointment) {
    return <Navigate to="/dashboard" replace />;
  }

  const availableSlots = slots.filter((s) => s.isAvailable);

  return (
    <main className="space-y-6 max-w-2xl mx-auto" role="main">
      <nav className="flex items-center gap-2 text-sm" aria-label="Breadcrumb">
        <Link to="/dashboard" className="text-primary hover:underline">
          Dashboard
        </Link>
        <span className="text-muted-foreground" aria-hidden="true">
          ›
        </span>
        <span aria-current="page" className="text-muted-foreground">
          Reschedule / Cancel
        </span>
      </nav>

      <h1 className="text-3xl font-bold text-foreground">Manage Appointment</h1>

      {/* Current Appointment Card */}
      <Card>
        <CardContent className="p-6">
          <h2 className="text-lg font-semibold mb-5">Current Appointment</h2>
          <div className="divide-y divide-gray-100">
            <div className="flex justify-between py-2">
              <span className="text-sm text-muted-foreground">Provider</span>
              <span className="text-sm font-medium">
                {appointment.providerName}
              </span>
            </div>
            <div className="flex justify-between py-2">
              <span className="text-sm text-muted-foreground">Date</span>
              <span className="text-sm font-medium">
                {formatDate(appointment.appointmentDateTime)}
              </span>
            </div>
            <div className="flex justify-between py-2">
              <span className="text-sm text-muted-foreground">Time</span>
              <span className="text-sm font-medium">
                {formatTime(appointment.appointmentDateTime)} –{' '}
                {formatEndTime(
                  appointment.appointmentDateTime,
                  appointment.durationMinutes,
                )}
              </span>
            </div>
            <div className="flex justify-between py-2">
              <span className="text-sm text-muted-foreground">Location</span>
              <span className="text-sm font-medium">
                {appointment.location}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Reschedule Card */}
      <Card>
        <CardContent className="p-6 space-y-5">
          <h2 className="text-lg font-semibold">Reschedule</h2>

          <div className="space-y-1">
            <Label htmlFor="new-date">New Date</Label>
            <Input
              type="date"
              id="new-date"
              value={selectedDate}
              min={getTodayDate()}
              onChange={(e) => {
                setSelectedDate(e.target.value);
              }}
              disabled={submitting}
              aria-label="Select new appointment date"
            />
          </div>

          {loadingSlots && (
            <div className="space-y-2" aria-busy="true">
              <Skeleton className="h-4 w-40" />
              <Skeleton className="h-10 w-full rounded-md" />
            </div>
          )}

          {!loadingSlots && selectedDate && availableSlots.length > 0 && (
            <div className="space-y-1">
              <Label htmlFor="new-time">New Time Slot</Label>
              <select
                id="new-time"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                value={selectedSlot?.id ?? ''}
                onChange={(e) => {
                  const slot =
                    availableSlots.find((s) => s.id === e.target.value) ?? null;
                  setSelectedSlot(slot);
                  setConflictError(false);
                }}
                disabled={submitting}
                aria-label="Select available time slot"
              >
                <option value="">Select available time</option>
                {availableSlots.map((slot) => (
                  <option key={slot.id} value={slot.id}>
                    {formatTime(slot.startTime)}
                  </option>
                ))}
              </select>
            </div>
          )}

          {!loadingSlots && selectedDate && availableSlots.length === 0 && (
            <p className="text-sm text-muted-foreground">
              No available slots on this date. Please select a different date.
            </p>
          )}

          {conflictError && (
            <div
              className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
              role="alert"
            >
              That slot is no longer available. Please select another time.
            </div>
          )}

          {error && (
            <div
              className="rounded-lg border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive"
              role="alert"
            >
              {error}
            </div>
          )}

          <div className="flex items-center gap-3 pt-2">
            <Button
              disabled={!selectedSlot || submitting}
              onClick={handleConfirmReschedule}
            >
              {submitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Rescheduling…
                </>
              ) : (
                'Confirm Reschedule'
              )}
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                setShowCancelDialog(true);
              }}
              disabled={submitting}
              aria-label="Cancel appointment"
            >
              Cancel Appointment
            </Button>
            <Button
              variant="ghost"
              onClick={() => {
                void navigate(-1);
              }}
              disabled={submitting}
            >
              Back
            </Button>
          </div>
        </CardContent>
      </Card>

      {showCancelDialog && (
        <CancelAppointmentDialog
          appointmentId={appointment.appointmentId}
          providerName={appointment.providerName}
          slotStartTime={appointment.appointmentDateTime}
          onCancelled={() => {
            toast.success('Appointment cancelled successfully.');
            void navigate('/dashboard', {
              replace: true,
              state: { refreshDashboard: Date.now() },
            });
          }}
          onClose={() => {
            setShowCancelDialog(false);
          }}
        />
      )}
    </main>
  );
}
