import { AlertTriangle, ExternalLink, FileText } from 'lucide-react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';
import type { ConflictSummary, DataCategory } from '../api/patient360Api';

export interface ConflictInlineIndicatorProps {
  /** The conflict to display. */
  conflict: ConflictSummary;
  /** Whether to show compact (row) or expanded view. */
  compact?: boolean;
}

/**
 * Inline conflict indicator (AC-4).
 * Displays inline within affected tab sections for warning-level conflicts.
 * Shows conflicting values and links to source documents (AC-5).
 */
export function ConflictInlineIndicator({
  conflict,
  compact = false,
}: ConflictInlineIndicatorProps) {
  const severityStyles = {
    high: 'border-red-200 bg-red-50 text-red-800',
    medium: 'border-amber-200 bg-amber-50 text-amber-800',
    low: 'border-yellow-200 bg-yellow-50 text-yellow-800',
  };

  const iconStyles = {
    high: 'text-red-600',
    medium: 'text-amber-600',
    low: 'text-yellow-600',
  };

  if (compact) {
    return (
      <span
        className={cn(
          'inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-xs font-medium',
          severityStyles[conflict.severity]
        )}
        title={`Conflict: ${conflict.values.join(' vs ')}`}
      >
        <AlertTriangle className="size-3" aria-hidden="true" />
        Conflict
      </span>
    );
  }

  return (
    <div
      className={cn(
        'flex items-start gap-3 rounded-md border p-3',
        severityStyles[conflict.severity]
      )}
      role="alert"
    >
      <AlertTriangle
        className={cn('mt-0.5 size-4 shrink-0', iconStyles[conflict.severity])}
        aria-hidden="true"
      />
      <div className="flex-1 space-y-1">
        <p className="text-sm font-medium">
          Conflicting values for <span className="font-semibold">{conflict.field}</span>
        </p>
        <ul className="flex flex-wrap gap-2 text-xs">
          {conflict.values.map((value, index) => (
            <li
              key={index}
              className="inline-flex items-center gap-1 rounded bg-white/50 px-2 py-0.5"
            >
              <FileText className="size-3" aria-hidden="true" />
              <span className="font-mono">{value}</span>
            </li>
          ))}
        </ul>
        <Link
          to={`/clinical/conflicts/${conflict.conflictId}`}
          className="mt-1 inline-flex items-center gap-1 text-xs font-medium underline hover:no-underline"
        >
          View source documents
          <ExternalLink className="size-3" aria-hidden="true" />
        </Link>
      </div>
    </div>
  );
}

export interface VerifiedBadgeProps {
  /** Category being verified. */
  category: DataCategory;
  /** Number of sources that agree. */
  sourceCount?: number;
}

/**
 * Verified data badge.
 * Displays when all data points in a category agree across documents.
 */
export function VerifiedBadge({ category, sourceCount = 2 }: VerifiedBadgeProps) {
  return (
    <span
      className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700"
      title={`All ${category} data verified across ${sourceCount} sources`}
    >
      <svg
        className="size-3"
        viewBox="0 0 16 16"
        fill="currentColor"
        aria-hidden="true"
      >
        <path
          fillRule="evenodd"
          d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm3.78-9.72a.75.75 0 0 0-1.06-1.06L6.75 9.19 5.28 7.72a.75.75 0 0 0-1.06 1.06l2 2a.75.75 0 0 0 1.06 0l4.5-4.5z"
          clipRule="evenodd"
        />
      </svg>
      Verified
    </span>
  );
}

export interface ConflictCountBadgeProps {
  /** Number of conflicts in this category. */
  count: number;
  /** Highest severity among conflicts. */
  severity?: 'high' | 'medium' | 'low';
}

/**
 * Conflict count badge for tab headers.
 * Shows number of conflicts with severity-based coloring.
 */
export function ConflictCountBadge({ count, severity = 'medium' }: ConflictCountBadgeProps) {
  if (count === 0) {
    return null;
  }

  const styles = {
    high: 'bg-red-100 text-red-700',
    medium: 'bg-amber-100 text-amber-700',
    low: 'bg-yellow-100 text-yellow-700',
  };

  return (
    <span
      className={cn(
        'ml-1 inline-flex size-5 items-center justify-center rounded-full text-xs font-medium',
        styles[severity]
      )}
      title={`${count} conflict${count > 1 ? 's' : ''}`}
    >
      {count}
    </span>
  );
}
