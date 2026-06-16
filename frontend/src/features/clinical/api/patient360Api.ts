import { getAccessToken } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

/** Clinical data category types matching DR-006. */
export type DataCategory =
  | 'vital'
  | 'history'
  | 'medication'
  | 'allergy'
  | 'lab'
  | 'diagnosis';

/** Extracted data point with confidence score (UXR-107). */
export interface ExtractedDataPoint {
  id: string;
  category: DataCategory;
  field: string;
  value: string;
  sourceDocument: string;
  confidence: number;
  extractedAt: string;
  conflictId?: string;
}

/** Patient demographics for the 360 view banner. */
export interface PatientDemographics {
  patientId: string;
  fullName: string;
  dateOfBirth: string;
  gender: string;
  mrn: string;
  phone?: string;
  email?: string;
  address?: string;
  insuranceProvider?: string;
  memberId?: string;
}

/** Conflict summary for alert display. */
export interface ConflictSummary {
  conflictId: string;
  field: string;
  category: DataCategory;
  values: string[];
  severity: 'high' | 'medium' | 'low';
}

/** Full 360-degree patient view response. */
export interface Patient360ViewResponse {
  demographics: PatientDemographics;
  extractedData: ExtractedDataPoint[];
  conflicts: ConflictSummary[];
  documentCount: number;
  lastUpdated: string;
}

/**
 * Fetches the 360-degree patient view data (SCR-016).
 * Returns aggregated clinical data with confidence scores.
 */
export async function fetchPatient360View(
  patientId?: string,
): Promise<
  | { success: true; data: Patient360ViewResponse }
  | { success: false; error: string }
> {
  try {
    const token = getAccessToken();
    const url = patientId
      ? `${API_BASE}/clinical/patients/${patientId}/360-view`
      : `${API_BASE}/clinical/patients/me/360-view`;

    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) {
      if (response.status === 404) {
        return { success: false, error: 'Patient not found.' };
      }
      if (response.status === 403) {
        return { success: false, error: 'You are not authorized to view this patient\'s data.' };
      }
      if (response.status === 401) {
        return { success: false, error: 'Authentication required. Please log in again.' };
      }
      return { success: false, error: `Failed to fetch data: ${response.status}` };
    }

    const data = (await response.json()) as Patient360ViewResponse;
    return { success: true, data };
  } catch {
    return { success: false, error: 'Network error. Unable to fetch patient data.' };
  }
}

/** Demo data for when backend is unavailable. */
export function getDemoPatient360View(): Patient360ViewResponse {
  return {
    demographics: {
      patientId: 'demo-patient',
      fullName: 'Jane Marie Doe',
      dateOfBirth: 'March 15, 1985',
      gender: 'Female',
      mrn: 'PAT-2024-001234',
      phone: '(555) 123-4567',
      email: 'jane.doe@email.com',
      address: '123 Main St, Suite 4, Springfield, IL 62701',
      insuranceProvider: 'Blue Cross Blue Shield',
      memberId: 'BCBS-12345678',
    },
    extractedData: [
      // Vitals
      { id: '1', category: 'vital', field: 'Blood Pressure', value: '120/80 mmHg', sourceDocument: 'Lab Report', confidence: 0.95, extractedAt: '2024-04-20' },
      { id: '2', category: 'vital', field: 'Heart Rate', value: '72 bpm', sourceDocument: 'Lab Report', confidence: 0.97, extractedAt: '2024-04-20' },
      { id: '3', category: 'vital', field: 'Blood Type', value: 'O+', sourceDocument: 'Lab Report OCR', confidence: 0.78, extractedAt: '2024-04-20' },
      // History
      { id: '4', category: 'history', field: 'Previous Surgery', value: 'Appendectomy (2018)', sourceDocument: 'AI Intake', confidence: 0.92, extractedAt: '2024-04-18' },
      { id: '5', category: 'history', field: 'Family History', value: 'Diabetes (Mother), Hypertension (Father)', sourceDocument: 'AI Intake', confidence: 0.88, extractedAt: '2024-04-18' },
      // Medications
      { id: '6', category: 'medication', field: 'Lisinopril', value: '10mg daily', sourceDocument: 'Medication List OCR', confidence: 0.94, extractedAt: '2024-04-19' },
      { id: '7', category: 'medication', field: 'Metformin', value: '500mg twice daily', sourceDocument: 'Medication List OCR', confidence: 0.91, extractedAt: '2024-04-19' },
      { id: '8', category: 'medication', field: 'Aspirin', value: '81mg daily', sourceDocument: 'AI Intake', confidence: 0.89, extractedAt: '2024-04-18' },
      // Allergies
      { id: '9', category: 'allergy', field: 'Penicillin', value: 'Severe - Anaphylaxis', sourceDocument: 'AI Intake', confidence: 0.96, extractedAt: '2024-04-18' },
      { id: '10', category: 'allergy', field: 'Shellfish', value: 'Moderate - Hives', sourceDocument: 'AI Intake', confidence: 0.94, extractedAt: '2024-04-18' },
      // Labs
      { id: '11', category: 'lab', field: 'Hemoglobin A1c', value: '6.2%', sourceDocument: 'Lab Report OCR', confidence: 0.93, extractedAt: '2024-04-20' },
      { id: '12', category: 'lab', field: 'Total Cholesterol', value: '195 mg/dL', sourceDocument: 'Lab Report OCR', confidence: 0.91, extractedAt: '2024-04-20' },
      { id: '13', category: 'lab', field: 'LDL Cholesterol', value: '120 mg/dL', sourceDocument: 'Lab Report OCR', confidence: 0.90, extractedAt: '2024-04-20' },
      // Diagnoses
      { id: '14', category: 'diagnosis', field: 'Type 2 Diabetes', value: 'E11.9', sourceDocument: 'Clinical Summary', confidence: 0.95, extractedAt: '2024-04-19' },
      { id: '15', category: 'diagnosis', field: 'Essential Hypertension', value: 'I10', sourceDocument: 'Clinical Summary', confidence: 0.93, extractedAt: '2024-04-19' },
    ],
    conflicts: [
      {
        conflictId: 'conflict-1',
        field: 'Aspirin Dosage',
        category: 'medication',
        values: ['81mg daily', '100mg daily'],
        severity: 'medium',
      },
    ],
    documentCount: 5,
    lastUpdated: new Date().toISOString(),
  };
}

