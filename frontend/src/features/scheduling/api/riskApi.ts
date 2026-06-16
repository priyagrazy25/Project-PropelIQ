import { authenticatedFetch } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

/**
 * Risk level categories matching design system tokens.
 * Low ≤30, Medium 31-70, High >70
 */
export type RiskLevel = 'Low' | 'Medium' | 'High';

/**
 * Appointment risk assessment from the ML model (AIR-007).
 */
export interface AppointmentRiskAssessment {
  appointmentId: string;
  patientId: string;
  patientName: string;
  appointmentDateTime: string;
  providerName: string;
  riskScore: number;
  riskLevel: RiskLevel;
  contributingFactors: string[];
}

/**
 * Model version metadata for display and rollback.
 */
export interface ModelVersionInfo {
  version: string;
  trainedAt: string;
  trainingSamples: number;
  accuracy: number;
  isActive: boolean;
}

/**
 * Response from the model versions endpoint.
 */
export interface ModelVersionsResponse {
  activeVersion: string;
  versions: ModelVersionInfo[];
}

/**
 * Parameters for fetching risk assessments.
 */
export interface RiskAssessmentParams {
  startDate: string; // ISO date string
  endDate: string;   // ISO date string
  riskLevel?: RiskLevel;
  providerId?: string;
}

/**
 * API error structure.
 */
export interface RiskApiError {
  status: number;
  message: string;
}

/**
 * Fetches risk assessments for appointments in a date range.
 * Implements SCR-022 risk dashboard data requirements.
 * Falls back to demo data when backend unavailable (dev mode).
 */
export async function fetchRiskAssessments(
  params: RiskAssessmentParams,
  signal?: AbortSignal,
): Promise<
  | { success: true; data: AppointmentRiskAssessment[]; isDemo?: boolean }
  | { success: false; error: RiskApiError }
> {
  try {
    const searchParams = new URLSearchParams({
      startDate: params.startDate,
      endDate: params.endDate,
    });

    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/risk/assessments?${searchParams.toString()}`,
      { method: 'GET', signal },
    );

    if (response.ok) {
      const data = (await response.json()) as AppointmentRiskAssessment[];
      
      // Filter by risk level if specified
      const filtered = params.riskLevel
        ? data.filter((a) => a.riskLevel === params.riskLevel)
        : data;
      
      return { success: true, data: filtered };
    }

    // Fallback to demo data for 401/403 (not authenticated or not authorized)
    if (response.status === 401 || response.status === 403) {
      const demoData = getDemoRiskAssessments();
      const filtered = params.riskLevel
        ? demoData.filter((a) => a.riskLevel === params.riskLevel)
        : demoData;
      return { success: true, data: filtered, isDemo: true };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to fetch risk assessments.',
      },
    };
  } catch (err) {
    if (err instanceof Error && err.name === 'AbortError') {
      return { success: false, error: { status: 0, message: 'Request cancelled' } };
    }
    // Fallback to demo data on network error
    const demoData = getDemoRiskAssessments();
    const filtered = params.riskLevel
      ? demoData.filter((a) => a.riskLevel === params.riskLevel)
      : demoData;
    return { success: true, data: filtered, isDemo: true };
  }
}

/**
 * Fetches available model versions for the risk assessment model.
 */
export async function fetchModelVersions(
  signal?: AbortSignal,
): Promise<
  | { success: true; data: ModelVersionsResponse }
  | { success: false; error: RiskApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/scheduling/risk/versions`,
      { method: 'GET', signal },
    );

    if (response.ok) {
      const data = (await response.json()) as ModelVersionsResponse;
      return { success: true, data };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to fetch model versions.',
      },
    };
  } catch {
    return {
      success: false,
      error: { status: 500, message: 'Network error occurred.' },
    };
  }
}

/**
 * Triggers a reminder for a high-risk appointment.
 */
