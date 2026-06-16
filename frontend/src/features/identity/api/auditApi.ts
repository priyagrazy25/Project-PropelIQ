import { authenticatedFetch } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

export interface AuditLogEntry {
  actorId: string | null;
  actorName: string;
  action: string;
  resource: string;
  resourceId: string | null;
  ipAddress: string | null;
  correlationId: string | null;
  timestamp: string;
}

export interface AiAuditLogEntry {
  actorId: string | null;
  actorName: string;
  functionName: string;
  modelId: string;
  modelVersion: string;
  promptTokens: number;
  completionTokens: number;
  confidenceScore: number | null;
  durationMs: number;
  success: boolean;
  errorMessage: string | null;
  correlationId: string | null;
}

export interface PaginatedAuditResponse {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PaginatedAiAuditResponse {
  items: AiAuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuditStatsResponse {
  totalRecords: number;
  uniqueActors: number;
  actionBreakdown: Record<string, number>;
  resourceBreakdown: Record<string, number>;
  timeRange: {
    start: string;
    end: string;
  };
}

export interface AuditApiError {
  status: number;
  message: string;
}

export interface AuditLogFilter {
  startDate?: string;
  endDate?: string;
  actorName?: string;
  action?: string;
  resource?: string;
  resourceId?: string;
  ipAddress?: string;
  page?: number;
  pageSize?: number;
}

export async function fetchAuditLogs(
  params: AuditLogFilter
): Promise<{ success: true; data: PaginatedAuditResponse } | { success: false; error: AuditApiError }> {
  try {
    const query = new URLSearchParams();
    if (params.startDate) query.set('startDate', params.startDate);
    if (params.endDate) query.set('endDate', params.endDate);
    if (params.actorName) query.set('actorName', params.actorName);
    if (params.action) query.set('action', params.action);
    if (params.resource) query.set('resource', params.resource);
    if (params.resourceId) query.set('resourceId', params.resourceId);
    if (params.ipAddress) query.set('ipAddress', params.ipAddress);
    query.set('page', String(params.page ?? 1));
    query.set('pageSize', String(params.pageSize ?? 50));

    const response = await authenticatedFetch(`${API_BASE}/auditlogs?${query.toString()}`);

    if (response.ok) {
      const body = (await response.json()) as PaginatedAuditResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to fetch audit logs.' },
    };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}

export async function fetchAiAuditLogs(
  params: { startDate?: string; endDate?: string; modelId?: string; functionName?: string; successOnly?: boolean; page?: number; pageSize?: number }
): Promise<{ success: true; data: PaginatedAiAuditResponse } | { success: false; error: AuditApiError }> {
  try {
    const query = new URLSearchParams();
    if (params.startDate) query.set('startDate', params.startDate);
    if (params.endDate) query.set('endDate', params.endDate);
    if (params.modelId) query.set('modelId', params.modelId);
    if (params.functionName) query.set('functionName', params.functionName);
    if (params.successOnly !== undefined) query.set('successOnly', String(params.successOnly));
    query.set('page', String(params.page ?? 1));
    query.set('pageSize', String(params.pageSize ?? 50));

    const response = await authenticatedFetch(`${API_BASE}/auditlogs/ai?${query.toString()}`);

    if (response.ok) {
      const body = (await response.json()) as PaginatedAiAuditResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to fetch AI audit logs.' },
    };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}

export async function fetchAuditStats(
  startDate?: string,
  endDate?: string
): Promise<{ success: true; data: AuditStatsResponse } | { success: false; error: AuditApiError }> {
  try {
    const query = new URLSearchParams();
    if (startDate) query.set('startDate', startDate);
    if (endDate) query.set('endDate', endDate);

    const response = await authenticatedFetch(`${API_BASE}/auditlogs/stats?${query.toString()}`);

    if (response.ok) {
      const body = (await response.json()) as AuditStatsResponse;
      return { success: true, data: body };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to fetch audit statistics.' },
    };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}

export async function exportAuditLogs(
  params: AuditLogFilter
): Promise<{ success: true; data: Blob } | { success: false; error: AuditApiError }> {
  try {
    const query = new URLSearchParams();
    if (params.startDate) query.set('startDate', params.startDate);
    if (params.endDate) query.set('endDate', params.endDate);
    if (params.actorName) query.set('actorName', params.actorName);
    if (params.action) query.set('action', params.action);
    if (params.resource) query.set('resource', params.resource);

    const response = await authenticatedFetch(`${API_BASE}/auditlogs/export?${query.toString()}`);

    if (response.ok) {
      const blob = await response.blob();
      return { success: true, data: blob };
    }

    return {
      success: false,
      error: { status: response.status, message: 'Failed to export audit logs.' },
    };
  } catch {
    return {
      success: false,
      error: { status: 0, message: 'Network error. Please try again.' },
    };
  }
}
