import { cn } from '@/lib/utils';
import type { ProviderSlot } from '../api/schedulingApi';

interface SlotSelectionPanelProps {
  slots: ProviderSlot[];
  selectedSlotId: string | null;
  onSelectSlot: (slotId: string) => void;
  providerName: string;
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

export function SlotSelectionPanel({
  slots,
  selectedSlotId,
  onSelectSlot,
  providerName,
}: SlotSelectionPanelProps) {
  const now = new Date();
  const availableSlots = slots.filter(
    (s) =>
      s.isAvailable &&
      new Date(s.startTime.endsWith('Z') ? s.startTime : s.startTime + 'Z') > now
  );

  if (availableSlots.length === 0) {
    return (
      <div className="space-y-2">
        <h3 className="text-sm font-semibold">Select a Time Slot</h3>
        <p className="text-sm text-muted-foreground">
          No available slots for {providerName}.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <h3 className="text-sm font-semibold">Select a Time Slot</h3>
      <div
        className="flex flex-wrap gap-2"
        role="radiogroup"
        aria-label={`Available slots for ${providerName}`}
      >
        {availableSlots.map((slot) => (
          <button
            key={slot.id}
            className={cn(
              'flex flex-col items-center rounded-md border px-3 py-2 text-sm transition-colors',
              selectedSlotId === slot.id
                ? 'bg-primary text-primary-foreground border-primary'
                : 'bg-background hover:bg-muted border-border',
            )}
            role="radio"
            aria-checked={selectedSlotId === slot.id}
            aria-label={`${formatDate(slot.startTime)} ${formatTime(slot.startTime)} to ${formatTime(slot.endTime)}`}
            onClick={() => {
              onSelectSlot(slot.id);
            }}
          >
            <span className="font-medium">{formatDate(slot.startTime)}</span>
            <span className="text-xs opacity-80">
              {formatTime(slot.startTime)} – {formatTime(slot.endTime)}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
