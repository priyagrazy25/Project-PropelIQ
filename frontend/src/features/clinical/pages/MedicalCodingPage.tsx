import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Textarea } from '@/components/ui/textarea';
import { AlertTriangle, ChevronRight, Filter, TrendingUp } from 'lucide-react';
import { useEffect, useState, type ChangeEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  fetchAgreementRate,
  fetchCodeVerificationQueue,
  getDemoAgreementRate,
  getDemoCodeVerificationQueue,
  overrideCode,
  verifyCode,
  type AgreementRateResponse,
  type CodeEntry,
  type CodeType,
  type CodeVerificationQueueResponse,
  type VerificationStatus,
} from '../api/codingApi';
import {
  CodeCandidateCard,
  NoDiagnosesMessage,
  NoProceduresMessage,
} from '../components/CodeCandidateCard';
import {
  ManualCodeOverrideInput,
  type ManualCodeOverrideData,
} from '../components/ManualCodeOverrideInput';

/** Rejection reasons per UXR-602. */
const REJECTION_REASONS = [
  { value: 'incorrect', label: 'Incorrect code' },
  { value: 'insufficient', label: 'Insufficient context' },
  { value: 'duplicate', label: 'Duplicate entry' },
  { value: 'other', label: 'Other' },
] as const;

/**
 * Medical Coding Page (SCR-018).
 * Unified view for ICD-10 diagnosis codes and CPT procedure codes.
 * Implements US_038 AC-2 (ICD-10 top-3) and US_039 AC-2 (CPT top-3).
 */
