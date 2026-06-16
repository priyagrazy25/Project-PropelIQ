import { authenticatedFetch } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

export type UserRole = 'Patient' | 'Provider' | 'Admin' | 'FrontDesk';
export type UserStatus = 'Active' | 'Inactive';

export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  status: UserStatus;
  phone: string | null;
  lastLogin: string | null;
}

export interface UserListResponse {
  users: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  phone?: string;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  phone?: string;
}

export interface AdminApiError {
  status: number;
  message: string;
}

export async function fetchUsers(params: {
  page: number;
  pageSize: number;
  search?: string;
  role?: UserRole | '';
  status?: UserStatus | '';
}): Promise<{ success: true; data: UserListResponse } | { success: false; error: AdminApiError }> {
  try {
    const query = new URLSearchParams({
      page: String(params.page),
      pageSize: String(params.pageSize),
    });
    if (params.search) query.set('search', params.search);
    if (params.role) query.set('role', params.role);
    if (params.status) query.set('status', params.status);

    const response = await authenticatedFetch(`${API_BASE}/admin/users?${query.toString()}`);

    if (response.ok) {
      const body = (await response.json()) as UserListResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to fetch users.' },
    };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}

export async function createUser(
  data: CreateUserRequest,
): Promise<{ success: true; user: UserListItem } | { success: false; error: AdminApiError }> {
  try {
    const response = await authenticatedFetch(`${API_BASE}/admin/users`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });

    if (response.ok) {
      const body = (await response.json()) as UserListItem;
      return { success: true, user: body };
    }

    const errorBody: unknown = await response.json().catch(() => null);
    const parsed = errorBody as Record<string, unknown> | null;
    const message =
      typeof parsed?.['message'] === 'string' ? parsed['message'] : 'Failed to create user.';

    return { success: false, error: { status: response.status, message } };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}

export async function updateUser(
  userId: string,
  data: UpdateUserRequest,
): Promise<{ success: true; user: UserListItem } | { success: false; error: AdminApiError }> {
  try {
    const response = await authenticatedFetch(`${API_BASE}/admin/users/${userId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });

    if (response.ok) {
      const body = (await response.json()) as UserListItem;
      return { success: true, user: body };
    }

    const errorBody: unknown = await response.json().catch(() => null);
    const parsed = errorBody as Record<string, unknown> | null;
    const message =
      typeof parsed?.['message'] === 'string' ? parsed['message'] : 'Failed to update user.';

    return { success: false, error: { status: response.status, message } };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}

export async function deactivateUser(
  userId: string,
): Promise<{ success: true } | { success: false; error: AdminApiError }> {
  try {
    const response = await authenticatedFetch(`${API_BASE}/admin/users/${userId}/deactivate`, {
      method: 'POST',
    });

    if (response.ok) {
      return { success: true };
    }

    const errorBody: unknown = await response.json().catch(() => null);
    const parsed = errorBody as Record<string, unknown> | null;
    const message =
      typeof parsed?.['message'] === 'string'
        ? parsed['message']
        : 'Failed to deactivate user.';

    return { success: false, error: { status: response.status, message } };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}

export async function reactivateUser(
  userId: string,
): Promise<{ success: true } | { success: false; error: AdminApiError }> {
  try {
    const response = await authenticatedFetch(`${API_BASE}/admin/users/${userId}/reactivate`, {
      method: 'POST',
    });

    if (response.ok) {
      return { success: true };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to reactivate user.' },
    };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}
