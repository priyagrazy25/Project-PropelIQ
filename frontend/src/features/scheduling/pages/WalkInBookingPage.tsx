import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { ErrorBanner } from '@/shared/components/ErrorBanner';
import { useCallback, useEffect, useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import {
  bookWalkIn,
  createPatient,
  fetchAvailableProviders,
  fetchSameDaySlots,
  searchPatients,
  type AvailableProvider,
} from '../api/schedulingApi';
import {
  CreatePatientModal,
  type NewPatientData,
} from '../components/CreatePatientModal';
import {
  PatientSearchBar,
  type PatientSearchResult,
} from '../components/PatientSearchBar';
import {
  SameDaySlotPicker,
  type SameDaySlot,
} from '../components/SameDaySlotPicker';

export function WalkInBookingPage() {
  const navigate = useNavigate();
  const location = useLocation();

  // Determine the correct base path for navigation
  const isAdminRoute = location.pathname.startsWith('/management');
  const dashboardPath = isAdminRoute ? '/management/dashboard' : '/staff/dashboard';
  const queuePath = isAdminRoute ? '/management/queue' : '/staff/queue';

  // Patient state
  const [selectedPatient, setSelectedPatient] =
    useState<PatientSearchResult | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isCreatingPatient, setIsCreatingPatient] = useState(false);

  // Appointment detail state
  const [visitType, setVisitType] = useState('');
  const [providerId, setProviderId] = useState('');
  const [reason, setReason] = useState('');

  // Providers & slots state
  const [providers, setProviders] = useState<AvailableProvider[]>([]);
  const [providersLoading, setProvidersLoading] = useState(true);
  const [providersError, setProvidersError] = useState<string | null>(null);
  const [slots, setSlots] = useState<SameDaySlot[]>([]);
  const [slotsLoading, setSlotsLoading] = useState(false);
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null);
  const [noSlotsAvailable, setNoSlotsAvailable] = useState(false);
  const [estimatedWaitTime, setEstimatedWaitTime] = useState<string | null>(
    null,
  );

  // Submission state
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Load available providers
  const loadProviders = useCallback(() => {
    setProvidersLoading(true);
    setProvidersError(null);
    void (async () => {
      const result = await fetchAvailableProviders();
      if (result.success) {
        setProviders(result.data);
      } else {
        setProvidersError(result.error.message);
      }
      setProvidersLoading(false);
    })();
  }, []);

  // eslint-disable-next-line react-hooks/exhaustive-deps -- intentional mount-only fetch
  useEffect(loadProviders, []);

  // Track previous provider to detect changes
  const [prevProviderId, setPrevProviderId] = useState('');

  // Reset slots when provider changes or is cleared
  if (providerId !== prevProviderId) {
    setPrevProviderId(providerId);
    setSlots([]);
    setSelectedSlotId(null);
    setNoSlotsAvailable(false);
    if (providerId) {
      setSlotsLoading(true);
    }
  }

  // Load same-day slots when provider changes
  useEffect(() => {
    if (!providerId) {
      return;
    }

    let cancelled = false;

    const today = new Date().toISOString().split('T')[0] ?? '';
    void (async () => {
      const result = await fetchSameDaySlots(providerId, today);
      if (cancelled) return;
      if (result.success) {
        const available = result.data.slots;
        setSlots(available);
        setNoSlotsAvailable(available.length === 0);
        setEstimatedWaitTime(result.data.estimatedWaitTime);
      } else {
        setSlots([]);
        setNoSlotsAvailable(true);
        setEstimatedWaitTime(null);
      }
      setSlotsLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [providerId]);

  const handlePatientSelect = useCallback((patient: PatientSearchResult) => {
    setSelectedPatient(patient);
  }, []);

  const handleCreatePatient = useCallback((data: NewPatientData) => {
    setIsCreatingPatient(true);
    void createPatient(data).then((result) => {
      setIsCreatingPatient(false);
      if (result.success) {
        setSelectedPatient(result.data);
        setShowCreateModal(false);
        toast.success(`Patient ${result.data.fullName} created successfully.`);
      } else {
        toast.error(result.error.message);
      }
    });
  }, []);

  const handleAddToWaitQueue = useCallback(() => {
    if (!selectedPatient || !providerId) {
      toast.error('Please select a patient and provider first.');
      return;
    }

    setIsSubmitting(true);
    void bookWalkIn({
      existingPatientId: selectedPatient.id,
      providerId,
      visitType: visitType || 'Walk-In',
      reason,
      slotId: null,
    }).then((result) => {
      setIsSubmitting(false);
      if (result.success) {
        toast.success(
          `Walk-in registered. Queue position: #${result.data.queuePosition}. Estimated wait: ${result.data.estimatedWaitTime ?? 'N/A'}.`,
          { duration: 5000 },
        );
        void navigate(queuePath);
      } else {
        toast.error(result.error.message);
      }
    });
  }, [selectedPatient, providerId, visitType, reason, navigate, queuePath]);

  const handleConfirmWalkIn = useCallback(() => {
    if (!selectedPatient) {
      toast.error('Please select or create a patient.');
      return;
    }
    if (!visitType) {
      toast.error('Please select a visit type.');
      return;
    }
    if (!providerId) {
      toast.error('Please select a provider.');
      return;
    }

    setIsSubmitting(true);
    void bookWalkIn({
      existingPatientId: selectedPatient.id,
      providerId,
      visitType,
      reason,
      slotId: selectedSlotId,
    }).then((result) => {
      setIsSubmitting(false);
      if (result.success) {
        toast.success(
          `Walk-in booked successfully! Queue position: #${result.data.queuePosition}.`,
          { duration: 5000 },
        );
        void navigate(queuePath);
      } else {
        toast.error(result.error.message);
      }
    });
  }, [
    selectedPatient,
    visitType,
    providerId,
    reason,
    selectedSlotId,
    navigate,
    queuePath,
  ]);

  const canSubmit = selectedPatient && visitType && providerId && !isSubmitting;

  return (
    <div className="max-w-175">
      {/* Breadcrumb */}
      <nav
        className="flex items-center gap-2 text-sm mb-6"
        aria-label="Breadcrumb"
      >
        <NavLink to={dashboardPath} className="text-primary hover:underline">
          Dashboard
        </NavLink>
        <span className="text-muted-foreground" aria-hidden="true">
          ›
        </span>
        <span aria-current="page">Walk-In Booking</span>
      </nav>

      <h1 className="text-[32px] font-bold mb-6">Walk-In Booking</h1>

      {/* Find Patient Card */}
      <Card className="mb-6">
        <CardContent className="pt-6">
          <h2 className="text-lg font-semibold mb-5">Find Patient</h2>

          {/* Selected patient badge */}
          {selectedPatient && (
            <div className="flex items-center justify-between p-3 border border-primary bg-accent rounded-md mb-4">
              <div>
                <div className="text-sm font-medium">
                  {selectedPatient.fullName}
                </div>
                <div className="text-xs text-muted-foreground">
                  DOB: {selectedPatient.dateOfBirth} · MRN:{' '}
                  {selectedPatient.mrn}
                </div>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedPatient(null)}
                aria-label="Clear selected patient"
              >
                Change
              </Button>
            </div>
          )}

          <PatientSearchBar
            onSelect={handlePatientSelect}
            onCreateNew={() => setShowCreateModal(true)}
            searchFn={searchPatients}
          />
        </CardContent>
      </Card>

      {/* Appointment Details Card */}
      <Card className="mb-6">
        <CardContent className="pt-6">
          <h2 className="text-lg font-semibold mb-5">Appointment Details</h2>

          {providersError && (
            <ErrorBanner
              message="Failed to load providers. Please try again."
              onRetry={loadProviders}
              onDismiss={() => setProvidersError(null)}
            />
          )}

          {providersLoading ? (
            <div className="space-y-4" aria-busy="true">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : (
            <SameDaySlotPicker
              slots={slots}
              loading={slotsLoading}
              selectedSlotId={selectedSlotId}
              onSelectSlot={setSelectedSlotId}
              visitType={visitType}
              onVisitTypeChange={setVisitType}
              provider={providerId}
              onProviderChange={setProviderId}
              reason={reason}
              onReasonChange={setReason}
              providers={providers.map((p) => ({ id: p.id, name: p.name }))}
              noSlotsAvailable={noSlotsAvailable}
              estimatedWaitTime={estimatedWaitTime}
              onAddToWaitQueue={handleAddToWaitQueue}
            />
          )}

          <div className="flex gap-3 mt-6">
            <Button
              size="lg"
              onClick={handleConfirmWalkIn}
              disabled={!canSubmit}
            >
              Add to Queue
            </Button>
            <Button
              variant="ghost"
              size="lg"
              onClick={() => void navigate(dashboardPath)}
            >
              Cancel
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Create Patient Modal (OVL-009) */}
      <CreatePatientModal
        open={showCreateModal}
        onOpenChange={setShowCreateModal}
        onSubmit={handleCreatePatient}
        isSubmitting={isCreatingPatient}
      />
    </div>
  );
}
