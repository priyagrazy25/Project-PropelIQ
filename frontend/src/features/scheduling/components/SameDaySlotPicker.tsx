import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { Clock } from 'lucide-react';

export interface SameDaySlot {
  id: string;
  startTime: string;
  endTime: string;
  providerName: string;
  isAvailable: boolean;
}

interface SameDaySlotPickerProps {
  slots: SameDaySlot[];
  loading: boolean;
  selectedSlotId: string | null;
  onSelectSlot: (slotId: string) => void;
  visitType: string;
  onVisitTypeChange: (value: string) => void;
  provider: string;
  onProviderChange: (value: string) => void;
  reason: string;
  onReasonChange: (value: string) => void;
  providers: { id: string; name: string }[];
  noSlotsAvailable: boolean;
  estimatedWaitTime: string | null;
  onAddToWaitQueue: () => void;
}

const VISIT_TYPES = ['Walk-In', 'Urgent', 'Follow-Up'] as const;

export function SameDaySlotPicker({
  slots,
  loading,
  selectedSlotId,
  onSelectSlot,
  visitType,
  onVisitTypeChange,
  provider,
  onProviderChange,
  reason,
  onReasonChange,
  providers,
  noSlotsAvailable,
  estimatedWaitTime,
  onAddToWaitQueue,
}: SameDaySlotPickerProps) {
  return (
    <div>
      <div className="grid grid-cols-2 gap-5">
        <div className="flex flex-col gap-1">
          <Label htmlFor="visit-type">
            Visit Type <span className="text-destructive">*</span>
          </Label>
          <select
            id="visit-type"
            className="h-10 px-3 border border-border rounded-md text-sm bg-background focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
            value={visitType}
            onChange={(e) => onVisitTypeChange(e.target.value)}
          >
            <option value="">Select</option>
            {VISIT_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="provider">
            Provider <span className="text-destructive">*</span>
          </Label>
          <select
            id="provider"
            className="h-10 px-3 border border-border rounded-md text-sm bg-background focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
            value={provider}
            onChange={(e) => onProviderChange(e.target.value)}
          >
            <option value="">Select available provider</option>
            {providers.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </div>
        <div className="flex flex-col gap-1 col-span-2">
          <Label htmlFor="reason">Reason for Visit</Label>
          <Input
            id="reason"
            placeholder="Brief reason for walk-in"
            value={reason}
            onChange={(e) => onReasonChange(e.target.value)}
          />
        </div>
      </div>

      {/* Same-day slot list */}
      {provider && (
        <div className="mt-5">
          <h4 className="text-sm font-medium mb-3">Available Slots</h4>

          {loading && (
            <div className="space-y-2" aria-busy="true">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          )}

          {!loading && noSlotsAvailable && (
            <div className="rounded-md border border-border p-4 text-center">
              <p className="text-sm text-muted-foreground mb-2">
                No same-day slots available for this provider.
              </p>
              {estimatedWaitTime && (
                <p className="text-xs text-muted-foreground mb-3">
                  Estimated wait time: <strong>{estimatedWaitTime}</strong>
                </p>
              )}
              <Button variant="outline" size="sm" onClick={onAddToWaitQueue}>
                <Clock className="h-4 w-4 mr-1" aria-hidden="true" />
                Add to Wait Queue
              </Button>
            </div>
          )}

          {!loading &&
            !noSlotsAvailable &&
            slots.map((slot) => (
              <button
                key={slot.id}
                type="button"
                disabled={!slot.isAvailable}
                className={cn(
                  'w-full text-left px-4 py-2 border rounded-md mb-2 text-sm transition-colors duration-(--duration-micro)',
                  selectedSlotId === slot.id
                    ? 'bg-accent border-primary font-medium'
                    : slot.isAvailable
                      ? 'border-border hover:bg-accent hover:border-primary cursor-pointer'
                      : 'border-border opacity-50 cursor-not-allowed',
                )}
                onClick={() => slot.isAvailable && onSelectSlot(slot.id)}
              >
                <span>
                  {slot.startTime} – {slot.endTime}
                </span>
              </button>
            ))}
        </div>
      )}
    </div>
  );
}
