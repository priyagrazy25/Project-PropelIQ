import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchCodeVerificationQueue, overrideCode, verifyCode } from './codingApi';

const getAccessTokenMock = vi.fn();

vi.mock('../../../shared/api/authInterceptor', () => ({
  getAccessToken: () => getAccessTokenMock(),
}));

describe('codingApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    getAccessTokenMock.mockReturnValue('token-abc');
  });

  it('appends query params when fetching verification queue with filters', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      json: async () => ({ entries: [], totalCount: 0, pendingCount: 0 }),
    } as Response);

    await fetchCodeVerificationQueue({ status: 'Pending', codeType: 'ICD-10' });

    expect(fetchSpy).toHaveBeenCalledTimes(1);
    expect(fetchSpy.mock.calls[0][0]).toBe('/api/clinical/codes/verification-queue?status=Pending&codeType=ICD-10');
  });

  it('maps verify 409 into already-verified message', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      status: 409,
    } as Response);

    const result = await verifyCode('entry-1', { action: 'accept' });

    expect(result).toEqual({ success: false, error: 'Code already verified by another user.' });
  });

  it('maps override 409 into already-verified message', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      status: 409,
    } as Response);

    const result = await overrideCode('entry-2', {
      code: 'E11.9',
      description: 'Type 2 diabetes mellitus without complications',
      overrideReason: 'clinical_judgment',
      notes: 'Updated after chart review',
    });

    expect(result).toEqual({ success: false, error: 'Code already verified by another user.' });
  });
});
