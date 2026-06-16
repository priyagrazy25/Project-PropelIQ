import { cn } from '@/lib/utils';
import { AlertTriangle, CheckCircle, Info, XCircle } from 'lucide-react';
import type { InsuranceVerificationStatus } from '../api/intakeApi';

export type { InsuranceVerificationStatus };

interface StatusConfig {
  icon: React.ReactNode;
  label: string;
  containerClass: string;
  textClass: string;
}

const STATUS_MAP: Record<InsuranceVerificationStatus, StatusConfig> = {
  verified: {
    icon: <CheckCircle className="size-6" aria-hidden="true" />,
    label: 'Coverage Verified',
    containerClass: 'bg-[var(--surface-success)] border-[var(--success)]',
    textClass: 'text-[#16A34A]',
  },
  partial: {
    icon: <AlertTriangle className="size-6" aria-hidden="true" />,
    label: 'Unverified - Member ID mismatch',
    containerClass: 'bg-[var(--surface-warning)] border-[var(--warning)]',
    textClass: 'text-[#D97706]',
  },
  unrecognized: {
    icon: <XCircle className="size-6" aria-hidden="true" />,
    label: 'Unverified - Insurance not recognized',
    containerClass: 'bg-[var(--surface-danger)] border-[var(--destructive)]',
    textClass: 'text-[#DC2626]',
  },
  unavailable: {
    icon: <Info className="size-6" aria-hidden="true" />,
    label: 'Verification unavailable',
    containerClass: 'bg-[var(--muted)] border-[var(--border)]',
    textClass: 'text-[var(--muted-foreground)]',
  },
};

export interface InsuranceStatusBadgeProps {
  status: InsuranceVerificationStatus;
  details?: string;
  staffNote?: string;
}

export function InsuranceStatusBadge({
  status,
  details,
  staffNote,
}: InsuranceStatusBadgeProps) {
  const config = STATUS_MAP[status];

  return (
    <div
      className={cn(
        'flex items-center gap-3 rounded-lg border p-4 mt-5',
        config.containerClass,
      )}
      role="status"
      aria-live="polite"
    >
      <span className={config.textClass}>{config.icon}</span>
      <div className="flex flex-col gap-0.5">
        <span className={cn('text-sm font-medium', config.textClass)}>
          {config.label}
        </span>
        {details && (
          <span className="text-[13px] text-muted-foreground">{details}</span>
        )}
        {staffNote && (
          <span className="text-[13px] text-muted-foreground mt-1 italic">
            {staffNote}
          </span>
        )}
      </div>
    </div>
  );
}
