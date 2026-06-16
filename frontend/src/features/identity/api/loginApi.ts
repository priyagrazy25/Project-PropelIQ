const API_BASE = '/api';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  userId: string;
  role: 'Patient' | 'Provider' | 'Admin' | 'FrontDesk';
  fullName: string;
}

export interface LoginError {
  status: number;
  message: string;
  remainingAttempts?: number;
  lockedUntil?: string;
}

export async function loginUser(
  data: LoginRequest,
): Promise<{ success: true; data: LoginResponse } | { success: false; error: LoginError }> {
  try {
    const response = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    });

    if (response.ok) {
      const body = (await response.json()) as LoginResponse;
      return { success: true, data: body };
    }

    if (response.status === 401) {
      const body: unknown = await response.json().catch(() => null);
      const parsed = body as Record<string, unknown> | null;
      const message =
        typeof parsed?.['message'] === 'string'
          ? parsed['message']
          : 'Invalid email or password.';
      const remainingAttempts =
        typeof parsed?.['remainingAttempts'] === 'number'
          ? parsed['remainingAttempts']
          : undefined;

      return {
        success: false,
        error: { status: 401, message, remainingAttempts },
      };
    }

    if (response.status === 423) {
      const body: unknown = await response.json().catch(() => null);
      const parsed = body as Record<string, unknown> | null;
      const lockedUntil =
        typeof parsed?.['lockedUntil'] === 'string' ? parsed['lockedUntil'] : undefined;

      return {
        success: false,
        error: {
          status: 423,
          message:
            'Your account has been locked due to multiple failed attempts. Please contact support.',
          lockedUntil,
        },
      };
    }

    return {
      success: false,
      error: {
        status: response.status,
        message: 'Login failed. Please try again.',
      },
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

export async function refreshAccessToken(): Promise<
  { success: true; accessToken: string } | { success: false }
> {
  try {
    const response = await fetch(`${API_BASE}/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
    });

    if (response.ok) {
      const body = (await response.json()) as { accessToken: string };
      return { success: true, accessToken: body.accessToken };
    }

    return { success: false };
  } catch {
    return { success: false };
  }
}
