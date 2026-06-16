import { cn } from '@/lib/utils';
import type { RiskLevel } from '../api/riskApi';

interface RiskIndicatorProps {
  /** Risk score from 0-100. */
  score: number;
  /** Risk level category. */
  level: RiskLevel;
  /** Whether to show the progress bar. */
  showBar?: boolean;
  /** Additional CSS classes. */
  className?: string;
}

/**
 * Visual risk level indicator matching design system tokens.
 * - Low (≤30): Green bg/text
 * - Medium (31-70): Amber bg/text
 * - High (>70): Red bg/text
 *
 * Used in SCR-022 No-Show Risk Dashboard.
 */
export function RiskIndicator({
  score,
  level,
  showBar = true,
  className,
}: RiskIndicatorProps) {
  const levelStyles: Record<RiskLevel, { badge: string; bar: string; score: string }> = {
    Low: {
      badge: 'bg-green-50 text-green-700 border-green-200',
      bar: 'bg-green-600',
      score: 'text-green-700',
    },
    Medium: {
      badge: 'bg-amber-50 text-amber-700 border-amber-200',
      bar: 'bg-amber-500',
      score: 'text-amber-700',
    },
    High: {
      badge: 'bg-red-50 text-red-700 border-red-200',
      bar: 'bg-red-600',
      score: 'text-red-700',
    },
  };

  const styles = levelStyles[level];

  return (
    <div className={cn('flex items-center gap-3', className)}>
      {showBar && (
        <div className="flex items-center gap-2">
          <span className={cn('font-mono text-sm font-semibold min-w-[36px]', styles.score)}>
            {score}%
          </span>
          <div className="w-16 h-2 rounded-full bg-gray-100 overflow-hidden">
            <div
              className={cn('h-full rounded-full transition-all duration-200', styles.bar)}
              style={{ width: `${Math.min(score, 100)}%` }}
            />
          </div>
        </div>
      )}
    </div>
  );
}

interface RiskBadgeProps {
  /** Risk level category. */
  level: RiskLevel;
  /** Additional CSS classes. */
  className?: string;
}

/**
 * Risk level badge component (AC-2: visual indicators).
 * Displays Low/Medium/High with color-coded styling.
 */
export function RiskBadge({ level, className }: RiskBadgeProps) {
  const levelStyles: Record<RiskLevel, string> = {
    Low: 'bg-green-50 text-green-700 border-green-200',
    Medium: 'bg-amber-50 text-amber-700 border-amber-200',
    High: 'bg-red-50 text-red-700 border-red-200',
  };

  return (
    <span
      className={cn(
        'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold border',
        levelStyles[level],
        className,
      )}
    >
      {level}
    </span>
  );
}

/**
 * Combined risk score display with progress bar and badge.
 * Used in the risk assessment table rows.
 */
export function RiskScoreCell({
  score,
  level,
}: {
  score: number;
  level: RiskLevel;
}) {
  return (
    <div className="flex items-center gap-4">
      <RiskIndicator score={score} level={level} showBar />
      <RiskBadge level={level} />
    </div>
  );
}
