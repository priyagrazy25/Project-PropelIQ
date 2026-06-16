import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { AlertCircle, Loader2 } from 'lucide-react';
import { type FormEvent, useCallback, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import type { InsuranceVerificationStatus } from '../api/intakeApi';
import { verifyInsurance } from '../api/intakeApi';
import { InsuranceStatusBadge } from './InsuranceStatusBadge';

// ── Input sanitisation (edge case: special characters, case-insensitive) ──

function sanitizeInput(value: string): string {
  return value.replace(/[<>"'`;]/g, '').trim();
}

// ── Validation ──

interface FormErrors {
  insuranceName?: string;
  memberId?: string;
}

function validateForm(
  insuranceName: string,
  memberId: string,
): FormErrors | null {
  const errors: FormErrors = {};

  if (!insuranceName.trim()) {
    errors.insuranceName = 'Insurance provider is required.';
  }
  if (!memberId.trim()) {
    errors.memberId = 'Member ID is required.';
  }

  return Object.keys(errors).length > 0 ? errors : null;
}

// ── Result state ──

interface VerificationResult {
  status: InsuranceVerificationStatus;
  details?: string;
  staffNote?: string;
}

// ── Component ──

export function InsurancePreCheckForm() {
  const [insuranceName, setInsuranceName] = useState('');
  const [memberId, setMemberId] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [isLoading, setIsLoading] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const [result, setResult] = useState<VerificationResult | null>(null);

  const abortRef = useRef<AbortController | null>(null);

  const handleSubmit = useCallback(
    async (e: FormEvent) => {
      e.preventDefault();
      setApiError(null);
      setResult(null);

      const sanitizedName = sanitizeInput(insuranceName);
      const sanitizedId = sanitizeInput(memberId);

      const validationErrors = validateForm(sanitizedName, sanitizedId);
      if (validationErrors) {
        setErrors(validationErrors);
        return;
      }
      setErrors({});

      // Abort any in-flight request
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setIsLoading(true);

      const response = await verifyInsurance(
        { insuranceName: sanitizedName, memberId: sanitizedId },
        controller.signal,
      );

      // Guard against aborted requests updating state
      if (controller.signal.aborted) return;

      setIsLoading(false);

      if (response.success) {
        const data = response.data;
        const staffNote =
          data.status === 'unrecognized'
            ? 'Staff follow-up required to confirm insurance details.'
            : undefined;

        setResult({
          status: data.status,
          details: data.details,
          staffNote,
        });
      } else {
        setApiError(response.error.message);
      }
    },
    [insuranceName, memberId],
  );

  return (
    <div className="max-w-150">
      {/* Breadcrumb per wireframe SCR-013 */}
      <nav
        className="flex items-center gap-2 text-sm mb-6"
        aria-label="Breadcrumb"
      >
        <Link to="/dashboard" className="text-primary hover:underline">
          Dashboard
        </Link>
        <span className="text-muted-foreground" aria-hidden="true">
          ›
        </span>
        <span className="text-muted-foreground" aria-current="page">
          Insurance Pre-Check
        </span>
      </nav>

      <h1 className="text-[32px] font-bold leading-10 -tracking-[0.02em] mb-2">
        Insurance Pre-Check
      </h1>
      <p className="text-sm text-muted-foreground mb-8">
        Verify your insurance coverage before your visit.
      </p>

      {/* Card matching wireframe */}
      <div className="rounded-lg border border-border bg-card p-6 shadow-(--shadow-1)">
        <form noValidate onSubmit={(e) => void handleSubmit(e)}>
          {/* Insurance Provider */}
          <div className="flex flex-col gap-1 mb-5">
            <Label htmlFor="ins-name" className="text-sm font-medium">
              Insurance Provider <span className="text-destructive">*</span>
            </Label>
            <Input
              id="ins-name"
              type="text"
              placeholder="e.g., Blue Cross Blue Shield"
              value={insuranceName}
              onChange={(e) => {
                setInsuranceName(e.target.value);
                if (errors.insuranceName) {
                  setErrors((prev) => ({ ...prev, insuranceName: undefined }));
                }
              }}
              className={cn(
                'h-10 text-base',
                errors.insuranceName && 'border-destructive',
              )}
              aria-invalid={!!errors.insuranceName}
              aria-describedby={
                errors.insuranceName ? 'ins-name-error' : undefined
              }
              disabled={isLoading}
            />
            {errors.insuranceName && (
              <span
                id="ins-name-error"
                className="flex items-center gap-1 text-sm text-destructive"
                role="alert"
              >
                <AlertCircle className="size-4" aria-hidden="true" />
                {errors.insuranceName}
              </span>
            )}
          </div>

          {/* Member ID */}
          <div className="flex flex-col gap-1 mb-5">
            <Label htmlFor="ins-member" className="text-sm font-medium">
              Member ID <span className="text-destructive">*</span>
            </Label>
            <Input
              id="ins-member"
              type="text"
              placeholder="e.g., XYZ123456789"
              value={memberId}
              onChange={(e) => {
                setMemberId(e.target.value);
                if (errors.memberId) {
                  setErrors((prev) => ({ ...prev, memberId: undefined }));
                }
              }}
              className={cn(
                'h-10 text-base',
                errors.memberId && 'border-destructive',
              )}
              aria-invalid={!!errors.memberId}
              aria-describedby={
                errors.memberId ? 'ins-member-error' : undefined
              }
              disabled={isLoading}
            />
            {errors.memberId && (
              <span
                id="ins-member-error"
                className="flex items-center gap-1 text-sm text-destructive"
                role="alert"
              >
                <AlertCircle className="size-4" aria-hidden="true" />
                {errors.memberId}
              </span>
            )}
          </div>

          {/* Action bar matching wireframe */}
          <div className="flex gap-3">
            <Button type="submit" className="h-10 px-4" disabled={isLoading}>
              {isLoading ? (
                <>
                  <Loader2 className="size-4 animate-spin" aria-hidden="true" />
                  Verifying…
                </>
              ) : (
                'Verify Coverage'
              )}
            </Button>
            <Link
              to="/intake/summary"
              className={cn(
                'inline-flex h-10 items-center justify-center rounded-lg px-4 text-sm font-medium',
                'text-muted-foreground hover:bg-(--surface-hover)',
                'transition-colors',
              )}
            >
              Skip
            </Link>
          </div>
        </form>

        {/* API error banner (UXR-602, UXR-205) */}
        {apiError && (
          <div
            className="flex items-center gap-2 rounded-md border border-destructive bg-(--surface-danger) p-4 mt-5"
            role="alert"
          >
            <AlertCircle
              className="size-5 text-destructive"
              aria-hidden="true"
            />
            <span className="text-sm text-destructive">{apiError}</span>
          </div>
        )}

        {/* Verification result (AC-2, AC-3, AC-4, edge case: unavailable) */}
        {result && (
          <InsuranceStatusBadge
            status={result.status}
            details={result.details}
            staffNote={result.staffNote}
          />
        )}
      </div>
    </div>
  );
}
