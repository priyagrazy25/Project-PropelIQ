import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { InsurancePreCheckForm } from './InsurancePreCheckForm';

const verifyInsuranceMock = vi.fn();

vi.mock('../api/intakeApi', async () => {
  const actual = await vi.importActual<typeof import('../api/intakeApi')>('../api/intakeApi');
  return {
    ...actual,
    verifyInsurance: (...args: unknown[]) => verifyInsuranceMock(...args),
  };
});

describe('InsurancePreCheckForm', () => {
  afterEach(() => {
    cleanup();
    verifyInsuranceMock.mockReset();
  });

  it('validates required fields before submit', async () => {
    render(
      <MemoryRouter>
        <InsurancePreCheckForm />
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Verify Coverage' }));

    expect(await screen.findByText('Insurance provider is required.')).toBeInTheDocument();
    expect(await screen.findByText('Member ID is required.')).toBeInTheDocument();
    expect(verifyInsuranceMock).not.toHaveBeenCalled();
  });

  it('sanitizes input and displays verified result', async () => {
    verifyInsuranceMock.mockResolvedValue({
      success: true,
      data: {
        status: 'verified',
        insuranceName: 'Blue Cross',
        message: 'Verified',
        details: 'Coverage active',
      },
    });

    render(
      <MemoryRouter>
        <InsurancePreCheckForm />
      </MemoryRouter>,
    );

    fireEvent.change(screen.getByLabelText(/Insurance Provider/i), {
      target: { value: "<Blue Cross>'" },
    });
    fireEvent.change(screen.getByLabelText(/Member ID/i), {
      target: { value: 'AB-12345;' },
    });

    fireEvent.click(screen.getByRole('button', { name: 'Verify Coverage' }));

    await waitFor(() => {
      expect(verifyInsuranceMock).toHaveBeenCalledWith(
        { insuranceName: 'Blue Cross', memberId: 'AB-12345' },
        expect.any(AbortSignal),
      );
    });

    expect(await screen.findByText('Coverage Verified')).toBeInTheDocument();
    expect(await screen.findByText('Coverage active')).toBeInTheDocument();
  });
});
