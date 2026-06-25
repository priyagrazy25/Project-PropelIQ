import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  fetchRiskAssessments,
  getDateRangeOptions,
  sendRiskReminder,
} from './riskApi';

const authenticatedFetchMock = vi.fn();

vi.mock('../../../shared/api/authInterceptor', () => ({
  authenticatedFetch: (...args: unknown[]) => authenticatedFetchMock(...args),
}));

describe('riskApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('falls back to demo data for unauthorized requests and applies risk filter', async () => {
    authenticatedFetchMock.mockResolvedValue({ ok: false, status: 403 } as Response);

    const result = await fetchRiskAssessments({
      startDate: '2026-06-20',
      endDate: '2026-06-27',
      riskLevel: 'High',
    });

    expect(result.success).toBe(true);
    if (!result.success) {
      throw new Error('Expected success result with demo data fallback');
    }

    expect(result.isDemo).toBe(true);
    expect(result.data.length).toBeGreaterThan(0);
    expect(result.data.every((item) => item.riskLevel === 'High')).toBe(true);
  });

  it('sends immediate risk reminder successfully when backend returns 204', async () => {
    authenticatedFetchMock.mockResolvedValue({ ok: false, status: 204 } as Response);

    const result = await sendRiskReminder('apt-001');

    expect(result).toEqual({ success: true });
    expect(authenticatedFetchMock).toHaveBeenCalledWith(
      '/api/notification/reminders/apt-001',
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('returns expected date range options', () => {
    const ranges = getDateRangeOptions();

    expect(ranges).toHaveLength(3);
    expect(ranges[0]?.label).toBe('Today');
    expect(ranges[1]?.label).toBe('Next 7 Days');
    expect(ranges[2]?.label).toBe('Next 30 Days');
  });
});
