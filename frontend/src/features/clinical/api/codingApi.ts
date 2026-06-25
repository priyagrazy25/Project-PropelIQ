import { getAccessToken } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

/** Code types for medical coding (US_038, US_039). */
export type CodeType = 'ICD-10' | 'CPT';

/** Verification status for a code candidate. */
export type VerificationStatus = 'Pending' | 'Accepted' | 'Rejected';

/** A single code candidate with confidence score (AC-2). */
export interface CodeCandidate {
  code: string;
  description: string;
  confidence: number;
}

/** A code entry requiring staff verification. */
export interface CodeEntry {
  id: string;
  patientId: string;
  patientName: string;
  codeType: CodeType;
  primaryCode: CodeCandidate;
  alternativeCandidates: CodeCandidate[];
  status: VerificationStatus;
  sourceDiagnosis?: string;
  sourceProcedure?: string;
  extractedAt: string;
  verifiedBy?: string;
  verifiedAt?: string;
  rejectionReason?: string;
}

/** Response for fetching code verification queue. */
export interface CodeVerificationQueueResponse {
  entries: CodeEntry[];
  totalCount: number;
  pendingCount: number;
}

/** Request to verify (accept/reject) a code. */
export interface VerifyCodeRequest {
  action: 'accept' | 'reject';
  reason?: string;
  notes?: string;
}

/**
 * Fetches the code verification queue for staff (SCR-018).
 * Returns ICD-10 and CPT codes with top-3 candidates ranked by confidence.
 */
export async function fetchCodeVerificationQueue(
  filters?: { status?: VerificationStatus; codeType?: CodeType }
): Promise<
  | { success: true; data: CodeVerificationQueueResponse }
  | { success: false; error: string }
> {
  try {
    const token = getAccessToken();
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.codeType) params.append('codeType', filters.codeType);

    const url = `${API_BASE}/clinical/codes/verification-queue${params.toString() ? `?${params.toString()}` : ''}`;

    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) {
      return { success: false, error: `Failed to fetch codes: ${response.status}` };
    }

    const data = (await response.json()) as CodeVerificationQueueResponse;
    return { success: true, data };
  } catch {
    return { success: false, error: 'Network error. Unable to fetch code queue.' };
  }
}

/**
 * Verifies (accepts or rejects) a code entry.
 */
export async function verifyCode(
  entryId: string,
  request: VerifyCodeRequest
): Promise<{ success: true } | { success: false; error: string }> {
  try {
    const token = getAccessToken();
    const response = await fetch(`${API_BASE}/clinical/codes/${entryId}/verify`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 409) {
        return { success: false, error: 'Code already verified by another user.' };
      }
      return { success: false, error: `Verification failed: ${response.status}` };
    }

    return { success: true };
  } catch {
    return { success: false, error: 'Network error. Unable to verify code.' };
  }
}

/** Demo data for when backend is unavailable. */
export function getDemoCodeVerificationQueue(): CodeVerificationQueueResponse {
  return {
    entries: [
      {
        id: 'code-1',
        patientId: 'patient-1',
        patientName: 'Jane Doe',
        codeType: 'ICD-10',
        primaryCode: {
          code: 'E11.65',
          description: 'Type 2 diabetes mellitus with hyperglycemia',
          confidence: 0.96,
        },
        alternativeCandidates: [
          {
            code: 'E11.9',
            description: 'Type 2 diabetes mellitus without complications',
            confidence: 0.82,
          },
          {
            code: 'E11.8',
            description: 'Type 2 diabetes mellitus with unspecified complications',
            confidence: 0.71,
          },
        ],
        status: 'Pending',
        sourceDiagnosis: 'Type 2 Diabetes with elevated blood sugar',
        extractedAt: '2026-04-24T10:30:00Z',
      },
      {
        id: 'code-2',
        patientId: 'patient-1',
        patientName: 'Jane Doe',
        codeType: 'ICD-10',
        primaryCode: {
          code: 'I10',
          description: 'Essential (primary) hypertension',
          confidence: 0.92,
        },
        alternativeCandidates: [
          {
            code: 'I11.9',
            description: 'Hypertensive heart disease without heart failure',
            confidence: 0.68,
          },
          {
            code: 'I15.9',
            description: 'Secondary hypertension, unspecified',
            confidence: 0.45,
          },
        ],
        status: 'Accepted',
        sourceDiagnosis: 'Essential Hypertension',
        extractedAt: '2026-04-24T10:30:00Z',
        verifiedBy: 'Dr. Martinez',
        verifiedAt: '2026-04-24T11:15:00Z',
      },
      {
        id: 'code-3',
        patientId: 'patient-2',
        patientName: 'John Smith',
        codeType: 'CPT',
        primaryCode: {
          code: '99213',
          description: 'Office or other outpatient visit for the evaluation and management of an established patient (moderate complexity)',
          confidence: 0.74,
        },
        alternativeCandidates: [
          {
            code: '99214',
            description: 'Office or other outpatient visit (moderate-high complexity)',
            confidence: 0.65,
          },
          {
            code: '99212',
            description: 'Office or other outpatient visit (straightforward)',
            confidence: 0.52,
          },
        ],
        status: 'Pending',
        sourceProcedure: 'Office visit - established patient',
        extractedAt: '2026-04-24T09:45:00Z',
      },
      {
        id: 'code-4',
        patientId: 'patient-2',
        patientName: 'John Smith',
        codeType: 'ICD-10',
        primaryCode: {
          code: 'J45.20',
          description: 'Mild intermittent asthma, uncomplicated',
          confidence: 0.58,
        },
        alternativeCandidates: [
          {
            code: 'J45.30',
            description: 'Mild persistent asthma, uncomplicated',
            confidence: 0.51,
          },
          {
            code: 'J45.909',
            description: 'Unspecified asthma, uncomplicated',
            confidence: 0.42,
          },
        ],
        status: 'Pending',
        sourceDiagnosis: 'Mild asthma - intermittent symptoms',
        extractedAt: '2026-04-24T09:45:00Z',
      },
      {
        id: 'code-5',
        patientId: 'patient-3',
        patientName: 'Sarah Johnson',
        codeType: 'CPT',
        primaryCode: {
          code: '36415',
          description: 'Collection of venous blood by venipuncture',
          confidence: 0.98,
        },
        alternativeCandidates: [
          {
            code: '36416',
            description: 'Collection of capillary blood specimen (finger/heel/ear stick)',
            confidence: 0.62,
          },
        ],
        status: 'Pending',
        sourceProcedure: 'Blood draw - venous sample',
        extractedAt: '2026-04-24T08:20:00Z',
      },
    ],
    totalCount: 5,
    pendingCount: 4,
  };
}

