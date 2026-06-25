import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ProviderSearchPage } from './ProviderSearchPage';

const mockSearchProviders = vi.fn();

vi.mock('../api/schedulingApi', () => ({
  searchProviders: (params: unknown) => mockSearchProviders(params),
}));

vi.mock('../hooks/useSignalRSlots', () => ({
  useSignalRSlots: () => ({ connectionState: 'connected' }),
}));

describe('ProviderSearchPage', () => {
  it('executes provider search flow and renders results from API response', async () => {
    mockSearchProviders.mockResolvedValue({
      success: true,
      data: {
        providers: [
          {
            id: 'provider-1',
            fullName: 'Dr. Maya Patel',
            specialty: 'Cardiology',
            location: 'Downtown',
            rating: 4.7,
            isAcceptingPatients: true,
            nextAvailableDate: new Date(2026, 5, 20, 9, 0, 0).toISOString(),
            availableSlots: [
              {
                id: 'slot-1',
                startTime: new Date(2026, 5, 20, 9, 0, 0).toISOString(),
                endTime: new Date(2026, 5, 20, 9, 30, 0).toISOString(),
                isAvailable: true,
              },
            ],
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      },
    });

    render(
      <MemoryRouter>
        <ProviderSearchPage />
      </MemoryRouter>,
    );

    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Search by provider name'), 'Maya');
    await user.click(screen.getByRole('button', { name: 'Search providers' }));

    await waitFor(() => {
      expect(mockSearchProviders).toHaveBeenCalledWith({
        name: 'Maya',
        specialty: undefined,
        location: undefined,
        date: undefined,
        page: 1,
        pageSize: 20,
        sortBy: 'earliest',
      });
    });

    expect(await screen.findByText('Dr. Maya Patel')).toBeInTheDocument();
    expect(screen.getByText('Cardiology · Downtown')).toBeInTheDocument();
  });
});
