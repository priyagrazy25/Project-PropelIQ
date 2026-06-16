const API_BASE = '/api';

export interface RegistrationRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  gender: string;
  password: string;
}

export interface RegistrationError {
  status: number;
  message: string;
  field?: string;
}

export async function registerPatient(
  data: RegistrationRequest,
): Promise<{ success: true } | { success: false; error: RegistrationError }> {
  try {
    const response = await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });

    if (response.ok) {
      return { success: true };
    }

    if (response.status === 409) {
      return {
        success: false,
        error: {
          status: 409,
          message: 'This email is already registered.',
          field: 'email',
        },
      };
    }

    const body: unknown = await response.json().catch(() => null);
    const message =
      body !== null &&
      typeof body === 'object' &&
      'message' in body &&
      typeof (body as { message: unknown }).message === 'string'
        ? (body as { message: string }).message
        : 'Registration failed. Please try again.';

    return {
      success: false,
      error: { status: response.status, message },
    };
  } catch {
    return {
      success: false,
      error: {
        status: 0,
        message: 'Unable to connect. Please check your network and try again.',
      },
    };
  }
}
