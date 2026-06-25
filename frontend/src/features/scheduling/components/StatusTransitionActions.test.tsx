import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { StatusTransitionActions } from './StatusTransitionActions';

describe('StatusTransitionActions', () => {
  it('shows Mark Arrived for scheduled patients and transitions to Waiting', () => {
    const onTransition = vi.fn();

    render(
      <StatusTransitionActions
        status="Scheduled"
        loading={false}
        onTransition={onTransition}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Mark patient as arrived' }));

    expect(onTransition).toHaveBeenCalledWith('Waiting');
  });

  it('shows Complete action when status is InProgress', () => {
    const onTransition = vi.fn();

    render(
      <StatusTransitionActions
        status="InProgress"
        loading={false}
        onTransition={onTransition}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Complete visit' }));

    expect(onTransition).toHaveBeenCalledWith('Completed');
  });

  it('renders no actions for terminal states', () => {
    const onTransition = vi.fn();

    const { container } = render(
      <StatusTransitionActions
        status="Completed"
        loading={false}
        onTransition={onTransition}
      />,
    );

    expect(container).toBeEmptyDOMElement();
  });
});
