import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Clock, Star } from 'lucide-react';
import { useCallback, useState } from 'react';
import type { ProviderResult, ProviderSlot } from '../api/schedulingApi';
import { SlotGrid } from './SlotGrid';

interface ProviderCardProps {
  provider: ProviderResult;
  onBookAppointment?: (provider: ProviderResult, slot: ProviderSlot) => void;
  onJoinWaitlist?: (provider: ProviderResult) => void;
  onViewProfile?: (provider: ProviderResult) => void;
  showUnavailableSlots?: boolean;
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

function formatTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

export function ProviderCard({
  provider,
  onBookAppointment,
  onJoinWaitlist,
  onViewProfile,
  showUnavailableSlots = false,
}: ProviderCardProps) {
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null);

  const handleSelectSlot = useCallback((slotId: string) => {
    setSelectedSlotId((prev) => (prev === slotId ? null : slotId));
  }, []);

  const handleBookClick = useCallback(() => {
    if (!selectedSlotId || !onBookAppointment) return;
    const slot = provider.availableSlots.find((s) => s.id === selectedSlotId);
    if (slot) {
      onBookAppointment(provider, slot);
    }
  }, [selectedSlotId, onBookAppointment, provider]);

  const dateLabel = provider.nextAvailableDate
    ? `Available Slots - ${new Date(provider.nextAvailableDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}`
    : 'Available Slots';

  return (
    <div
      className="rounded-lg border border-border bg-card p-5 shadow-sm transition-all duration-150 hover:shadow-md hover:-translate-y-px"
      role="article"
    >
      <div className="flex gap-3 mb-4">
        <div
          className="h-12 w-12 shrink-0 rounded-full bg-blue-50 text-[#1E6F9F] flex items-center justify-center font-semibold text-lg"
          aria-hidden="true"
        >
          {getInitials(provider.fullName)}
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-base font-semibold text-foreground">
            {provider.fullName}
          </p>
          <p className="text-[13px] text-muted-foreground">
            {provider.specialty} · {provider.location}
          </p>
          <div className="flex items-center gap-1 text-[13px] text-orange-500 mt-0.5">
            <Star className="h-3.5 w-3.5 fill-current" />
            <span>{provider.rating.toFixed(1)}</span>
          </div>
        </div>
      </div>

      {provider.isAcceptingPatients && (
        <Badge className="bg-emerald-50 text-emerald-700 hover:bg-emerald-50 mb-3 text-xs font-medium">
          Accepting Patients
        </Badge>
      )}

      <SlotGrid
        slots={provider.availableSlots}
        selectedSlotId={selectedSlotId}
        onSelectSlot={handleSelectSlot}
        dateLabel={dateLabel}
      />

      {showUnavailableSlots &&
        (() => {
          const unavailable = provider.availableSlots.filter(
            (s) => !s.isAvailable,
          );
          if (unavailable.length === 0) return null;
          return (
            <div className="space-y-2 mt-3">
              <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Unavailable Slots (prefer for swap)
              </p>
              <div
                className="flex flex-wrap gap-1.5"
                role="group"
                aria-label="Unavailable time slots"
              >
                {unavailable.map((slot) => (
                  <span
                    key={slot.id}
                    className="inline-flex items-center rounded border border-border bg-muted/50 px-2 py-1 text-xs text-muted-foreground line-through"
                    aria-label={`${formatTime(slot.startTime)} unavailable`}
                  >
                    {formatTime(slot.startTime)}
                  </span>
                ))}
              </div>
            </div>
          );
        })()}

      <div className="flex gap-2 mt-4">
        <Button
          size="sm"
          className="bg-[#1E6F9F] hover:bg-[#175F87] text-white"
          disabled={!selectedSlotId}
          aria-label={`Book appointment with ${provider.fullName}`}
          onClick={handleBookClick}
        >
          Book Appointment
        </Button>
        {provider.availableSlots.every((s) => !s.isAvailable) &&
          onJoinWaitlist && (
            <Button
              variant="outline"
              size="sm"
              className="border-amber-300 text-amber-700 hover:bg-amber-50"
              aria-label={`Join waitlist for ${provider.fullName}`}
              onClick={() => {
                onJoinWaitlist(provider);
              }}
            >
              <Clock className="h-3.5 w-3.5 mr-1.5" />
              Join Waitlist
            </Button>
          )}
        <Button
          variant="outline"
          size="sm"
          className="border-[#1E6F9F] text-[#1E6F9F] hover:bg-blue-50"
          aria-label={`View profile for ${provider.fullName}`}
          onClick={() => onViewProfile?.(provider)}
        >
          View Profile
        </Button>
      </div>
    </div>
  );
}
