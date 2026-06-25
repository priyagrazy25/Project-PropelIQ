import { describe, expect, it, vi } from 'vitest';
import {
  downloadPdfConfirmation,
  generatePdfConfirmation,
  openPdfConfirmation,
} from './notificationApi';

const authenticatedFetchMock = vi.fn();

vi.mock('../../../shared/api/authInterceptor', () => ({
  authenticatedFetch: (...args: unknown[]) => authenticatedFetchMock(...args),
}));

describe('notificationApi', () => {
  it('treats 202 as success for PDF generation', async () => {
    authenticatedFetchMock.mockResolvedValue({ ok: false, status: 202 });

    const result = await generatePdfConfirmation('apt/001');

    expect(result).toEqual({ success: true });
  });

  it('returns not-generated message on 404 PDF download', async () => {
    authenticatedFetchMock.mockResolvedValue({ ok: false, status: 404 });

    const result = await downloadPdfConfirmation('apt-404');

    expect(result).toEqual({
      success: false,
      error: 'PDF not yet generated for this appointment.',
    });
  });

  it('opens PDF confirmation in new tab with encoded appointment id', () => {
    const openSpy = vi.spyOn(window, 'open').mockImplementation(() => null);

    openPdfConfirmation('apt/abc');

    expect(openSpy).toHaveBeenCalledWith(
      '/api/notification/pdf/apt%2Fabc',
      '_blank',
      'noopener,noreferrer',
    );

    openSpy.mockRestore();
  });
});
