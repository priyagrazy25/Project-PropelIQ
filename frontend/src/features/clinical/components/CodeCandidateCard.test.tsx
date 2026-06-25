import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CodeCandidateCard } from './CodeCandidateCard';

const lowConfidenceEntry = {
  id: 'entry-low',
  patientId: 'p1',
  patientName: 'Jane Doe',
  codeType: 'ICD-10' as const,
  primaryCode: {
    code: 'R69',
    description: 'Unknown and unspecified causes of morbidity',
    confidence: 0.42,
  },
  alternativeCandidates: [
    {
      code: 'R68.89',
      description: 'Other general symptoms and signs',
      confidence: 0.48,
    },
  ],
  status: 'Pending' as const,
  extractedAt: '2026-06-20T00:00:00Z',
};

describe('CodeCandidateCard', () => {
  afterEach(() => {
    cleanup();
  });

  it('shows low-confidence warning when all candidates are below threshold', () => {
    render(
      <CodeCandidateCard
        entry={lowConfidenceEntry}
        onAccept={vi.fn()}
        onReject={vi.fn()}
      />,
    );

    expect(
      screen.getByText('⚠️ All candidates below 50% confidence. Manual code entry may be required.'),
    ).toBeInTheDocument();
  });

  it('calls override callback with entry id', () => {
    const onOverride = vi.fn();

    render(
      <CodeCandidateCard
        entry={lowConfidenceEntry}
        onAccept={vi.fn()}
        onReject={vi.fn()}
        onOverride={onOverride}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Override R69 with custom code' }));

    expect(onOverride).toHaveBeenCalledWith('entry-low');
  });
});
