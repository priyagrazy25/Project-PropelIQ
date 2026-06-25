import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ConflictResolutionPanel } from './ConflictResolutionPanel';

const conflict = {
  conflictId: 'conf-1',
  field: 'Address',
  category: 'history',
  sourceA: {
    sourceLabel: 'AI Intake',
    value: '123 Main St',
    confidence: 0.95,
    extractedAt: '2026-06-10T00:00:00Z',
    documentName: 'intake.pdf',
  },
  sourceB: {
    sourceLabel: 'Insurance OCR',
    value: '456 Oak St',
    confidence: 0.72,
    extractedAt: '2026-06-11T00:00:00Z',
    documentName: 'insurance.pdf',
  },
};

describe('ConflictResolutionPanel', () => {
  afterEach(() => {
    cleanup();
  });

  it('calls onSelectSource when source panel is clicked', () => {
    const onSelectSource = vi.fn();

    render(
      <ConflictResolutionPanel
        conflict={conflict}
        selectedOption={null}
        onSelectSource={onSelectSource}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Select Source A: AI Intake' }));

    expect(onSelectSource).toHaveBeenCalledWith('A');
  });

  it('shows selected state indicator for selected source', () => {
    render(
      <ConflictResolutionPanel
        conflict={conflict}
        selectedOption="B"
      />,
    );

    expect(screen.getByRole('button', { name: 'Select Source B: Insurance OCR' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByLabelText('Selected')).toBeInTheDocument();
  });
});
