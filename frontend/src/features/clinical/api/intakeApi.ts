import { authenticatedFetch } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

export interface IntakeMessage {
  id: string;
  role: 'user' | 'ai' | 'system';
  content: string;
  timestamp: string;
  extractedFields?: ExtractedField[];
}

export interface ExtractedField {
  key: string;
  label: string;
  value: string;
  category: 'allergy' | 'medication' | 'symptom' | 'history' | 'vital';
  confidence: number;
}

export interface IntakeSendRequest {
  appointmentId?: string;
  message: string;
  conversationId?: string;
}

export interface IntakeSendResponse {
  conversationId: string;
  reply: IntakeMessage;
  extractedFields: ExtractedField[];
  isComplete: boolean;
}

export interface IntakeApiError {
  status: number;
  message: string;
}

export async function sendIntakeMessage(
  request: IntakeSendRequest,
  signal?: AbortSignal,
): Promise<
  | { success: true; data: IntakeSendResponse }
  | { success: false; error: IntakeApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/clinical/intake/chat`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
        signal,
      },
    );

    if (response.ok) {
      const body = (await response.json()) as IntakeSendResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to process intake message.',
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

export async function checkAiAvailability(
  signal?: AbortSignal,
): Promise<boolean> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/clinical/intake/ai-status`,
      { signal },
    );
    return response.ok;
  } catch (err) {
    // Aborted requests (e.g. React Strict Mode cleanup) should not
    // mark AI as unavailable — return true so the next mount retries.
    if (err instanceof DOMException && err.name === 'AbortError') {
      return true;
    }
    return false;
  }
}

export async function submitIntakeReview(
  conversationId: string,
  fields: ExtractedField[],
  signal?: AbortSignal,
): Promise<{ success: true } | { success: false; error: IntakeApiError }> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/clinical/intake/submit`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ conversationId, fields }),
        signal,
      },
    );

    if (response.ok) {
      return { success: true };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to submit intake review.',
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

// ── Insurance Pre-Check (FR-018, UC-013) ──

export type InsuranceVerificationStatus =
  | 'verified'
  | 'partial'
  | 'unrecognized'
  | 'unavailable';

export interface InsuranceVerifyRequest {
  insuranceName: string;
  memberId: string;
}

export interface InsuranceVerifyResponse {
  status: InsuranceVerificationStatus;
  insuranceName: string;
  message: string;
  details?: string;
}

export async function verifyInsurance(
  request: InsuranceVerifyRequest,
  signal?: AbortSignal,
): Promise<
  | { success: true; data: InsuranceVerifyResponse }
  | { success: false; error: IntakeApiError }
> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/clinical/insurance/verify`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
        signal,
      },
    );

    if (response.ok) {
      const body = (await response.json()) as InsuranceVerifyResponse;
      return { success: true, data: body };
    }

    if (response.status === 404) {
      return {
        success: true,
        data: {
          status: 'unavailable',
          insuranceName: request.insuranceName,
          message: 'Verification unavailable',
          details:
            'No insurance records found. You may proceed with your visit.',
        },
      };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Failed to verify insurance.',
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
