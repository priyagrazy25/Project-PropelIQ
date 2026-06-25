import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AutosaveIndicator } from './AutosaveIndicator';

describe('AutosaveIndicator', () => {
  it('shows saving status label', () => {
    render(<AutosaveIndicator status="saving" lastSavedAt={null} />);

    expect(screen.getByText('Saving…')).toBeInTheDocument();
  });

  it('shows last saved timestamp when idle and lastSavedAt exists', () => {
    const lastSavedAt = new Date('2026-06-20T09:15:00.000Z');

    render(<AutosaveIndicator status="idle" lastSavedAt={lastSavedAt} />);

    expect(screen.getByText(/Last saved/i)).toBeInTheDocument();
  });
});
