import { cn } from '@/lib/utils';

export interface ConfidenceScoreBadgeProps {
  score: number;
}

function getConfidenceLevel(score: number) {
  if (score >= 0.7) {
    return {
      label: 'High Confidence',
      className: 'bg-[var(--confidence-high-bg)] text-[var(--confidence-high)]',
    };
  }
  if (score >= 0.5) {
    return {
      label: 'Low Confidence',
      className:
        'bg-[var(--confidence-medium-bg)] text-[var(--confidence-medium)]',
    };
  }
  return {
    label: 'Very Low Confidence',
    className: 'bg-[var(--confidence-low-bg)] text-[var(--confidence-low)]',
  };
}

export function ConfidenceScoreBadge({ score }: ConfidenceScoreBadgeProps) {
  const { label, className } = getConfidenceLevel(score);
  const percentage = Math.round(score * 100);

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium',
        className,
      )}
      title={`${label}: ${percentage}%`}
    >
      <span aria-hidden="true">
        {score >= 0.7 ? '✓' : score >= 0.5 ? '!' : '✕'}
      </span>
      {percentage}%
    </span>
  );
}
