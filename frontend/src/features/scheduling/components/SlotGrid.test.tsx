import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { ProviderSlot } from '../api/schedulingApi';
import { SlotGrid } from './SlotGrid';

function localIso(year: number, month: number, day: number, hour: number, minute = 0): string {
  return new Date(year, month - 1, day, hour, minute, 0).toISOString();
}

function displayTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

describe('SlotGrid', () => {
  it('renders only available slots and calls onSelectSlot with selected id', () => {
    const slots: ProviderSlot[] = [
      {
        id: 'slot-available',
        startTime: localIso(2026, 6, 20, 9),
        endTime: localIso(2026, 6, 20, 9, 30),
        isAvailable: true,
      },
      {
        id: 'slot-unavailable',
        startTime: localIso(2026, 6, 20, 10),
        endTime: localIso(2026, 6, 20, 10, 30),
        isAvailable: false,
      },
    ];

    const onSelectSlot = vi.fn();

    render(
      <SlotGrid
        slots={slots}
        selectedSlotId={null}
        onSelectSlot={onSelectSlot}
        dateLabel="Available Slots"
      />,
    );

    const availableLabel = displayTime(slots[0].startTime);
    expect(screen.getByText(availableLabel)).toBeInTheDocument();

    const unavailableLabel = displayTime(slots[1].startTime);
    expect(screen.queryByText(unavailableLabel)).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: `Book ${availableLabel} slot` }));
    expect(onSelectSlot).toHaveBeenCalledWith('slot-available');
  });
});