/** Detailed conflict information for resolution page. */
export interface ConflictDetail {
  conflictId: string;
  patientId: string;
  field: string;
  category: DataCategory;
  sourceA: {
    documentId: string;
    documentName: string;
    value: string;
    confidence: number;
    extractedAt: string;
  };
  sourceB: {
    documentId: string;
    documentName: string;
    value: string;
    confidence: number;
    extractedAt: string;
  };
  severity: 'high' | 'medium' | 'low';
  resolutionStatus: 'Open' | 'Resolved' | 'Dismissed';
  resolvedValue?: string;
  resolvedAt?: string;
  resolvedBy?: string;
  resolutionNotes?: string;
}

/** Request payload for resolving a conflict. */
export interface ResolveConflictRequest {
  conflictId: string;
  resolvedValue: string;
  resolutionSource: 'A' | 'B' | 'manual';
  notes?: string;
}

/** Response after resolving a conflict. */
export interface ResolveConflictResponse {
  success: boolean;
  message: string;
  conflictId: string;
  resolvedValue: string;
  resolvedAt: string;
}

/**
 * Fetches detailed conflict information for resolution (SCR-017).
 * Returns source documents and values for side-by-side comparison.
 */
export async function fetchConflictDetail(
  conflictId: string,
): Promise<
  | { success: true; data: ConflictDetail }
  | { success: false; error: string }
> {
  try {
    const token = getAccessToken();
    const response = await fetch(`${API_BASE}/clinical/conflicts/${conflictId}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) {
      if (response.status === 404) {
        return { success: false, error: 'Conflict not found or already resolved.' };
      }
      if (response.status === 409) {
        return { success: false, error: 'Conflict was already resolved by another user.' };
      }
      return { success: false, error: `Failed to fetch conflict: ${response.status}` };
    }

    const data = (await response.json()) as ConflictDetail;
    return { success: true, data };
  } catch {
    return { success: false, error: 'Network error. Unable to fetch conflict details.' };
  }
}

/**
 * Resolves a data conflict with the selected value (AC-2).
 * Supports accepting source A, B, or manual override.
 * Returns updated conflict status.
 */
export async function resolveConflict(
  request: ResolveConflictRequest,
): Promise<
  | { success: true; data: ResolveConflictResponse }
  | { success: false; error: string; isOptimisticConflict?: boolean }
> {
  try {
    const token = getAccessToken();
    const response = await fetch(`${API_BASE}/clinical/conflicts/${request.conflictId}/resolve`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 404) {
        return { success: false, error: 'Conflict not found.' };
      }
      if (response.status === 409) {
        // Optimistic concurrency conflict - conflict was already resolved
        return {
          success: false,
          error: 'This conflict was already resolved by another user. Please refresh.',
          isOptimisticConflict: true,
        };
      }
      return { success: false, error: `Failed to resolve conflict: ${response.status}` };
    }

    const data = (await response.json()) as ResolveConflictResponse;
    return { success: true, data };
  } catch {
    return { success: false, error: 'Network error. Unable to resolve conflict.' };
  }
}

/** Demo conflict detail for when backend is unavailable. */
export function getDemoConflictDetail(conflictId: string): ConflictDetail {
  return {
    conflictId,
    patientId: 'demo-patient',
    field: 'Address',
    category: 'history',
    sourceA: {
      documentId: 'doc-1',
      documentName: 'AI Intake',
      value: '123 Main St, Springfield, IL 62701',
      confidence: 0.94,
      extractedAt: '2025-12-15T10:30:00Z',
    },
    sourceB: {
      documentId: 'doc-2',
      documentName: 'Insurance Card OCR',
      value: '456 Oak Ave, Springfield, IL 62702',
      confidence: 0.78,
      extractedAt: '2025-12-16T14:15:00Z',
    },
    severity: 'medium',
    resolutionStatus: 'Open',
  };
}
