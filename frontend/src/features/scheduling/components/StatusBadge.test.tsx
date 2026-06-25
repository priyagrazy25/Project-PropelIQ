import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StatusBadge } from './StatusBadge';

describe('StatusBadge', () => {
  it('renders friendly label for InProgress status', () => {
    render(<StatusBadge status="InProgress" />);

    expect(screen.getByText('In Progress')).toBeInTheDocument();
  });

  it('renders No-Show label for NoShow status', () => {
    render(<StatusBadge status="NoShow" />);

    expect(screen.getByText('No-Show')).toBeInTheDocument();
  });
});
