import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { AlertTriangle } from 'lucide-react';
import { useState, type ChangeEvent } from 'react';
import type { CodeType } from '../api/codingApi';

export interface ManualCodeOverrideInputProps {
  /** Whether the dialog is open. */
  open: boolean;
  /** Called when dialog should close. */
  onOpenChange: (open: boolean) => void;
  /** The code type being overridden (ICD-10 or CPT). */
  codeType: CodeType;
  /** The original AI-suggested code. */
  originalCode: string;
  /** Original code description. */
  originalDescription: string;
  /** Called when override is submitted. */
  onSubmit: (data: ManualCodeOverrideData) => void;
  /** Whether form submission is in progress. */
  isSubmitting?: boolean;
}

export interface ManualCodeOverrideData {
  code: string;
  description: string;
  overrideReason: string;
  notes?: string;
}

/** Override reasons per UXR-602. */
const OVERRIDE_REASONS = [
  { value: 'more_specific', label: 'More specific code available' },
  { value: 'incorrect_mapping', label: 'Incorrect AI mapping' },
  { value: 'updated_info', label: 'Updated patient information' },
  { value: 'clinical_judgment', label: 'Clinical judgment' },
  { value: 'other', label: 'Other' },
] as const;

/**
 * ManualCodeOverrideInput - Dialog for staff to enter a custom code (AIR-S04).
 * Used when AI suggestions are inadequate or staff has better clinical knowledge.
 */
export function ManualCodeOverrideInput({
  open,
  onOpenChange,
  codeType,
  originalCode,
  originalDescription,
  onSubmit,
  isSubmitting = false,
}: ManualCodeOverrideInputProps) {
  const [code, setCode] = useState('');
  const [description, setDescription] = useState('');
  const [overrideReason, setOverrideReason] = useState('');
  const [notes, setNotes] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const codePattern = codeType === 'ICD-10' ? /^[A-Z]\d{2}(\.\d{1,4})?$/ : /^\d{5}$/;
  const codeFormatHint =
    codeType === 'ICD-10'
      ? 'e.g., E11.65, I10, J45.909'
      : 'e.g., 99213, 36415, 80053';

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!code.trim()) {
      newErrors.code = 'Code is required';
    } else if (!codePattern.test(code.trim().toUpperCase())) {
      newErrors.code = `Invalid ${codeType} format. ${codeFormatHint}`;
    }

    if (!description.trim()) {
      newErrors.description = 'Description is required';
    } else if (description.trim().length < 10) {
      newErrors.description = 'Description must be at least 10 characters';
    }

    if (!overrideReason) {
      newErrors.overrideReason = 'Please select a reason for override';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = () => {
    if (!validate()) return;

    onSubmit({
      code: code.trim().toUpperCase(),
      description: description.trim(),
      overrideReason,
      notes: notes.trim() || undefined,
    });

    // Reset form
    setCode('');
    setDescription('');
    setOverrideReason('');
    setNotes('');
    setErrors({});
  };

  const handleClose = () => {
    setCode('');
    setDescription('');
    setOverrideReason('');
    setNotes('');
    setErrors({});
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Manual Code Override</DialogTitle>
          <DialogDescription>
            Enter a custom {codeType} code to replace the AI suggestion.
          </DialogDescription>
        </DialogHeader>

        {/* Original code info */}
        <div className="rounded-md border border-amber-200 bg-amber-50 p-3">
          <div className="flex items-start gap-2">
            <AlertTriangle className="mt-0.5 size-4 shrink-0 text-amber-600" />
            <div className="text-sm">
              <p className="font-medium text-amber-800">Replacing AI Suggestion:</p>
              <p className="mt-1 text-amber-700">
                <code className="rounded bg-amber-100 px-1">{originalCode}</code>{' '}
                — {originalDescription}
              </p>
            </div>
          </div>
        </div>

        <div className="space-y-4 py-2">
          {/* Code input */}
          <div className="space-y-2">
            <Label htmlFor="override-code">
              {codeType} Code <span className="text-red-500">*</span>
            </Label>
            <Input
              id="override-code"
              placeholder={codeFormatHint}
              value={code}
              onChange={(e: ChangeEvent<HTMLInputElement>) => {
                setCode(e.target.value);
                if (errors.code) setErrors((prev) => ({ ...prev, code: '' }));
              }}
              className={errors.code ? 'border-red-500' : ''}
              aria-invalid={Boolean(errors.code)}
              aria-describedby={errors.code ? 'code-error' : undefined}
            />
            {errors.code && (
              <p id="code-error" className="text-sm text-red-500">
                {errors.code}
              </p>
            )}
          </div>

          {/* Description input */}
          <div className="space-y-2">
            <Label htmlFor="override-description">
              Description <span className="text-red-500">*</span>
            </Label>
            <Input
              id="override-description"
              placeholder="Full code description"
              value={description}
              onChange={(e: ChangeEvent<HTMLInputElement>) => {
                setDescription(e.target.value);
                if (errors.description) setErrors((prev) => ({ ...prev, description: '' }));
              }}
              className={errors.description ? 'border-red-500' : ''}
              aria-invalid={Boolean(errors.description)}
              aria-describedby={errors.description ? 'desc-error' : undefined}
            />
            {errors.description && (
              <p id="desc-error" className="text-sm text-red-500">
                {errors.description}
              </p>
            )}
          </div>

          {/* Override reason */}
          <div className="space-y-2">
            <Label htmlFor="override-reason">
              Reason for Override <span className="text-red-500">*</span>
            </Label>
            <Select
              value={overrideReason}
              onValueChange={(value) => {
                setOverrideReason(value);
                if (errors.overrideReason) setErrors((prev) => ({ ...prev, overrideReason: '' }));
              }}
            >
              <SelectTrigger
                id="override-reason"
                className={errors.overrideReason ? 'border-red-500' : ''}
                aria-invalid={Boolean(errors.overrideReason)}
              >
                <SelectValue placeholder="Select reason" />
              </SelectTrigger>
              <SelectContent>
                {OVERRIDE_REASONS.map((r) => (
                  <SelectItem key={r.value} value={r.value}>
                    {r.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.overrideReason && (
              <p className="text-sm text-red-500">{errors.overrideReason}</p>
            )}
          </div>

          {/* Additional notes */}
          <div className="space-y-2">
            <Label htmlFor="override-notes">Additional Notes</Label>
            <Textarea
              id="override-notes"
              placeholder="Optional notes about the override..."
              value={notes}
              onChange={(e: ChangeEvent<HTMLTextAreaElement>) => setNotes(e.target.value)}
              rows={2}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : 'Save Override'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
