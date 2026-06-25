import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { QueueEntry } from '../api/schedulingApi';
import { QueuePatientRow } from './QueuePatientRow';

function renderRow(entry: QueueEntry, onTransition = vi.fn()) {
  render(
    <MemoryRouter>
      <table>
        <tbody>
          <QueuePatientRow entry={entry} loading={false} onTransition={onTransition} />
        </tbody>
      </table>
    </MemoryRouter>,
  );

  return { onTransition };
}

describe('QueuePatientRow', () => {
  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it('shows pre-arrival state and triggers Mark Arrived transition', () => {
    const entry: QueueEntry = {
      id: 'entry-1',
      position: 1,
      patientId: 'patient-1',
      patientName: 'Alex Johnson',
      appointmentType: 'Walk-In',
      providerName: 'Dr. Patel',
      status: 'Scheduled',
      arrivalTime: null,
      rowVersion: 'rv-1',
    };

    const { onTransition } = renderRow(entry);

    expect(screen.getByText('Not yet arrived')).toBeInTheDocument();
    expect(screen.getByText('Alex Johnson')).toHaveAttribute('href', '/staff/patient-view/patient-1');

    fireEvent.click(screen.getByRole('button', { name: 'Mark patient as arrived' }));

    expect(onTransition).toHaveBeenCalledWith('entry-1', 'Waiting');
  });

  it('shows elapsed wait duration for arrived patients', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-06-20T10:35:00.000Z'));

    const entry: QueueEntry = {
      id: 'entry-2',
      position: 2,
      patientId: 'patient-2',
      patientName: 'Sam Rivera',
      appointmentType: 'Follow-Up',
      providerName: 'Dr. Singh',
      status: 'Arrived',
      arrivalTime: '2026-06-20T10:00:00.000Z',
      rowVersion: 'rv-2',
    };

    renderRow(entry);

    expect(screen.getByText('Sam Rivera')).toBeInTheDocument();
    expect(screen.getByText('35 min')).toBeInTheDocument();
  });
});
