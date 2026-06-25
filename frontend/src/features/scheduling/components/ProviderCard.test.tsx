import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { ProviderResult } from '../api/schedulingApi';
import { ProviderCard } from './ProviderCard';

function localIso(year: number, month: number, day: number, hour: number, minute = 0): string {
  return new Date(year, month - 1, day, hour, minute, 0).toISOString();
}

describe('ProviderCard', () => {
  it('renders provider details, slots, and books selected slot', () => {
    const provider: ProviderResult = {
      id: 'provider-1',
      fullName: 'Dr. Maya Patel',
      specialty: 'Cardiology',
      location: 'Downtown',
      rating: 4.8,
      isAcceptingPatients: true,
      nextAvailableDate: localIso(2026, 6, 20, 9),
      availableSlots: [
        {
          id: 'slot-1',
          startTime: localIso(2026, 6, 20, 9),
          endTime: localIso(2026, 6, 20, 9, 30),
          isAvailable: true,
        },
        {
          id: 'slot-2',
          startTime: localIso(2026, 6, 20, 10),
          endTime: localIso(2026, 6, 20, 10, 30),
          isAvailable: false,
        },
      ],
    };

    const onBookAppointment = vi.fn();

    render(
      <ProviderCard
        provider={provider}
        onBookAppointment={onBookAppointment}
        showUnavailableSlots
      />,
    );

    expect(screen.getByText('Dr. Maya Patel')).toBeInTheDocument();
    expect(screen.getByText('Cardiology · Downtown')).toBeInTheDocument();
    expect(screen.getByText('Accepting Patients')).toBeInTheDocument();
    expect(screen.getByRole('group', { name: 'Available time slots' })).toBeInTheDocument();
    expect(screen.getByRole('group', { name: 'Unavailable time slots' })).toBeInTheDocument();

    const availableSlotButton = screen.getByRole('button', {
      name: /Book .* slot/i,
    });

    fireEvent.click(availableSlotButton);
    fireEvent.click(
      screen.getByRole('button', { name: 'Book appointment with Dr. Maya Patel' }),
    );

    expect(onBookAppointment).toHaveBeenCalledTimes(1);
    expect(onBookAppointment).toHaveBeenCalledWith(provider, provider.availableSlots[0]);
  });
});
