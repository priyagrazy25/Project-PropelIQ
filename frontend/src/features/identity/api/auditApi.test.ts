import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  exportAuditLogs,
  fetchAiAuditLogs,
  fetchAuditLogs,
  fetchAuditStats,
} from './auditApi';

const authenticatedFetchMock = vi.fn();

vi.mock('../../../shared/api/authInterceptor', () => ({
  authenticatedFetch: (...args: unknown[]) => authenticatedFetchMock(...args),
}));

describe('auditApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('builds query params and returns audit logs on success', async () => {
    authenticatedFetchMock.mockResolvedValue({
      ok: true,
      json: async () => ({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 50,
        totalPages: 0,
      }),
    } as Response);

    const result = await fetchAuditLogs({
      startDate: '2026-06-01',
      endDate: '2026-06-30',
      actorName: 'Admin',
      action: 'View',
      resource: 'Patient',
      resourceId: 'p-1',
      ipAddress: '127.0.0.1',
      page: 2,
      pageSize: 25,
    });

    expect(result.success).toBe(true);
    expect(authenticatedFetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/auditlogs?'),
    );

    const url = String(authenticatedFetchMock.mock.calls[0]?.[0]);
    expect(url).toContain('startDate=2026-06-01');
    expect(url).toContain('endDate=2026-06-30');
    expect(url).toContain('actorName=Admin');
    expect(url).toContain('action=View');
    expect(url).toContain('resource=Patient');
    expect(url).toContain('resourceId=p-1');
    expect(url).toContain('ipAddress=127.0.0.1');
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=25');
  });

  it('maps AI audit API failures to a user-friendly error', async () => {
    authenticatedFetchMock.mockResolvedValue({ ok: false, status: 500 } as Response);

    const result = await fetchAiAuditLogs({ page: 1, pageSize: 50 });

    expect(result.success).toBe(false);
    if (result.success) {
      throw new Error('Expected failure result');
    }

    expect(result.error.status).toBe(500);
    expect(result.error.message).toBe('Failed to fetch AI audit logs.');
  });

  it('returns stats when backend responds successfully', async () => {
    authenticatedFetchMock.mockResolvedValue({
      ok: true,
      json: async () => ({
        totalRecords: 10,
        uniqueActors: 3,
        actionBreakdown: { View: 8 },
        resourceBreakdown: { Patient: 8 },
        timeRange: { start: '2026-06-01', end: '2026-06-30' },
      }),
    } as Response);

    const result = await fetchAuditStats('2026-06-01', '2026-06-30');

    expect(result.success).toBe(true);
    expect(authenticatedFetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/auditlogs/stats?startDate=2026-06-01&endDate=2026-06-30'),
    );
  });

  it('exports audit logs as blob when export endpoint succeeds', async () => {
    const blob = new Blob(['id,name\n1,entry'], { type: 'text/csv' });
    authenticatedFetchMock.mockResolvedValue({
      ok: true,
      blob: async () => blob,
    } as Response);

    const result = await exportAuditLogs({ actorName: 'Admin', action: 'View' });

    expect(result.success).toBe(true);
    if (!result.success) {
      throw new Error('Expected success export result');
    }

    expect(result.data).toBe(blob);
    expect(authenticatedFetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/auditlogs/export?actorName=Admin&action=View'),
    );
  });
});
