import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchConflictDetail, fetchPatient360View, resolveConflict } from './patient360Api';

const getAccessTokenMock = vi.fn();

vi.mock('../../../shared/api/authInterceptor', () => ({
  getAccessToken: () => getAccessTokenMock(),
}));

describe('patient360Api', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    getAccessTokenMock.mockReturnValue('token-123');
  });

  it('returns authorization error for forbidden patient 360 fetch', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      status: 403,
    } as Response);

    const result = await fetchPatient360View('patient-1');

    expect(result).toEqual({
      success: false,
      error: "You are not authorized to view this patient's data.",
    });
  });

  it('maps conflict fetch 409 to already resolved message', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      status: 409,
    } as Response);

    const result = await fetchConflictDetail('conf-1');

    expect(result).toEqual({
      success: false,
      error: 'Conflict was already resolved by another user.',
    });
  });

  it('returns optimistic conflict marker when resolving an already-resolved conflict', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      status: 409,
    } as Response);

    const result = await resolveConflict({
      conflictId: 'conf-1',
      resolvedValue: '123 Main St',
      resolutionSource: 'A',
      notes: 'Prefer intake value',
    });

    expect(result).toEqual({
      success: false,
      error: 'This conflict was already resolved by another user. Please refresh.',
      isOptimisticConflict: true,
    });
  });
});
