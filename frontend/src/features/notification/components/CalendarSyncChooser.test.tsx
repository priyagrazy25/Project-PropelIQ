import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CalendarSyncChooser } from './CalendarSyncChooser';

const authenticatedFetchMock = vi.fn();

vi.mock('../../../shared/api/authInterceptor', () => ({
  authenticatedFetch: (...args: unknown[]) => authenticatedFetchMock(...args),
}));

describe('CalendarSyncChooser', () => {
  afterEach(() => {
    cleanup();
    authenticatedFetchMock.mockReset();
  });

  it('allows provider selection and toggles radio state', () => {
    render(<CalendarSyncChooser appointmentId="appt-123" />);

    const google = screen.getByRole('radio', { name: 'Google Calendar' });
    const outlook = screen.getByRole('radio', { name: 'Outlook Calendar' });

    expect(google).toHaveAttribute('aria-checked', 'true');
    expect(outlook).toHaveAttribute('aria-checked', 'false');

    fireEvent.click(outlook);

    expect(outlook).toHaveAttribute('aria-checked', 'true');
    expect(google).toHaveAttribute('aria-checked', 'false');
  });

  it('shows failed state when connect initialization fails', async () => {
    authenticatedFetchMock.mockRejectedValue(new Error('network error'));

    render(<CalendarSyncChooser appointmentId="appt-123" />);

    fireEvent.click(screen.getByRole('button', { name: 'Connect Calendar' }));

    await waitFor(() => {
      expect(screen.getByText('Sync failed')).toBeInTheDocument();
      expect(
        screen.getByText('Failed to connect to calendar service.'),
      ).toBeInTheDocument();
    });
  });
});
