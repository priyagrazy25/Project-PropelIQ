import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ConfidenceScoreBadge } from './ConfidenceScoreBadge';

describe('ConfidenceScoreBadge', () => {
  it('renders high confidence score with percentage', () => {
    render(<ConfidenceScoreBadge score={0.75} />);

    const badge = screen.getByText('75%');
    expect(badge).toBeInTheDocument();
    expect(badge.closest('span')).toHaveAttribute('title', 'High Confidence: 75%');
  });

  it('renders very low confidence score label in title', () => {
    render(<ConfidenceScoreBadge score={0.4} />);

    const badge = screen.getByText('40%');
    expect(badge.closest('span')).toHaveAttribute('title', 'Very Low Confidence: 40%');
  });
});
