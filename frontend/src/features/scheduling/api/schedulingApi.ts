import { authenticatedFetch } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

export interface ProviderSlot {
  id: string;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}

export interface ProviderResult {
  id: string;
  fullName: string;
  specialty: string;
  location: string;
  rating: number;
  isAcceptingPatients: boolean;
  availableSlots: ProviderSlot[];
  nextAvailableDate: string | null;
}

export interface ProviderSearchResponse {
  providers: ProviderResult[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ProviderSearchParams {
  name?: string;
  specialty?: string;
  location?: string;
  date?: string;
  page: number;
  pageSize: number;
  sortBy?: string;
}

export interface SchedulingApiError {
  status: number;
  message: string;
}

export interface BookingRequest {
  providerId: string;
  slotId: string;
  idempotencyKey: string;
  preferredSlotId?: string;
}

export interface BookingResponse {
  appointmentId: string;
  providerId: string;
  providerName: string;
  specialty: string;
  location: string;
  slotStartTime: string;
  slotEndTime: string;
  status: string;
}

export async function bookAppointment(
  request: BookingRequest,
  signal?: AbortSignal,
): Promise<
  | { success: true; data: BookingResponse }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/appointments`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
        signal,
      },
    );

    if (response.ok) {
      const body = (await response.json()) as BookingResponse;
      return { success: true, data: body };
    }

    if (response.status === 409) {
      return {
        success: false,
        error: {
          status: 409,
          message: 'This slot is no longer available. Please select another.',
        },
      };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to book appointment.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── My Appointments ── */

export interface MyAppointment {
  appointmentId: string;
  providerId: string;
  providerName: string;
  specialty: string;
  location: string;
  appointmentDateTime: string;
  durationMinutes: number;
  status: string;
  type: string;
}

export async function fetchMyAppointments(): Promise<
  | { success: true; data: MyAppointment[] }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/appointments/my`,
    );

    if (response.ok) {
      const body = (await response.json()) as MyAppointment[];
      return { success: true, data: body };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to load appointments.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function searchProviders(
  params: ProviderSearchParams,
): Promise<
  | { success: true; data: ProviderSearchResponse }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const query = new URLSearchParams();
    query.set('page', params.page.toString());
    query.set('pageSize', params.pageSize.toString());
    if (params.name) query.set('name', params.name);
    if (params.specialty) query.set('specialty', params.specialty);
    if (params.location) query.set('location', params.location);
    if (params.date) query.set('date', params.date);
    if (params.sortBy) query.set('sortBy', params.sortBy);

    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/providers/search?${query.toString()}`,
    );

    if (response.ok) {
      const body = (await response.json()) as ProviderSearchResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to search providers.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function fetchProviderSlots(
  providerId: string,
  date: string,
): Promise<
  | { success: true; data: ProviderSlot[] }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/providers/${encodeURIComponent(providerId)}/slots?date=${encodeURIComponent(date)}`,
    );

    if (response.ok) {
      const body = (await response.json()) as {
        providerId: string;
        slots: ProviderSlot[];
      };
      return { success: true, data: body.slots };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to load slots.' },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── Preferred Slot Swap ── */

export type SwapStatus = 'Pending' | 'Executed' | 'Expired' | 'Cancelled';

export interface SwapPreference {
  id: string;
  appointmentId: string;
  preferredSlotId: string;
  preferredSlotStartTime: string;
  preferredSlotEndTime: string;
  providerName: string;
  status: SwapStatus;
  createdAt: string;
}

export interface SwapExecutedEvent {
  swapId: string;
  appointmentId: string;
  newSlotStartTime: string;
  newSlotEndTime: string;
  providerName: string;
}

export async function fetchSwapPreferences(): Promise<
  | { success: true; data: SwapPreference[] }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(`${API_BASE}/scheduling/swaps`);

    if (response.ok) {
      const body = (await response.json()) as SwapPreference[];
      return { success: true, data: body };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to load swap preferences.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function cancelSwapPreference(
  swapId: string,
): Promise<{ success: true } | { success: false; error: SchedulingApiError }> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/swaps/${encodeURIComponent(swapId)}`,
      { method: 'DELETE' },
    );

    if (response.ok) {
      return { success: true };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to cancel swap preference.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── Waitlist Management ── */

export type WaitlistStatus =
  | 'Active'
  | 'Notified'
  | 'Booked'
  | 'Expired'
  | 'Cancelled';

export interface WaitlistEntry {
  id: string;
  providerId: string;
  providerName: string;
  specialty: string;
  preferredDateStart: string;
  preferredDateEnd: string;
  status: WaitlistStatus;
  position: number;
  createdAt: string;
  notifiedAt: string | null;
}

export interface JoinWaitlistRequest {
  providerId: string;
  preferredDateStart: string;
  preferredDateEnd: string;
}

export interface WaitlistAvailableEvent {
  waitlistId: string;
  providerId: string;
  providerName: string;
  slotId: string;
  slotStartTime: string;
  slotEndTime: string;
}

export async function joinWaitlist(
  request: JoinWaitlistRequest,
): Promise<
  | { success: true; data: WaitlistEntry }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/waitlist`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      },
    );

