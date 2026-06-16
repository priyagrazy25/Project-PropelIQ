import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { useCallback } from 'react';
import type { ProviderSlot } from '../api/schedulingApi';

interface PreferredSlotPickerProps {
  slots: ProviderSlot[];
  selectedPreferredSlotId: string | null;
  onSelectPreferred: (slotId: string | null) => void;
  bookedSlotId: string;
}

function formatTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });
}

export function PreferredSlotPicker({
  slots,
  selectedPreferredSlotId,
  onSelectPreferred,
  bookedSlotId,
}: PreferredSlotPickerProps) {
  const unavailableSlots = slots.filter(
    (s) => !s.isAvailable && s.id !== bookedSlotId,
  );

  const handleSelect = useCallback(
    (slotId: string) => {
      onSelectPreferred(selectedPreferredSlotId === slotId ? null : slotId);
    },
    [selectedPreferredSlotId, onSelectPreferred],
  );

  if (unavailableSlots.length === 0) {
    return null;
  }

  return (
    <div className="space-y-2">
      <p className="text-sm font-medium">
        Select a preferred time (currently unavailable)
      </p>
      <p className="text-xs text-muted-foreground">
        If this slot opens up, your appointment will be automatically swapped.
      </p>
      <div
        className="flex flex-wrap gap-2"
        role="radiogroup"
        aria-label="Preferred unavailable slots"
      >
        {unavailableSlots.map((slot) => (
          <button
            key={slot.id}
            className={cn(
              'flex flex-col items-center rounded-md border px-3 py-2 text-sm transition-colors',
              selectedPreferredSlotId === slot.id
                ? 'bg-primary text-primary-foreground border-primary'
                : 'bg-background hover:bg-muted border-border',
            )}
            role="radio"
            aria-checked={selectedPreferredSlotId === slot.id}
            aria-label={`Prefer ${formatDate(slot.startTime)} ${formatTime(slot.startTime)}`}
            onClick={() => {
              handleSelect(slot.id);
            }}
          >
            <span className="font-medium">{formatDate(slot.startTime)}</span>
            <span className="text-xs opacity-80">
              {formatTime(slot.startTime)} – {formatTime(slot.endTime)}
            </span>
            <Badge variant="secondary" className="mt-1 text-xs">
              Unavailable
            </Badge>
          </button>
        ))}
      </div>
    </div>
  );
}
