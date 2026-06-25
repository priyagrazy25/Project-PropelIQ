import { cn } from '@/lib/utils';
import type { ProviderSlot } from '../api/schedulingApi';

interface SlotGridProps {
  slots: ProviderSlot[];
  selectedSlotId: string | null;
  onSelectSlot: (slotId: string) => void;
  dateLabel: string;
}

function formatTime(isoString: string): string {
  const date = new Date(isoString);
  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

export function SlotGrid({
  slots,
  selectedSlotId,
  onSelectSlot,
  dateLabel,
}: SlotGridProps) {
  const now = new Date();
  const availableSlots = slots.filter(
    (s) =>
      s.isAvailable &&
      new Date(s.startTime.endsWith('Z') ? s.startTime : s.startTime + 'Z') > now
  );

  if (availableSlots.length === 0) {
    return (
      <div className="space-y-1">
        <p className="text-xs font-medium text-muted-foreground">
          No slots available
        </p>
      </div>
    );
  }

  return (
    <div className="mt-3">
      <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">
        {dateLabel}
      </p>
      <div
        className="flex flex-wrap gap-2"
        role="group"
        aria-label="Available time slots"
      >
        {availableSlots.map((slot) => (
          <button
            key={slot.id}
            className={cn(
              'inline-flex items-center rounded border px-3 py-1.5 text-[13px] font-medium transition-all duration-150 cursor-pointer',
              selectedSlotId === slot.id
                ? 'bg-[#1E6F9F] text-white border-[#1E6F9F]'
                : 'bg-card hover:border-[#1E6F9F] hover:text-[#1E6F9F] hover:bg-blue-50 border-border text-foreground',
            )}
            aria-label={`Book ${formatTime(slot.startTime)} slot`}
            aria-pressed={selectedSlotId === slot.id}
            onClick={() => {
              onSelectSlot(slot.id);
            }}
          >
            {formatTime(slot.startTime)}
          </button>
        ))}
      </div>
    </div>
  );
}
