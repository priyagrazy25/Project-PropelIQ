import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { NoShowRiskDashboardPage } from './NoShowRiskDashboardPage';

const fetchRiskAssessmentsMock = vi.fn();
const getDateRangeOptionsMock = vi.fn();
const sendRiskReminderMock = vi.fn();
const toastSuccessMock = vi.fn();
const toastErrorMock = vi.fn();

vi.mock('../api/riskApi', () => ({
  fetchRiskAssessments: (...args: unknown[]) => fetchRiskAssessmentsMock(...args),
  getDateRangeOptions: (...args: unknown[]) => getDateRangeOptionsMock(...args),
  sendRiskReminder: (...args: unknown[]) => sendRiskReminderMock(...args),
}));

vi.mock('sonner', () => ({
  toast: {
    success: (...args: unknown[]) => toastSuccessMock(...args),
    error: (...args: unknown[]) => toastErrorMock(...args),
  },
}));

function renderPage() {
  return render(
    <MemoryRouter>
      <NoShowRiskDashboardPage />
    </MemoryRouter>,
  );
}

describe('NoShowRiskDashboardPage', () => {
  afterEach(() => {
    cleanup();
  });

  beforeEach(() => {
    vi.clearAllMocks();

    getDateRangeOptionsMock.mockReturnValue([
      { label: 'Today', startDate: '2026-06-20', endDate: '2026-06-21' },
      { label: 'Next 7 Days', startDate: '2026-06-20', endDate: '2026-06-27' },
      { label: 'Next 30 Days', startDate: '2026-06-20', endDate: '2026-07-20' },
    ]);

    fetchRiskAssessmentsMock.mockResolvedValue({
      success: true,
      data: [
        {
          appointmentId: 'apt-low',
          patientId: 'p-low',
          patientName: 'Low Risk Patient',
          appointmentDateTime: '2026-06-20T11:00:00.000Z',
          providerName: 'Dr. Lane',
          riskScore: 20,
          riskLevel: 'Low',
          contributingFactors: [],
        },
        {
          appointmentId: 'apt-high',
          patientId: 'p-high',
          patientName: 'High Risk Patient',
          appointmentDateTime: '2026-06-20T10:00:00.000Z',
          providerName: 'Dr. Ray',
          riskScore: 85,
          riskLevel: 'High',
          contributingFactors: ['prior no-show'],
        },
      ],
    });

    sendRiskReminderMock.mockResolvedValue({ success: true });
  });

  it('renders table sorted by highest risk first', async () => {
    renderPage();

    await screen.findByText('High Risk Patient');

    const rows = screen.getAllByRole('row');
    const firstDataRow = rows[1];
    expect(within(firstDataRow as HTMLElement).getByText('High Risk Patient')).toBeInTheDocument();
  });

  it('reloads with high-risk filter when selection changes', async () => {
    renderPage();

    await screen.findByText('High Risk Patient');

    fireEvent.change(screen.getAllByLabelText('Risk level filter')[0] as HTMLElement, {
      target: { value: 'High' },
    });

    expect(fetchRiskAssessmentsMock).toHaveBeenCalledWith(
      expect.objectContaining({ riskLevel: 'High' }),
    );
  });

  it('sends reminder from action button', async () => {
    renderPage();

    await screen.findByText('High Risk Patient');

    fireEvent.click(screen.getAllByRole('button', { name: 'Send Reminder' })[0] as HTMLElement);

    expect(sendRiskReminderMock).toHaveBeenCalled();
  });
});