export function MedicalCodingPage() {
  const [data, setData] = useState<CodeVerificationQueueResponse | null>(null);
  const [agreementRate, setAgreementRate] = useState<AgreementRateResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [processingId, setProcessingId] = useState<string | null>(null);

  // Filters
  const [statusFilter, setStatusFilter] = useState<VerificationStatus | 'all'>('all');
  const [typeFilter, setTypeFilter] = useState<CodeType | 'all'>('all');

  // Rejection dialog state
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false);
  const [rejectingEntry, setRejectingEntry] = useState<CodeEntry | null>(null);
  const [rejectReason, setRejectReason] = useState<string>('');
  const [rejectNotes, setRejectNotes] = useState<string>('');

  // Override dialog state
  const [overrideDialogOpen, setOverrideDialogOpen] = useState(false);
  const [overridingEntry, setOverridingEntry] = useState<CodeEntry | null>(null);
  const [isOverriding, setIsOverriding] = useState(false);

  // Load agreement rate on mount
  useEffect(() => {
    const loadAgreementRate = async () => {
      const result = await fetchAgreementRate();
      if (result.success) {
        setAgreementRate(result.data);
      } else {
        // Fallback to demo data
        setAgreementRate(getDemoAgreementRate());
      }
    };
    void loadAgreementRate();
  }, []);

  useEffect(() => {
    const loadData = async () => {
      setIsLoading(true);
      setError(null);

      const filters = {
        status: statusFilter !== 'all' ? statusFilter : undefined,
        codeType: typeFilter !== 'all' ? typeFilter : undefined,
      };

      const result = await fetchCodeVerificationQueue(filters);
      if (result.success) {
        setData(result.data);
      } else {
        // Fallback to demo data
        setData(getDemoCodeVerificationQueue());
        setError('Using demo data - backend unavailable');
      }
      setIsLoading(false);
    };

    void loadData();
  }, [statusFilter, typeFilter]);

  // Filter entries by type for display
  const icd10Entries = data?.entries.filter((e) => e.codeType === 'ICD-10') ?? [];
  const cptEntries = data?.entries.filter((e) => e.codeType === 'CPT') ?? [];

  // Apply status filter for display
  const filterByStatus = (entries: CodeEntry[]) => {
    if (statusFilter === 'all') return entries;
    return entries.filter((e) => e.status === statusFilter);
  };

  const filteredIcd10 = typeFilter === 'CPT' ? [] : filterByStatus(icd10Entries);
  const filteredCpt = typeFilter === 'ICD-10' ? [] : filterByStatus(cptEntries);

  const handleAccept = (entryId: string) => {
    const doAccept = async () => {
      setProcessingId(entryId);
      const result = await verifyCode(entryId, { action: 'accept' });
      if (result.success) {
        // Update local state optimistically
        setData((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            entries: prev.entries.map((e) =>
              e.id === entryId ? { ...e, status: 'Accepted' as VerificationStatus } : e
            ),
            pendingCount: prev.pendingCount - 1,
          };
        });
      } else {
        setError(result.error);
      }
      setProcessingId(null);
    };
    void doAccept();
  };

  const handleRejectClick = (entryId: string) => {
    const entry = data?.entries.find((e) => e.id === entryId);
    if (entry) {
      setRejectingEntry(entry);
      setRejectReason('');
      setRejectNotes('');
      setRejectDialogOpen(true);
    }
  };

  const handleConfirmReject = () => {
    if (!rejectingEntry || !rejectReason) return;

    const doReject = async () => {
      setProcessingId(rejectingEntry.id);
      setRejectDialogOpen(false);

      const result = await verifyCode(rejectingEntry.id, {
        action: 'reject',
        reason: rejectReason,
        notes: rejectNotes || undefined,
      });

      if (result.success) {
        setData((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            entries: prev.entries.map((e) =>
              e.id === rejectingEntry.id
                ? { ...e, status: 'Rejected' as VerificationStatus, rejectionReason: rejectReason }
                : e
            ),
            pendingCount: prev.pendingCount - 1,
          };
        });
      } else {
        setError(result.error);
      }

      setProcessingId(null);
      setRejectingEntry(null);
    };

    void doReject();
  };

  const handleOverrideClick = (entryId: string) => {
    const entry = data?.entries.find((e) => e.id === entryId);
    if (entry) {
      setOverridingEntry(entry);
      setOverrideDialogOpen(true);
    }
  };

  const handleConfirmOverride = (overrideData: ManualCodeOverrideData) => {
    if (!overridingEntry) return;

    const doOverride = async () => {
      setIsOverriding(true);

      const result = await overrideCode(overridingEntry.id, overrideData);

      if (result.success) {
        setData((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            entries: prev.entries.map((e) =>
              e.id === overridingEntry.id
                ? {
                    ...e,
                    status: 'Accepted' as VerificationStatus,
                    primaryCode: {
                      code: overrideData.code,
                      description: overrideData.description,
                      confidence: 1.0, // Manual override = 100% confidence
                    },
                  }
                : e
            ),
            pendingCount: prev.pendingCount - 1,
          };
        });
        setOverrideDialogOpen(false);
        setOverridingEntry(null);
      } else {
        setError(result.error);
      }

      setIsOverriding(false);
    };

    void doOverride();
  };

  if (isLoading) {
    return <MedicalCodingPageSkeleton />;
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-1 text-sm" aria-label="Breadcrumb">
        <Link to="/staff/dashboard" className="text-primary hover:underline">
          Dashboard
        </Link>
        <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
        <span className="text-muted-foreground" aria-current="page">
          Code Verification
        </span>
      </nav>

      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Code Verification</h1>
          {data && (
            <p className="mt-1 text-sm text-muted-foreground">
              {data.pendingCount} pending of {data.totalCount} total codes
            </p>
          )}
        </div>
        <div className="flex items-center gap-3">
          {error && (
            <Badge variant="outline" className="bg-amber-50 text-amber-700">
              Demo Mode
            </Badge>
          )}
        </div>
      </div>

      {/* Agreement Rate Metric */}
      {agreementRate && (
        <Card className="border-emerald-200 bg-gradient-to-r from-emerald-50 to-white">
          <CardContent className="flex items-center justify-between p-4">
            <div className="flex items-center gap-3">
              <div className="flex size-10 items-center justify-center rounded-full bg-emerald-100">
                <TrendingUp className="size-5 text-emerald-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">30-Day Agreement Rate</p>
                <p className="text-2xl font-bold text-emerald-700">
                  {(agreementRate.rate ?? 0).toFixed(1)}%
                </p>
              </div>
            </div>
            <div className="flex gap-6 text-sm">
              <div className="text-center">
                <p className="font-semibold text-gray-900">{agreementRate.acceptedCount ?? 0}</p>
                <p className="text-gray-500">Accepted</p>
              </div>
              <div className="text-center">
                <p className="font-semibold text-gray-900">{agreementRate.rejectedCount ?? 0}</p>
                <p className="text-gray-500">Rejected</p>
              </div>
              <div className="text-center">
                <p className="font-semibold text-gray-900">{agreementRate.overriddenCount ?? 0}</p>
                <p className="text-gray-500">Overridden</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Filters */}
      <div className="flex items-center gap-3">
        <Filter className="size-4 text-muted-foreground" aria-hidden="true" />
        <Select
          value={statusFilter}
          onValueChange={(v) => setStatusFilter(v as VerificationStatus | 'all')}
        >
          <SelectTrigger className="w-[150px]" aria-label="Filter by status">
            <SelectValue placeholder="All Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Status</SelectItem>
            <SelectItem value="Pending">Pending</SelectItem>
            <SelectItem value="Accepted">Accepted</SelectItem>
            <SelectItem value="Rejected">Rejected</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={typeFilter}
          onValueChange={(v) => setTypeFilter(v as CodeType | 'all')}
        >
          <SelectTrigger className="w-[150px]" aria-label="Filter by type">
            <SelectValue placeholder="All Types" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Types</SelectItem>
            <SelectItem value="ICD-10">ICD-10</SelectItem>
            <SelectItem value="CPT">CPT</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Error Banner */}
      {error && !data && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex items-center gap-3 p-4">
            <AlertTriangle className="size-5 text-red-600" />
            <span className="text-red-800">{error}</span>
          </CardContent>
        </Card>
      )}

      {/* ICD-10 Section */}
      <section aria-labelledby="icd10-heading">
        <h2
          id="icd10-heading"
          className="mb-4 flex items-center gap-2 text-xl font-semibold"
        >
          <Badge variant="outline" className="bg-purple-50 text-purple-700">
            ICD-10
          </Badge>
          Diagnosis Codes
          {filteredIcd10.length > 0 && (
            <span className="text-sm font-normal text-muted-foreground">
              ({filteredIcd10.length})
            </span>
          )}
        </h2>

        {typeFilter === 'CPT' ? null : filteredIcd10.length === 0 ? (
          <NoDiagnosesMessage />
        ) : (
          <div className="space-y-3">
            {filteredIcd10.map((entry) => (
              <CodeCandidateCard
                key={entry.id}
                entry={entry}
                onAccept={handleAccept}
                onReject={handleRejectClick}
                onOverride={handleOverrideClick}
                disabled={processingId === entry.id}
              />
            ))}
          </div>
        )}
      </section>

      {/* CPT Section */}
      <section aria-labelledby="cpt-heading" className="mt-8">
        <h2
          id="cpt-heading"
          className="mb-4 flex items-center gap-2 text-xl font-semibold"
        >
          <Badge variant="outline" className="bg-blue-50 text-blue-700">
            CPT
          </Badge>
          Procedure Codes
          {filteredCpt.length > 0 && (
            <span className="text-sm font-normal text-muted-foreground">
              ({filteredCpt.length})
            </span>
          )}
        </h2>

        {typeFilter === 'ICD-10' ? null : filteredCpt.length === 0 ? (
          <NoProceduresMessage />
        ) : (
          <div className="space-y-3">
            {filteredCpt.map((entry) => (
              <CodeCandidateCard
                key={entry.id}
                entry={entry}
                onAccept={handleAccept}
                onReject={handleRejectClick}
                onOverride={handleOverrideClick}
                disabled={processingId === entry.id}
              />
            ))}
          </div>
        )}
      </section>

      {/* Rejection Dialog (OVL-007) */}
      <Dialog open={rejectDialogOpen} onOpenChange={setRejectDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reject Code</DialogTitle>
            <DialogDescription>
              Rejecting code{' '}
              <code className="rounded bg-gray-100 px-1">
                {rejectingEntry?.primaryCode.code}
              </code>
              . Please provide a reason.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="reject-reason">Reason</Label>
              <Select value={rejectReason} onValueChange={setRejectReason}>
                <SelectTrigger id="reject-reason">
                  <SelectValue placeholder="Select reason" />
                </SelectTrigger>
                <SelectContent>
                  {REJECTION_REASONS.map((r) => (
                    <SelectItem key={r.value} value={r.value}>
                      {r.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="reject-notes">Additional Notes</Label>
              <Textarea
                id="reject-notes"
                placeholder="Provide details for rejection..."
                value={rejectNotes}
                onChange={(e: ChangeEvent<HTMLTextAreaElement>) => setRejectNotes(e.target.value)}
                rows={3}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setRejectDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleConfirmReject}
              disabled={!rejectReason}
            >
              Confirm Rejection
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Override Dialog */}
      {overridingEntry && (
        <ManualCodeOverrideInput
          open={overrideDialogOpen}
          onOpenChange={setOverrideDialogOpen}
          codeType={overridingEntry.codeType}
          originalCode={overridingEntry.primaryCode.code}
          originalDescription={overridingEntry.primaryCode.description}
          onSubmit={handleConfirmOverride}
          isSubmitting={isOverriding}
        />
      )}
    </div>
  );
}

/**
 * Loading skeleton for the Medical Coding page.
 */
function MedicalCodingPageSkeleton() {
  return (
    <div className="mx-auto max-w-6xl space-y-6 p-6">
      <Skeleton className="h-4 w-48" />
      <Skeleton className="h-10 w-64" />
      <div className="flex gap-3">
        <Skeleton className="h-10 w-[150px]" />
        <Skeleton className="h-10 w-[150px]" />
      </div>

      <div className="space-y-3">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-24 w-full" />
      </div>

      <div className="space-y-3">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-24 w-full" />
      </div>
    </div>
  );
}