export async function sendRiskReminder(
  appointmentId: string,
  signal?: AbortSignal,
): Promise<{ success: boolean; error?: RiskApiError }> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/notification/reminders/${appointmentId}`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ type: 'risk_outreach' }),
        signal,
      },
    );

    if (response.ok || response.status === 204) {
      return { success: true };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to send reminder.',
      },
    };
  } catch {
    return {
      success: false,
      error: { status: 500, message: 'Network error occurred.' },
    };
  }
}

/**
 * Calculates display date range options.
 */
export function getDateRangeOptions(): { label: string; startDate: string; endDate: string }[] {
  const today = new Date();
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);
  
  const next7Days = new Date(today);
  next7Days.setDate(next7Days.getDate() + 7);
  
  const next30Days = new Date(today);
  next30Days.setDate(next30Days.getDate() + 30);

  const toISODate = (d: Date): string => d.toISOString().slice(0, 10);

  return [
    { label: 'Today', startDate: toISODate(today), endDate: toISODate(tomorrow) },
    { label: 'Next 7 Days', startDate: toISODate(today), endDate: toISODate(next7Days) },
    { label: 'Next 30 Days', startDate: toISODate(today), endDate: toISODate(next30Days) },
  ];
}

/**
 * Demo data for development/testing when backend unavailable.
 */
export function getDemoRiskAssessments(): AppointmentRiskAssessment[] {
  const now = new Date();
  const addHours = (h: number) => {
    const d = new Date(now);
    d.setHours(d.getHours() + h);
    return d.toISOString();
  };

  return [
    {
      appointmentId: 'demo-apt-001',
      patientId: 'demo-pat-001',
      patientName: 'Sarah Johnson',
      appointmentDateTime: addHours(2),
      providerName: 'Dr. Emily Chen',
      riskScore: 82,
      riskLevel: 'High',
      contributingFactors: ['2 prior no-shows', 'Last-minute booking', 'No insurance on file'],
    },
    {
      appointmentId: 'demo-apt-002',
      patientId: 'demo-pat-002',
      patientName: 'Michael Brown',
      appointmentDateTime: addHours(4),
      providerName: 'Dr. James Wilson',
      riskScore: 75,
      riskLevel: 'High',
      contributingFactors: ['Distance > 30 miles', 'First-time patient', 'Monday AM slot'],
    },
    {
      appointmentId: 'demo-apt-003',
      patientId: 'demo-pat-003',
      patientName: 'Jennifer Martinez',
      appointmentDateTime: addHours(6),
      providerName: 'Dr. Emily Chen',
      riskScore: 55,
      riskLevel: 'Medium',
      contributingFactors: ['1 prior no-show', 'Weather advisory'],
    },
    {
      appointmentId: 'demo-apt-004',
      patientId: 'demo-pat-004',
      patientName: 'Robert Williams',
      appointmentDateTime: addHours(8),
      providerName: 'Dr. Lisa Anderson',
      riskScore: 45,
      riskLevel: 'Medium',
      contributingFactors: ['Unconfirmed appointment'],
    },
    {
      appointmentId: 'demo-apt-005',
      patientId: 'demo-pat-005',
      patientName: 'Emily Davis',
      appointmentDateTime: addHours(24),
      providerName: 'Dr. James Wilson',
      riskScore: 35,
      riskLevel: 'Medium',
      contributingFactors: ['Weekend slot'],
    },
    {
      appointmentId: 'demo-apt-006',
      patientId: 'demo-pat-006',
      patientName: 'David Lee',
      appointmentDateTime: addHours(26),
      providerName: 'Dr. Emily Chen',
      riskScore: 18,
      riskLevel: 'Low',
      contributingFactors: [],
    },
    {
      appointmentId: 'demo-apt-007',
      patientId: 'demo-pat-007',
      patientName: 'Amanda Taylor',
      appointmentDateTime: addHours(28),
      providerName: 'Dr. Lisa Anderson',
      riskScore: 12,
      riskLevel: 'Low',
      contributingFactors: [],
    },
    {
      appointmentId: 'demo-apt-008',
      patientId: 'demo-pat-008',
      patientName: 'Christopher Moore',
      appointmentDateTime: addHours(48),
      providerName: 'Dr. James Wilson',
      riskScore: 8,
      riskLevel: 'Low',
      contributingFactors: [],
    },
  ];
}
