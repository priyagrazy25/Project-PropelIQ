import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { VerificationActions } from './VerificationActions';

describe('VerificationActions', () => {
  afterEach(() => {
    cleanup();
  });

  it('triggers accept and reject callbacks with entry id', () => {
    const onAccept = vi.fn();
    const onReject = vi.fn();

    render(
      <VerificationActions
        entryId="mc-1"
        onAccept={onAccept}
        onReject={onReject}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Accept code' }));
    fireEvent.click(screen.getByRole('button', { name: 'Reject code' }));

    expect(onAccept).toHaveBeenCalledWith('mc-1');
    expect(onReject).toHaveBeenCalledWith('mc-1');
  });

  it('triggers override callback when override button is enabled', () => {
    const onOverride = vi.fn();

    render(
      <VerificationActions
        entryId="mc-2"
        onAccept={vi.fn()}
        onReject={vi.fn()}
        onOverride={onOverride}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Override with custom code' }));

    expect(onOverride).toHaveBeenCalledWith('mc-2');
  });
});
