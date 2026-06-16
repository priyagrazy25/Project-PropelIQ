import { Button } from '@/components/ui/button';
import { Check, Edit2, X } from 'lucide-react';

export interface VerificationActionsProps {
  /** Entry ID for callbacks. */
  entryId: string;
  /** Whether actions should be disabled. */
  disabled?: boolean;
  /** Called when Accept is clicked. */
  onAccept: (entryId: string) => void;
  /** Called when Reject is clicked. */
  onReject: (entryId: string) => void;
  /** Called when Override is clicked. */
  onOverride?: (entryId: string) => void;
  /** Show override button. */
  showOverride?: boolean;
}

/**
 * VerificationActions - Accept/Reject/Override buttons for code verification (AIR-S04).
 * Used in CodeCandidateCard for inline status updates without page reload (AC-4).
 */
export function VerificationActions({
  entryId,
  disabled = false,
  onAccept,
  onReject,
  onOverride,
  showOverride = true,
}: VerificationActionsProps) {
  return (
    <div className="flex shrink-0 items-center gap-2">
      <Button
        size="sm"
        variant="default"
        className="bg-emerald-600 hover:bg-emerald-700"
        onClick={() => onAccept(entryId)}
        disabled={disabled}
        aria-label="Accept code"
      >
        <Check className="mr-1 size-4" aria-hidden="true" />
        Accept
      </Button>
      <Button
        size="sm"
        variant="destructive"
        onClick={() => onReject(entryId)}
        disabled={disabled}
        aria-label="Reject code"
      >
        <X className="mr-1 size-4" aria-hidden="true" />
        Reject
      </Button>
      {showOverride && onOverride && (
        <Button
          size="sm"
          variant="outline"
          onClick={() => onOverride(entryId)}
          disabled={disabled}
          aria-label="Override with custom code"
        >
          <Edit2 className="mr-1 size-4" aria-hidden="true" />
          Override
        </Button>
      )}
    </div>
  );
}
