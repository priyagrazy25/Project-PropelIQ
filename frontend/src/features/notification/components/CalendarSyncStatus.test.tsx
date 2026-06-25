import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { CalendarSyncStatus } from './CalendarSyncStatus';

describe('CalendarSyncStatus', () => {
  it('renders nothing for idle state', () => {
    const { container } = render(<CalendarSyncStatus state="idle" />);

    expect(container).toBeEmptyDOMElement();
  });

  it('renders pending status text', () => {
    render(<CalendarSyncStatus state="pending" message="Queued for retry" />);

    expect(screen.getByText('Sync pending')).toBeInTheDocument();
    expect(screen.getByText('Queued for retry')).toBeInTheDocument();
  });

  it('shows retry action for failed state', () => {
    const onRetry = vi.fn();

    render(
      <CalendarSyncStatus
        state="failed"
        message="Calendar API unavailable"
        onRetry={onRetry}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Retry calendar sync' }));

    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
