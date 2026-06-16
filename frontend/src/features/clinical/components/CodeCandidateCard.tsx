import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { ChevronDown, ChevronUp, Check, Edit2, X } from 'lucide-react';
import { useState } from 'react';
import type { CodeCandidate, CodeEntry, CodeType, VerificationStatus } from '../api/codingApi';

export interface CodeCandidateCardProps {
  entry: CodeEntry;
  onAccept: (entryId: string) => void;
  onReject: (entryId: string) => void;
  onOverride?: (entryId: string) => void;
  disabled?: boolean;
}

/**
 * Returns confidence badge styling based on threshold (AC-2).
 * Green ≥0.7, Amber 0.5-0.7, Red <0.5
 */
function getConfidenceStyle(confidence: number): {
  className: string;
  label: string;
} {
  if (confidence >= 0.7) {
    return {
      className: 'bg-emerald-50 text-emerald-700',
      label: 'High',
    };
  }
  if (confidence >= 0.5) {
    return {
      className: 'bg-amber-50 text-amber-700',
      label: 'Medium',
    };
  }
  return {
    className: 'bg-red-50 text-red-700',
    label: 'Low',
  };
}

/**
 * Returns status badge variant based on verification status.
 */
function getStatusBadge(status: VerificationStatus): {
  variant: 'default' | 'secondary' | 'destructive' | 'outline';
  className: string;
} {
  switch (status) {
    case 'Accepted':
      return { variant: 'secondary', className: 'bg-emerald-50 text-emerald-700 border-emerald-200' };
    case 'Rejected':
      return { variant: 'destructive', className: '' };
    default:
      return { variant: 'outline', className: 'bg-blue-50 text-blue-700 border-blue-200' };
  }
}

/**
 * Returns code type badge color.
 */
function getCodeTypeStyle(codeType: CodeType): string {
  return codeType === 'ICD-10'
    ? 'bg-purple-50 text-purple-700'
    : 'bg-blue-50 text-blue-700';
}

/**
 * Single code candidate display with confidence.
 */
function CandidateRow({
  candidate,
  rank,
  isPrimary,
}: {
  candidate: CodeCandidate;
  rank: number;
  isPrimary: boolean;
}) {
  const { className } = getConfidenceStyle(candidate.confidence);
  const percentage = Math.round(candidate.confidence * 100);

  return (
    <div
      className={cn(
        'flex items-center gap-3 rounded-md px-3 py-2',
        isPrimary ? 'bg-gray-50' : 'bg-white'
      )}
    >
      <span
        className={cn(
          'flex size-6 shrink-0 items-center justify-center rounded-full text-xs font-semibold',
          isPrimary ? 'bg-primary text-white' : 'bg-gray-200 text-gray-600'
        )}
      >
        {rank}
      </span>
      <code className="shrink-0 rounded bg-gray-100 px-2 py-0.5 font-mono text-sm">
        {candidate.code}
      </code>
      <span className="flex-1 truncate text-sm text-gray-700">
        {candidate.description}
      </span>
      <span
        className={cn(
          'shrink-0 rounded px-2 py-0.5 text-xs font-semibold',
          className
        )}
      >
        {percentage}%
      </span>
    </div>
  );
}

/**
 * CodeCandidateCard displays a code entry with top-3 candidates.
 * Implements AC-2 (confidence ranking) and UXR-601 (code review workflow).
 */
