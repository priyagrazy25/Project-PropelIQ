import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ErrorBanner } from './ErrorBanner';

describe('ErrorBanner', () => {
  it('exposes accessible alert semantics', () => {
    const { container } = render(<ErrorBanner message="Unable to load records" />);

    const alert = screen.getByRole('alert');
    expect(alert).toHaveAttribute('aria-live', 'assertive');
    expect(alert).toHaveTextContent('Unable to load records');

    const alertIcon = container.querySelector('svg[aria-hidden="true"]');
    expect(alertIcon).not.toBeNull();
  });

  it('invokes retry and dismiss actions', () => {
    const onRetry = vi.fn();
    const onDismiss = vi.fn();

    render(
      <ErrorBanner
        message="Temporary service issue"
        onRetry={onRetry}
        onDismiss={onDismiss}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    fireEvent.click(screen.getByRole('button', { name: 'Dismiss error' }));

    expect(onRetry).toHaveBeenCalledTimes(1);
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });
});
