import { AlertTriangle, XCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { ConflictSummary } from '../api/patient360Api';

export interface ConflictBannerProps {
  /** List of conflicts to display. */
  conflicts: ConflictSummary[];
}

/**
 * Critical conflict alert banner (AC-4).
 * Displays at top of 360-view for critical (high severity) conflicts.
 * Links to conflict resolution page (AC-5).
 */
export function ConflictBanner({ conflicts }: ConflictBannerProps) {
  // Filter for critical (high severity) conflicts only
  const criticalConflicts = conflicts.filter((c) => c.severity === 'high');

  if (criticalConflicts.length === 0) {
    return null;
  }

  return (
    <div
      className="flex items-start gap-3 rounded-lg border border-red-200 bg-red-50 p-4"
      role="alert"
      aria-live="assertive"
    >
      <XCircle className="mt-0.5 size-5 shrink-0 text-red-600" aria-hidden="true" />
      <div className="flex-1">
        <h2 className="font-semibold text-red-800">
          {criticalConflicts.length} Critical Conflict
          {criticalConflicts.length > 1 ? 's' : ''} Detected
        </h2>
        <ul className="mt-2 space-y-1 text-sm text-red-700">
          {criticalConflicts.slice(0, 3).map((conflict) => (
            <li key={conflict.conflictId} className="flex items-center gap-2">
              <AlertTriangle className="size-3.5" aria-hidden="true" />
              <span>
                <span className="font-medium">{conflict.field}</span>: Conflicting values (
                {conflict.values.join(' vs ')})
              </span>
            </li>
          ))}
          {criticalConflicts.length > 3 && (
            <li className="text-red-600">
              +{criticalConflicts.length - 3} more critical conflict
              {criticalConflicts.length - 3 > 1 ? 's' : ''}
            </li>
          )}
        </ul>
        <Link
          to={`/clinical/conflicts/${criticalConflicts[0].conflictId}`}
          className="mt-3 inline-flex items-center gap-1 text-sm font-semibold text-red-700 underline hover:text-red-900"
        >
          Resolve critical conflicts now
        </Link>
      </div>
    </div>
  );
}

/**
 * Warning conflict alert banner (AC-4).
 * Displays below critical conflicts for warning (medium severity) conflicts.
 * Links to conflict resolution page (AC-5).
 */
export function ConflictWarningBanner({ conflicts }: ConflictBannerProps) {
  // Filter for warning (medium severity) conflicts only
  const warningConflicts = conflicts.filter((c) => c.severity === 'medium');

  if (warningConflicts.length === 0) {
    return null;
  }

  return (
    <div
      className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4"
      role="alert"
      aria-live="polite"
    >
      <AlertTriangle className="mt-0.5 size-5 shrink-0 text-amber-600" aria-hidden="true" />
      <div className="flex-1">
        <p className="text-amber-800">
          {warningConflicts.length} data conflict{warningConflicts.length > 1 ? 's' : ''} detected.{' '}
          <Link
            to={`/clinical/conflicts/${warningConflicts[0].conflictId}`}
            className="font-semibold underline hover:text-amber-900"
          >
            Resolve now
          </Link>
        </p>
      </div>
    </div>
  );
}