export function CodeCandidateCard({
  entry,
  onAccept,
  onReject,
  onOverride,
  disabled,
}: CodeCandidateCardProps) {
  const [expanded, setExpanded] = useState(false);
  const statusBadge = getStatusBadge(entry.status);
  const codeTypeStyle = getCodeTypeStyle(entry.codeType);
  const allCandidatesBelowThreshold =
    entry.primaryCode.confidence < 0.5 &&
    entry.alternativeCandidates.every((c) => c.confidence < 0.5);

  const isPending = entry.status === 'Pending';

  return (
    <div className="rounded-lg border bg-white shadow-sm">
      {/* Main row */}
      <div className="flex items-center gap-4 p-4">
        {/* Patient name */}
        <div className="w-32 shrink-0">
          <span className="text-sm font-medium text-gray-900">
            {entry.patientName}
          </span>
        </div>

        {/* Primary code */}
        <code className="shrink-0 rounded bg-gray-100 px-2 py-1 font-mono text-sm font-medium">
          {entry.primaryCode.code}
        </code>

        {/* Description */}
        <div className="flex-1 min-w-0">
          <p className="truncate text-sm text-gray-700">
            {entry.primaryCode.description}
          </p>
          {entry.sourceDiagnosis && (
            <p className="truncate text-xs text-gray-500">
              Source: {entry.sourceDiagnosis}
            </p>
          )}
          {entry.sourceProcedure && (
            <p className="truncate text-xs text-gray-500">
              Source: {entry.sourceProcedure}
            </p>
          )}
        </div>

        {/* Code type badge */}
        <Badge variant="outline" className={cn('shrink-0', codeTypeStyle)}>
          {entry.codeType}
        </Badge>

        {/* Confidence */}
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <span
                className={cn(
                  'shrink-0 rounded px-2 py-1 text-sm font-semibold',
                  getConfidenceStyle(entry.primaryCode.confidence).className
                )}
              >
                {Math.round(entry.primaryCode.confidence * 100)}%
              </span>
            </TooltipTrigger>
            <TooltipContent>
              <p>
                {getConfidenceStyle(entry.primaryCode.confidence).label} confidence
              </p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>

        {/* Status badge */}
        <Badge variant={statusBadge.variant} className={cn('shrink-0', statusBadge.className)}>
          {entry.status === 'Pending' && allCandidatesBelowThreshold
            ? 'Low Confidence'
            : entry.status}
        </Badge>

        {/* Actions */}
        <div className="flex shrink-0 items-center gap-2">
          {isPending ? (
            <>
              <Button
                size="sm"
                variant="default"
                className="bg-emerald-600 hover:bg-emerald-700"
                onClick={() => onAccept(entry.id)}
                disabled={disabled}
                aria-label={`Accept ${entry.primaryCode.code}`}
              >
                <Check className="mr-1 size-4" />
                Accept
              </Button>
              <Button
                size="sm"
                variant="destructive"
                onClick={() => onReject(entry.id)}
                disabled={disabled}
                aria-label={`Reject ${entry.primaryCode.code}`}
              >
                <X className="mr-1 size-4" />
                Reject
              </Button>
              {onOverride && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => onOverride(entry.id)}
                  disabled={disabled}
                  aria-label={`Override ${entry.primaryCode.code} with custom code`}
                >
                  <Edit2 className="mr-1 size-4" />
                  Override
                </Button>
              )}
            </>
          ) : (
            <span className="text-sm text-gray-500">
              {entry.verifiedBy ? `By ${entry.verifiedBy}` : 'Reviewed'}
            </span>
          )}

          {/* Expand toggle for alternatives */}
          {entry.alternativeCandidates.length > 0 && (
            <Button
              size="sm"
              variant="ghost"
              onClick={() => setExpanded(!expanded)}
              aria-expanded={expanded}
              aria-label={expanded ? 'Hide alternatives' : 'Show alternatives'}
            >
              {expanded ? (
                <ChevronUp className="size-4" />
              ) : (
                <ChevronDown className="size-4" />
              )}
            </Button>
          )}
        </div>
      </div>

      {/* Low confidence warning */}
      {allCandidatesBelowThreshold && isPending && (
        <div className="border-t border-amber-200 bg-amber-50 px-4 py-2">
          <p className="text-sm text-amber-800">
            ⚠️ All candidates below 50% confidence. Manual code entry may be required.
          </p>
        </div>
      )}

      {/* Expanded alternatives */}
      {expanded && entry.alternativeCandidates.length > 0 && (
        <div className="space-y-1 border-t bg-gray-50 p-3">
          <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-gray-500">
            Alternative Candidates
          </p>
          <CandidateRow
            candidate={entry.primaryCode}
            rank={1}
            isPrimary
          />
          {entry.alternativeCandidates.map((candidate, idx) => (
            <CandidateRow
              key={candidate.code}
              candidate={candidate}
              rank={idx + 2}
              isPrimary={false}
            />
          ))}
        </div>
      )}
    </div>
  );
}

/**
 * Empty state for no diagnoses available.
 */
export function NoDiagnosesMessage() {
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-dashed bg-gray-50 py-12">
      <div className="text-4xl">🩺</div>
      <p className="mt-3 text-lg font-medium text-gray-900">
        No diagnoses available for coding
      </p>
      <p className="mt-1 text-sm text-gray-500">
        Diagnoses will appear here once extracted from clinical documents.
      </p>
    </div>
  );
}

/**
 * Empty state for no procedures available.
 */
export function NoProceduresMessage() {
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-dashed bg-gray-50 py-12">
      <div className="text-4xl">📋</div>
      <p className="mt-3 text-lg font-medium text-gray-900">
        No procedures available for coding
      </p>
      <p className="mt-1 text-sm text-gray-500">
        Procedures will appear here once extracted from clinical documents.
      </p>
    </div>
  );
}
