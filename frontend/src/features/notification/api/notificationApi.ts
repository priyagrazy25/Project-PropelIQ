import { authenticatedFetch } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

/**
 * Triggers PDF confirmation generation and email delivery for an appointment.
 */
export async function generatePdfConfirmation(
  appointmentId: string,
): Promise<{ success: boolean; error?: string }> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/notification/pdf/${encodeURIComponent(appointmentId)}/generate`,
      { method: 'POST' },
    );

    if (response.ok || response.status === 202) {
      return { success: true };
    }

    return { success: false, error: 'Failed to generate PDF confirmation.' };
  } catch {
    return { success: false, error: 'Unable to connect to server.' };
  }
}

/**
 * Opens the PDF confirmation in a new browser tab for viewing/download.
 */
export function openPdfConfirmation(appointmentId: string): void {
  window.open(
    `${API_BASE}/notification/pdf/${encodeURIComponent(appointmentId)}`,
    '_blank',
    'noopener,noreferrer',
  );
}

/**
 * Downloads the PDF confirmation as a blob and triggers a file download.
 */
export async function downloadPdfConfirmation(
  appointmentId: string,
): Promise<{ success: boolean; error?: string }> {
  try {
    const response = await authenticatedFetch(
      `${API_BASE}/notification/pdf/${encodeURIComponent(appointmentId)}`,
    );

    if (!response.ok) {
      if (response.status === 404) {
        return {
          success: false,
          error: 'PDF not yet generated for this appointment.',
        };
      }
      return { success: false, error: 'Failed to download PDF.' };
    }

    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `Confirmation_${appointmentId}.pdf`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    return { success: true };
  } catch {
    return { success: false, error: 'Unable to connect to server.' };
  }
}
