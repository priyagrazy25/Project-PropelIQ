import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Loader2 } from 'lucide-react';
import { useState } from 'react';
import type { ConflictSource } from './ConflictResolutionPanel';

export type ResolutionOption = 'A' | 'B' | 'manual';

export interface ResolutionActionsProps {
  /** Source A details for display in radio option. */
  sourceA: ConflictSource;
  /** Source B details for display in radio option. */
  sourceB: ConflictSource;
  /** Currently selected option. */
  selectedOption: ResolutionOption | null;
  /** Callback when option changes. */
  onOptionChange: (option: ResolutionOption) => void;
  /** Manual value when 'manual' option is selected. */
  manualValue: string;
  /** Callback when manual value changes. */
  onManualValueChange: (value: string) => void;
  /** Resolution notes (optional). */
  notes: string;
  /** Callback when notes change. */
  onNotesChange: (notes: string) => void;
  /** Submit resolution handler. */
  onSubmit: () => void;
  /** Cancel handler. */
  onCancel: () => void;
  /** Whether submission is in progress. */
  isSubmitting?: boolean;
  /** Whether form is valid for submission. */
  isValid?: boolean;
}

/**
 * Resolution action form with radio options (AC-2).
 * Options: Accept Source A, Accept Source B, Enter Manual Value.
 * Includes optional resolution notes and action buttons.
 */
export function ResolutionActions({
  sourceA,
  sourceB,
  selectedOption,
  onOptionChange,
  manualValue,
  onManualValueChange,
  notes,
  onNotesChange,
  onSubmit,
  onCancel,
  isSubmitting = false,
  isValid = true,
}: ResolutionActionsProps) {
  const sourceAConfidence = Math.round(sourceA.confidence * 100);
  const sourceBConfidence = Math.round(sourceB.confidence * 100);

  return (
    <Card>
      <CardHeader className="pb-4">
        <CardTitle className="text-lg font-semibold">Select Resolution</CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        <div
          role="radiogroup"
          aria-label="Choose correct value"
          className="space-y-3"
        >
          {/* Option A */}
          <div
            className="flex cursor-pointer items-center space-x-3 rounded-md border p-3 transition-colors hover:border-primary hover:bg-blue-50/50"
            onClick={() => onOptionChange('A')}
            role="radio"
            aria-checked={selectedOption === 'A'}
            tabIndex={0}
            onKeyDown={(e: React.KeyboardEvent) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onOptionChange('A');
              }
            }}
          >
            <input
              type="radio"
              name="resolution"
              id="res-a"
              checked={selectedOption === 'A'}
              onChange={() => onOptionChange('A')}
              className="size-4 accent-primary"
            />
            <Label htmlFor="res-a" className="flex-1 cursor-pointer text-sm font-medium">
              Use Source A:{' '}
              <strong className="font-semibold">{sourceA.value}</strong>{' '}
              <span className="text-muted-foreground">({sourceAConfidence}% confidence)</span>
            </Label>
          </div>

          {/* Option B */}
          <div
            className="flex cursor-pointer items-center space-x-3 rounded-md border p-3 transition-colors hover:border-primary hover:bg-blue-50/50"
            onClick={() => onOptionChange('B')}
            role="radio"
            aria-checked={selectedOption === 'B'}
            tabIndex={0}
            onKeyDown={(e: React.KeyboardEvent) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onOptionChange('B');
              }
            }}
          >
            <input
              type="radio"
              name="resolution"
              id="res-b"
              checked={selectedOption === 'B'}
              onChange={() => onOptionChange('B')}
              className="size-4 accent-primary"
            />
            <Label htmlFor="res-b" className="flex-1 cursor-pointer text-sm font-medium">
              Use Source B:{' '}
              <strong className="font-semibold">{sourceB.value}</strong>{' '}
              <span className="text-muted-foreground">({sourceBConfidence}% confidence)</span>
            </Label>
          </div>

          {/* Manual Option */}
          <div
            className="flex cursor-pointer items-center space-x-3 rounded-md border p-3 transition-colors hover:border-primary hover:bg-blue-50/50"
            onClick={() => onOptionChange('manual')}
            role="radio"
            aria-checked={selectedOption === 'manual'}
            tabIndex={0}
            onKeyDown={(e: React.KeyboardEvent) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onOptionChange('manual');
              }
            }}
          >
            <input
              type="radio"
              name="resolution"
              id="res-manual"
              checked={selectedOption === 'manual'}
              onChange={() => onOptionChange('manual')}
              className="size-4 accent-primary"
            />
            <Label htmlFor="res-manual" className="flex-1 cursor-pointer text-sm font-medium">
              Enter manually
            </Label>
          </div>
        </div>

        {/* Manual Value Input (shown when manual option is selected) */}
        {selectedOption === 'manual' && (
          <div className="space-y-1.5">
            <Label htmlFor="manual-value" className="text-sm font-medium">
              Correct Value
            </Label>
            <textarea
              id="manual-value"
              value={manualValue}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => onManualValueChange(e.target.value)}
              placeholder="Enter the correct value..."
              className="min-h-[60px] w-full resize-y rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            />
          </div>
        )}

        {/* Resolution Notes */}
        <div className="space-y-1.5">
          <Label htmlFor="resolution-notes" className="text-sm font-medium text-muted-foreground">
            Resolution Notes (optional)
          </Label>
          <textarea
            id="resolution-notes"
            value={notes}
            onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => onNotesChange(e.target.value)}
            placeholder="Add context for this resolution decision..."
            className="min-h-[80px] w-full resize-y rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          />
        </div>

        {/* Action Buttons */}
        <div className="flex gap-3 pt-2">
          <Button
            onClick={onSubmit}
            disabled={!isValid || isSubmitting}
            className="min-w-[140px]"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="mr-2 size-4 animate-spin" />
                Saving...
              </>
            ) : (
              'Save Resolution'
            )}
          </Button>
          <Button
            variant="ghost"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

/**
 * Hook to manage resolution form state.
 * Provides controlled state for all resolution form fields.
 */
export function useResolutionForm() {
  const [selectedOption, setSelectedOption] = useState<ResolutionOption | null>(null);
  const [manualValue, setManualValue] = useState('');
  const [notes, setNotes] = useState('');

  const isValid =
    selectedOption === 'A' ||
    selectedOption === 'B' ||
    (selectedOption === 'manual' && manualValue.trim().length > 0);

  const getResolvedValue = (sourceA: ConflictSource, sourceB: ConflictSource): string => {
    if (selectedOption === 'A') return sourceA.value;
    if (selectedOption === 'B') return sourceB.value;
    if (selectedOption === 'manual') return manualValue.trim();
    return '';
  };

  const reset = () => {
    setSelectedOption(null);
    setManualValue('');
    setNotes('');
  };

  return {
    selectedOption,
    setSelectedOption,
    manualValue,
    setManualValue,
    notes,
    setNotes,
    isValid,
    getResolvedValue,
    reset,
  };
}
