import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { RiskBadge, RiskIndicator } from './RiskIndicator';

describe('RiskIndicator', () => {
  it('renders score and progress width for high risk', () => {
    const { container } = render(<RiskIndicator score={82} level="High" showBar />);

    expect(screen.getByText('82%')).toBeInTheDocument();

    const progressFill = container.querySelector('.bg-red-600') as HTMLElement | null;
    expect(progressFill).not.toBeNull();
    expect(progressFill?.style.width).toBe('82%');
  });

  it('clamps progress width to 100% when score exceeds 100', () => {
    const { container } = render(<RiskIndicator score={130} level="High" showBar />);

    const progressFill = container.querySelector('.bg-red-600') as HTMLElement | null;
    expect(progressFill?.style.width).toBe('100%');
  });
});

describe('RiskBadge', () => {
  it('renders low risk label', () => {
    render(<RiskBadge level="Low" />);

    expect(screen.getByText('Low')).toBeInTheDocument();
  });
});
