import { useCallback, useState } from 'react';
import type { WaitlistAvailableEvent } from '../api/schedulingApi';
import { QueueStatusSection } from '../components/QueueStatusSection';
import { WaitlistDashboardSection } from '../components/WaitlistDashboardSection';
import type { SlotUpdate } from '../hooks/useSignalRSlots';
import { useSignalRSlots } from '../hooks/useSignalRSlots';

const NOOP_SLOT_UPDATE = (_update: SlotUpdate) => {};

export function WaitlistPage() {
  const [availableEvent, setAvailableEvent] =
    useState<WaitlistAvailableEvent | null>(null);

  const handleWaitlistAvailable = useCallback(
    (event: WaitlistAvailableEvent) => {
      setAvailableEvent(event);
    },
    [],
  );

  const handleDismissAvailable = useCallback(() => {
    setAvailableEvent(null);
  }, []);

  useSignalRSlots({
    providerIds: [],
    onSlotUpdate: NOOP_SLOT_UPDATE,
    onWaitlistAvailable: handleWaitlistAvailable,
    enabled: true,
  });

  return (
    <main className="space-y-6" role="main">
      {/* Show current queue status if patient is waiting */}
      <QueueStatusSection />
      
      <WaitlistDashboardSection
        availableEvent={availableEvent}
        onDismissAvailable={handleDismissAvailable}
      />
    </main>
  );
}
