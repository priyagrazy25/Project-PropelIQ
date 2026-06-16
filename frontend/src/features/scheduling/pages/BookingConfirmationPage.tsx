import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { CheckCircle, Loader2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { generatePdfConfirmation } from '../../notification/api/notificationApi';
import { CalendarSyncChooser } from '../../notification/components/CalendarSyncChooser';
import {
  bookAppointment,
  type BookingResponse,
  type ProviderResult,
  type ProviderSlot,
} from '../api/schedulingApi';
import { PreferredSlotPicker } from '../components/PreferredSlotPicker';

interface ConfirmLocationState {
  provider: ProviderResult;
  slot: ProviderSlot;
}

function isConfirmState(state: unknown): state is ConfirmLocationState {
  if (typeof state !== 'object' || state === null) return false;
  const s = state as Record<string, unknown>;
  return (
    typeof s.provider === 'object' &&
    s.provider !== null &&
    typeof s.slot === 'object' &&
    s.slot !== null
  );
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

export function BookingConfirmationPage() {
  const location = useLocation();
  const navigate = useNavigate();

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showPreferred, setShowPreferred] = useState(false);
  const [preferredSlotId, setPreferredSlotId] = useState<string | null>(null);
  const [syncCalendar, setSyncCalendar] = useState(false);
  const [preCheckInsurance, setPreCheckInsurance] = useState(false);
  const [confirmedBooking, setConfirmedBooking] =
    useState<BookingResponse | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const idempotencyKeyRef = useRef(crypto.randomUUID());

  useEffect(() => {
    return () => {
      abortRef.current?.abort();
    };
  }, []);

  if (!isConfirmState(location.state)) {
    return <Navigate to="/search" replace />;
  }

  const { provider, slot } = location.state;

  const handleConfirm = () => {
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
          setConfirmedBooking(result.data);
          // Trigger PDF confirmation generation and email delivery
          generatePdfConfirmation(result.data.appointmentId).then(
            (pdfResult) => {
              if (pdfResult.success) {
                toast.success('Confirmation PDF sent to your email.');
              }
            },
          );
        } else if (result.error.status === 409) {
          toast.error(
            'This slot is no longer available. Please select another.',
          );
          void navigate('/search');
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
  };

  return (
    <main className="space-y-6 max-w-2xl" role="main">
      <nav
        className="flex items-center gap-1 text-sm text-muted-foreground"
        aria-label="Breadcrumb"
      >
        <a href="/dashboard" className="hover:text-foreground">
          Dashboard
        </a>
        <span aria-hidden="true">›</span>
        <a href="/search" className="hover:text-foreground">
          Search Providers
        </a>
        <span aria-hidden="true">›</span>
        <span aria-current="page" className="text-foreground">
          Confirm Booking
        </span>
      </nav>

      <h1 className="text-2xl font-bold text-foreground">
        Confirm Your Appointment
      </h1>

      {/* Booking Summary Card */}
      <Card>
        <CardContent className="p-6 space-y-5">
          {/* Provider Summary */}
          <div className="flex items-center gap-4 pb-5 border-b">
            <Avatar className="h-14 w-14">
              <AvatarFallback className="bg-blue-50 text-[#1E6F9F] text-xl font-semibold">
                {getInitials(provider.fullName)}
              </AvatarFallback>
            </Avatar>
            <div>
              <p className="text-lg font-semibold">{provider.fullName}</p>
              <p className="text-sm text-muted-foreground">
                {provider.specialty} · {provider.location}
              </p>
            </div>
          </div>

          {/* Appointment Details */}
          <h2 className="text-base font-semibold">Appointment Details</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4 text-sm">
            <div className="flex flex-col gap-0.5">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Date
              </span>
              <span className="text-base font-medium">
                {formatDate(slot.startTime)}
              </span>
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Time
              </span>
              <span className="text-base font-medium">
                {formatTime(slot.startTime)} – {formatTime(slot.endTime)}
              </span>
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Visit Type
              </span>
              <span className="text-base font-medium">New Patient Visit</span>
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Location
              </span>
              <span className="text-base font-medium">{provider.location}</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Options Section */}
      <div className="space-y-3">
        {/* Preferred Slot Option */}
        <div className="flex items-start gap-3 rounded-lg border p-4 transition-colors hover:border-[#1E6F9F]/40">
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
              Get notified if an earlier slot opens up. You&apos;ll receive an
              alert to accept or decline.
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

        {/* Calendar Sync Option */}
        <div className="flex items-start gap-3 rounded-lg border p-4 transition-colors hover:border-[#1E6F9F]/40">
          <Checkbox
            id="opt-calendar"
            checked={syncCalendar}
            onCheckedChange={(checked) => {
              setSyncCalendar(checked === true);
            }}
            aria-describedby="opt-calendar-desc"
          />
          <div className="grid gap-1.5 leading-none">
            <Label htmlFor="opt-calendar" className="flex items-center gap-2">
              Sync to Calendar
              <Badge variant="secondary">Optional</Badge>
            </Label>
            <p className="text-sm text-muted-foreground" id="opt-calendar-desc">
              Add this appointment to your Google Calendar or Outlook after
              booking.{' '}
              <Link
                to="/calendar-sync"
                className="text-[#1E6F9F] hover:underline"
              >
                Configure
              </Link>
            </p>
          </div>
        </div>

        {/* Insurance Pre-Check Option */}
        <div className="flex items-start gap-3 rounded-lg border p-4 transition-colors hover:border-[#1E6F9F]/40">
          <Checkbox
            id="opt-insurance"
            checked={preCheckInsurance}
            onCheckedChange={(checked) => {
              setPreCheckInsurance(checked === true);
            }}
            aria-describedby="opt-insurance-desc"
          />
          <div className="grid gap-1.5 leading-none">
            <Label htmlFor="opt-insurance" className="flex items-center gap-2">
              Pre-Check Insurance
              <Badge variant="secondary">Optional</Badge>
            </Label>
            <p
              className="text-sm text-muted-foreground"
              id="opt-insurance-desc"
            >
              Verify your insurance coverage before the visit.{' '}
              <Link
                to="/intake/insurance"
                className="text-[#1E6F9F] hover:underline"
              >
                Start pre-check
              </Link>
            </p>
          </div>
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

      {/* Action Bar */}
      <div className="flex items-center gap-3">
        <Button
          size="lg"
          className="bg-[#1E6F9F] hover:bg-[#175F87]"
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
        <Button
          variant="ghost"
          size="lg"
          onClick={() => {
            void navigate('/search');
          }}
          disabled={submitting}
        >
          Back to Search
        </Button>
      </div>

      {/* OVL-001 Booking Confirmed Modal */}
      {confirmedBooking && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 overflow-y-auto py-8"
          role="dialog"
          aria-modal="true"
          aria-labelledby="confirm-modal-title"
        >
          <div className="bg-background rounded-xl shadow-lg max-w-lg w-[90%] p-6">
            <div className="flex items-center gap-2 rounded-full bg-green-100 px-4 py-2 text-green-700 text-xl font-bold w-fit mb-4">
              <CheckCircle className="h-6 w-6" />
              <span aria-hidden="true">✓</span>
            </div>
            <h2 id="confirm-modal-title" className="text-xl font-semibold mb-3">
              Booking Confirmed!
            </h2>
            <div className="text-sm text-muted-foreground space-y-2 mb-6">
              <p>
                Your appointment with{' '}
                <strong>{confirmedBooking.providerName}</strong> is confirmed
                for{' '}
                <strong>
                  {formatDate(confirmedBooking.slotStartTime)} at{' '}
                  {formatTime(confirmedBooking.slotStartTime)}
                </strong>
                .
              </p>
              <p>
                You&apos;ll receive a reminder 24 hours before your appointment.
              </p>
            </div>

            {/* Calendar Sync UI — shown when user opted in */}
            {syncCalendar && (
              <div className="mb-6 border-t pt-5">
                <CalendarSyncChooser
                  appointmentId={confirmedBooking.appointmentId}
                  onSkip={() => {
                    void navigate('/dashboard');
                  }}
                />
              </div>
            )}

            <div className="flex justify-end">
              <Button
                className="bg-[#1E6F9F] hover:bg-[#175F87]"
                onClick={() => {
                  void navigate('/dashboard');
                }}
              >
                Go to Dashboard
              </Button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