/** Request to override a code with manual entry. */
export interface OverrideCodeRequest {
  code: string;
  description: string;
  overrideReason: string;
  notes?: string;
}

/**
 * Overrides a code entry with a manual code (AIR-S04).
 */
export async function overrideCode(
  entryId: string,
  request: OverrideCodeRequest
): Promise<{ success: true } | { success: false; error: string }> {
  try {
    const token = getAccessToken();
    const response = await fetch(`${API_BASE}/clinical/codes/${entryId}/override`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 409) {
        return { success: false, error: 'Code already verified by another user.' };
      }
      return { success: false, error: `Override failed: ${response.status}` };
    }

    return { success: true };
  } catch {
    return { success: false, error: 'Network error. Unable to override code.' };
  }
}

/** Agreement rate metrics response. */
export interface AgreementRateResponse {
  /** 30-day rolling agreement rate (0-100). */
  rate: number;
  /** Codes accepted in period. */
  acceptedCount: number;
  /** Codes rejected in period. */
  rejectedCount: number;
  /** Codes overridden in period. */
  overriddenCount: number;
  /** Codes pending. */
  pendingCount: number;
  /** Total codes processed in period. */
  totalProcessed: number;
  /** Period start date. */
  periodStart: string;
  /** Period end date. */
  periodEnd: string;
  /** Whether target is met (>98%). */
  targetMet: boolean;
  /** Trend direction. */
  trend: number;
  /** Daily rates for chart. */
  dailyRates: { date: string; rate: number }[];
}

/**
 * Fetches 30-day rolling agreement rate metrics.
 */
export async function fetchAgreementRate(): Promise<
  | { success: true; data: AgreementRateResponse }
  | { success: false; error: string }
> {
  try {
    const token = getAccessToken();
    const response = await fetch(`${API_BASE}/clinical/codes/agreement-rate`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) {
      return { success: false, error: `Failed to fetch metrics: ${response.status}` };
    }

    const data = (await response.json()) as AgreementRateResponse;
    return { success: true, data };
  } catch {
    return { success: false, error: 'Network error. Unable to fetch metrics.' };
  }
}

/**
 * Returns the count of open data conflicts (used by staff dashboard SCR-021).
 */
export async function fetchOpenConflictsCount(): Promise<
  | { success: true; data: number }
  | { success: false; error: string }
> {
  try {
    const token = getAccessToken();
    const response = await fetch(`${API_BASE}/clinical/conflicts/open-count`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) {
      return { success: false, error: `Failed to fetch conflict count: ${response.status}` };
    }

    const count = (await response.json()) as number;
    return { success: true, data: count };
  } catch {
    return { success: false, error: 'Network error. Unable to fetch conflict count.' };
  }
}

/** Demo agreement rate data. */
export function getDemoAgreementRate(): AgreementRateResponse {
  const now = new Date();
  const thirtyDaysAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);

  return {
    rate: 87.5,
    acceptedCount: 42,
    rejectedCount: 4,
    overriddenCount: 2,
    pendingCount: 5,
    totalProcessed: 48,
    periodStart: thirtyDaysAgo.toISOString(),
    periodEnd: now.toISOString(),
    targetMet: false,
    trend: 0.5,
    dailyRates: [],
  };
}
