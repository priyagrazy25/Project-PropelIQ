import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { AlertCircle, AlertTriangle, CheckCircle2, ChevronRight } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
  fetchConflictDetail,
  getDemoConflictDetail,
  resolveConflict,
  type ConflictDetail,
} from '../api/patient360Api';
import {
  ConflictResolutionPanel,
  type ConflictDetails,
} from '../components/ConflictResolutionPanel';
import {
  ResolutionActions,
  useResolutionForm,
  type ResolutionOption,
} from '../components/ResolutionActions';

/**
 * Conflict Resolution Page (SCR-017).
 * Provides side-by-side comparison of conflicting values with resolution actions.
 * Implements AC-1 (side-by-side), AC-2 (resolution options), AC-5 (incremental update).
 */
export function ConflictResolutionPage() {
  const { conflictId } = useParams<{ conflictId: string }>();
  const navigate = useNavigate();

  const [conflict, setConflict] = useState<ConflictDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showConcurrencyWarning, setShowConcurrencyWarning] = useState(false);

  const {
    selectedOption,
    setSelectedOption,
    manualValue,
    setManualValue,
    notes,
    setNotes,
    isValid,
    getResolvedValue,
  } = useResolutionForm();

  // Load conflict details
  useEffect(() => {
    const loadConflict = async () => {
      if (!conflictId) {
        setError('No conflict ID provided.');
        setIsLoading(false);
        return;
      }

      setIsLoading(true);
      setError(null);

      const result = await fetchConflictDetail(conflictId);
      if (result.success) {
        // Check if already resolved
        if (result.data.resolutionStatus !== 'Open') {
          setError('This conflict has already been resolved.');
        } else {
          setConflict(result.data);
        }
      } else {
        // Fall back to demo data in dev mode
        const isDev = import.meta.env.DEV;
        if (isDev) {
          setConflict(getDemoConflictDetail(conflictId));
        } else {
          setError(result.error);
        }
      }
      setIsLoading(false);
    };

    void loadConflict();
  }, [conflictId]);

  // Handle panel click to select source
  const handleSelectSource = useCallback(
    (source: 'A' | 'B') => {
      setSelectedOption(source);
    },
    [setSelectedOption]
  );

  // Handle resolution submission
  const handleSubmit = useCallback(async () => {
    if (!conflict || !isValid || !selectedOption) return;

    const sourceA = {
      sourceLabel: conflict.sourceA.documentName,
      value: conflict.sourceA.value,
      confidence: conflict.sourceA.confidence,
      extractedAt: conflict.sourceA.extractedAt,
    };
    const sourceB = {
      sourceLabel: conflict.sourceB.documentName,
      value: conflict.sourceB.value,
      confidence: conflict.sourceB.confidence,
      extractedAt: conflict.sourceB.extractedAt,
    };

    const resolvedValue = getResolvedValue(sourceA, sourceB);

    setIsSubmitting(true);
    setShowConcurrencyWarning(false);

    const result = await resolveConflict({
      conflictId: conflict.conflictId,
      resolvedValue,
      resolutionSource: selectedOption,
      notes: notes.trim() || undefined,
    });

    setIsSubmitting(false);

    if (result.success) {
      toast.success('Conflict resolved successfully', {
        description: `Value set to: ${resolvedValue}`,
      });
      // Navigate back to health profile (AC-5: incremental update)
      navigate('/health-profile', { state: { conflictResolved: conflict.conflictId } });
    } else {
      if (result.isOptimisticConflict) {
        // Show concurrency warning (optimistic concurrency handling)
        setShowConcurrencyWarning(true);
        toast.error('Conflict already resolved', {
          description: 'Another user resolved this conflict. Please refresh.',
        });
      } else {
        toast.error('Failed to resolve conflict', {
          description: result.error,
        });
      }
    }
  }, [conflict, isValid, selectedOption, notes, getResolvedValue, navigate]);

  // Handle cancel
  const handleCancel = useCallback(() => {
    navigate('/health-profile');
  }, [navigate]);

  // Loading state
  if (isLoading) {
    return <ConflictResolutionSkeleton />;
  }

  // Error state
  if (error || !conflict) {
    return (
      <div className="mx-auto max-w-4xl space-y-6 p-6">
        <Card className="border-amber-200 bg-amber-50">
          <CardContent className="flex items-center gap-3 p-4">
            <AlertTriangle className="size-5 text-amber-600" />
            <span className="text-amber-800">{error ?? 'Conflict not found.'}</span>
          </CardContent>
        </Card>
        <Button onClick={() => navigate('/health-profile')}>Back to Health Profile</Button>
      </div>
    );
  }

  // Convert API data to component props
  const conflictDetails: ConflictDetails = {
    conflictId: conflict.conflictId,
    field: conflict.field,
    category: conflict.category,
    sourceA: {
      sourceLabel: conflict.sourceA.documentName,
      value: conflict.sourceA.value,
      confidence: conflict.sourceA.confidence,
      extractedAt: conflict.sourceA.extractedAt,
      documentName: conflict.sourceA.documentName,
    },
    sourceB: {
      sourceLabel: conflict.sourceB.documentName,
      value: conflict.sourceB.value,
      confidence: conflict.sourceB.confidence,
      extractedAt: conflict.sourceB.extractedAt,
      documentName: conflict.sourceB.documentName,
    },
  };

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-1 text-sm" aria-label="Breadcrumb">
        <Link to="/dashboard" className="text-primary hover:underline">
          Dashboard
        </Link>
        <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
        <Link to="/health-profile" className="text-primary hover:underline">
          Health Profile
        </Link>
        <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
        <span className="text-muted-foreground" aria-current="page">
          Resolve Conflict
        </span>
      </nav>

      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold">Resolve Data Conflict</h1>
        <p className="mt-1 text-muted-foreground">
          The same field has different values from two sources. Select the correct value.
        </p>
      </div>

      {/* Concurrency Warning */}
      {showConcurrencyWarning && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex items-center gap-3 p-4">
            <AlertCircle className="size-5 shrink-0 text-red-600" />
            <p className="text-red-800">
              This conflict was already resolved by another user.{' '}
              <button
                type="button"
                className="font-semibold underline"
                onClick={() => window.location.reload()}
              >
                Refresh the page
              </button>{' '}
              to see the current state.
            </p>
          </CardContent>
        </Card>
      )}

      {/* Side-by-side Comparison Panels (AC-1, UXR-108) */}
      <ConflictResolutionPanel
        conflict={conflictDetails}
        selectedOption={selectedOption}
        onSelectSource={handleSelectSource}
      />

      {/* Resolution Form (AC-2) */}
      <ResolutionActions
        sourceA={conflictDetails.sourceA}
        sourceB={conflictDetails.sourceB}
        selectedOption={selectedOption}
        onOptionChange={setSelectedOption as (option: ResolutionOption) => void}
        manualValue={manualValue}
        onManualValueChange={setManualValue}
        notes={notes}
        onNotesChange={setNotes}
        onSubmit={handleSubmit}
        onCancel={handleCancel}
        isSubmitting={isSubmitting}
        isValid={isValid}
      />

      {/* Resolution Help Text */}
      <div className="flex items-start gap-2 text-sm text-muted-foreground">
        <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-emerald-500" />
        <p>
          After resolving this conflict, the 360-degree health profile will be updated
          automatically with your selected value.
        </p>
      </div>
    </div>
  );
}

/** Loading skeleton for conflict resolution page. */
function ConflictResolutionSkeleton() {
  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      {/* Breadcrumb */}
      <Skeleton className="h-4 w-48" />

      {/* Header */}
      <div className="space-y-2">
        <Skeleton className="h-9 w-64" />
        <Skeleton className="h-5 w-96" />
      </div>

      {/* Comparison Panels */}
      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardContent className="space-y-4 p-5">
            <div className="flex justify-between">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-4 w-24" />
            </div>
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-1/2" />
          </CardContent>
        </Card>
        <Card>
          <CardContent className="space-y-4 p-5">
            <div className="flex justify-between">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-4 w-24" />
            </div>
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-1/2" />
          </CardContent>
        </Card>
      </div>

      {/* Resolution Form */}
      <Card>
        <CardContent className="space-y-4 p-6">
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-20 w-full" />
          <div className="flex gap-3">
            <Skeleton className="h-10 w-36" />
            <Skeleton className="h-10 w-20" />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