    if (response.ok) {
      const body = (await response.json()) as WaitlistEntry;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to join waitlist.' },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function fetchWaitlistEntries(): Promise<
  | { success: true; data: WaitlistEntry[] }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/waitlist/my`,
    );

    if (response.ok) {
      const body = (await response.json()) as WaitlistEntry[];
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to load waitlist.' },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function removeFromWaitlist(
  waitlistId: string,
): Promise<{ success: true } | { success: false; error: SchedulingApiError }> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/waitlist/${encodeURIComponent(waitlistId)}`,
      { method: 'DELETE' },
    );

    if (response.ok) {
      return { success: true };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to remove from waitlist.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── Cancel & Reschedule ── */

export interface CancelAppointmentRequest {
  cancellationReason?: string;
}

export async function cancelAppointment(
  appointmentId: string,
  request?: CancelAppointmentRequest,
): Promise<{ success: true } | { success: false; error: SchedulingApiError }> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/appointments/${encodeURIComponent(appointmentId)}`,
      {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request ?? {}),
      },
    );

    if (response.ok || response.status === 204) {
      return { success: true };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to cancel appointment.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function rescheduleAppointment(
  oldAppointmentId: string,
  newSlotId: string,
  idempotencyKey: string,
  cancellationReason?: string,
): Promise<
  | { success: true; data: BookingResponse }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/appointments/${encodeURIComponent(oldAppointmentId)}/reschedule`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          newSlotId,
          idempotencyKey,
          cancellationReason: cancellationReason ?? 'Rescheduled to new time',
        }),
      },
    );

    if (response.ok) {
      const body = (await response.json()) as BookingResponse;
      return { success: true, data: body };
    }

    if (response.status === 409) {
      return {
        success: false,
        error: {
          status: 409,
          message:
            'The selected slot is no longer available. Please select another.',
        },
      };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to reschedule appointment.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── Walk-In Booking (Staff Only) ── */

export interface AvailableProvider {
  id: string;
  name: string;
  specialty: string;
}

export interface SameDaySlotsResponse {
  slots: {
    id: string;
    startTime: string;
    endTime: string;
    providerName: string;
    isAvailable: boolean;
  }[];
  estimatedWaitTime: string | null;
}

export interface WalkInBookingRequest {
  existingPatientId: string;
  providerId: string;
  visitType?: string;
  reason?: string;
  slotId?: string | null;
}

export interface WalkInBookingResponse {
  appointmentId: string;
  queuePosition: number;
  estimatedWaitTime: string | null;
  status: string;
}

export async function searchPatients(
  query: string,
  signal?: AbortSignal,
): Promise<
  | {
      success: true;
      data: {
        id: string;
        fullName: string;
        dateOfBirth: string;
        contactNumber: string;
        email: string;
        mrn: string;
      }[];
    }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/walk-in/patients/search?q=${encodeURIComponent(query)}`,
      { signal },
    );

    if (response.ok) {
      const rawBody = (await response.json()) as
        | {
            value: {
              patientId: string;
              fullName: string;
              dateOfBirth: string;
              contactNumber: string;
              email: string;
            }[];
          }
        | {
            patientId: string;
            fullName: string;
            dateOfBirth: string;
            contactNumber: string;
            email: string;
          }[];
      // Handle both wrapped { value: [...] } and plain array responses
      const items = Array.isArray(rawBody) ? rawBody : rawBody.value;
      const data = items.map((p) => ({
        id: p.patientId,
        fullName: p.fullName,
        dateOfBirth: p.dateOfBirth,
        contactNumber: p.contactNumber,
        email: p.email,
        mrn: (p.mrn as string) ?? '',
      }));
      return { success: true, data };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Patient search failed.' },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function createPatient(data: {
  firstName: string;
  lastName: string;
  contactNumber: string;
  dateOfBirth: string;
}): Promise<
  | {
      success: true;
      data: {
        id: string;
        fullName: string;
        dateOfBirth: string;
        contactNumber: string;
        email: string;
        mrn: string;
      };
    }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/patients`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      },
    );

    if (response.ok) {
      const body = (await response.json()) as {
        id: string;
        fullName: string;
        dateOfBirth: string;
        contactNumber: string;
        email: string;
        mrn: string;
      };
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to create patient.' },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function fetchAvailableProviders(): Promise<
  | { success: true; data: AvailableProvider[] }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/providers/available`,
    );

    if (response.ok) {
      const body = (await response.json()) as AvailableProvider[];
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to load providers.' },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function fetchSameDaySlots(
  providerId: string,
  date: string,
): Promise<
  | { success: true; data: SameDaySlotsResponse }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/providers/${encodeURIComponent(providerId)}/same-day-slots?date=${encodeURIComponent(date)}`,
    );

    if (response.ok) {
      const body = (await response.json()) as SameDaySlotsResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to load same-day slots.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function bookWalkIn(
  request: WalkInBookingRequest,
): Promise<
  | { success: true; data: WalkInBookingResponse }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/walk-in`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      },
    );

    if (response.ok) {
      const body = (await response.json()) as WalkInBookingResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to register walk-in.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── Queue Management (Staff Only) ── */

export type QueueStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'Arrived'
  | 'Waiting'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'Left'
  | 'NoShow'
  | 'Rescheduled';

export interface QueueEntry {
  id: string;
  position: number;
  patientId: string;
  patientName: string;
  appointmentType: string;
  providerName: string;
  status: QueueStatus;
  arrivalTime: string | null;
  rowVersion: string;
}

export interface QueueSummary {
  entries: QueueEntry[];
  waitingCount: number;
  inProgressCount: number;
  completedCount: number;
  averageWaitMinutes: number;
}

export async function fetchQueueEntries(): Promise<
  | { success: true; data: QueueSummary }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(`${API_BASE}/scheduling/queue`);

    if (response.ok) {
      const body = (await response.json()) as QueueSummary;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to load queue.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function updateQueueEntryStatus(
  entryId: string,
  newStatus: QueueStatus,
  rowVersion: string,
): Promise<
  | { success: true; data: QueueEntry }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/queue/${encodeURIComponent(entryId)}/status`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ NewStatus: newStatus, RowVersion: rowVersion }),
      },
    );

    if (response.ok) {
      const body = (await response.json()) as QueueEntry;
      return { success: true, data: body };
    }

    if (response.status === 409) {
      return {
        success: false,
        error: {
          status: 409,
          message: 'This entry was updated by another user. Refreshing queue.',
        },
      };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to update status.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

export async function markArrival(
  entryId: string,
  rowVersion: string,
): Promise<
  | { success: true; data: QueueEntry }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/appointments/${encodeURIComponent(entryId)}/arrive`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ rowVersion }),
      },
    );

    if (response.ok) {
      const body = (await response.json()) as QueueEntry;
      return { success: true, data: body };
    }

    if (response.status === 403) {
      return {
        success: false,
        error: {
          status: 403,
          message: 'Only staff members can mark patient arrival.',
        },
      };
    }

    if (response.status === 409) {
      return {
        success: false,
        error: {
          status: 409,
          message: 'This entry was updated by another user. Refreshing queue.',
        },
      };
    }

    if (response.status === 422) {
      const body = (await response.json()) as { message?: string };
      return {
        success: false,
        error: {
          status: 422,
          message: body.message ?? 'Cannot mark arrival for this appointment.',
        },
      };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to mark arrival.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── Patient Queue Status ── */

export interface PatientQueueStatus {
  id: string;
  patientId: string;
  patientName: string;
  appointmentType: string;
  providerName: string;
  status: 'Waiting' | 'InProgress';
  arrivalTime: string | null;
  waitDurationMinutes: number;
  position: number;
}

export async function fetchMyQueueStatus(): Promise<
  | { success: true; data: PatientQueueStatus }
  | { success: false; notInQueue: true }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(`${API_BASE}/scheduling/queue/my`);

    if (response.ok) {
      const body = (await response.json()) as PatientQueueStatus;
      return { success: true, data: body };
    }

    if (response.status === 404) {
      return { success: false, notInQueue: true };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to load queue status.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}

/* ── Calendar Sync ── */

export type CalendarProvider = 'Google' | 'Outlook';

export type CalendarSyncState = 'idle' | 'synced' | 'pending' | 'failed';

export interface CalendarSyncRequest {
  appointmentId: string;
  provider: CalendarProvider;
  oauthToken: string;
}

export interface CalendarSyncResponse {
  syncId: string;
  status: CalendarSyncState;
  provider: CalendarProvider;
  message: string;
}

export async function syncCalendar(
  request: CalendarSyncRequest,
): Promise<
  | { success: true; data: CalendarSyncResponse }
  | { success: false; error: SchedulingApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/calendar/sync`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      },
    );

    if (response.ok) {
      const body = (await response.json()) as CalendarSyncResponse;
      return { success: true, data: body };
    }

    if (response.status === 503) {
      return {
        success: false,
        error: {
          status: 503,
          message:
            'Calendar sync is temporarily unavailable. Your event will be synced once service resumes.',
        },
      };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to sync calendar event.',
      },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network.',
      },
    };
  }
}
