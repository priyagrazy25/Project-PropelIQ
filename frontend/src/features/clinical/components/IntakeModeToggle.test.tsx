import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { setIntakeDraft, setIntakeMode, type IntakeDraft } from '../clinicalSlice';
import { IntakeModeToggle } from './IntakeModeToggle';

const mockDispatch = vi.fn();
const mockNavigate = vi.fn();
const mockUseAppSelector = vi.fn();

vi.mock('../../../app/hooks', () => ({
  useAppDispatch: () => mockDispatch,
  useAppSelector: (selector: (state: unknown) => unknown) => mockUseAppSelector(selector),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const currentData: IntakeDraft = {
  allergies: { name: 'Peanut', type: 'Food', severity: 'High', reaction: 'Rash' },
  medications: { name: 'Ibuprofen', dosage: '200mg', notes: 'As needed' },
  history: { conditions: ['Asthma'], additionalNotes: 'None' },
  symptoms: { chiefComplaint: 'Headache', duration: '2 days', severity: 'Moderate' },
};

describe('IntakeModeToggle', () => {
  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it('switches from ai to manual and snapshots draft data', () => {
    mockUseAppSelector.mockImplementation((selector: (state: unknown) => unknown) =>
      selector({ clinical: { intakeMode: 'ai', aiAvailable: true } }),
    );

    render(<IntakeModeToggle currentData={currentData} />);

    fireEvent.click(screen.getByRole('switch', { name: 'Toggle between AI and manual intake mode' }));

    expect(mockDispatch).toHaveBeenCalledWith(setIntakeDraft(currentData));
    expect(mockDispatch).toHaveBeenCalledWith(setIntakeMode('manual'));
    expect(mockNavigate).toHaveBeenCalledWith('/intake/manual');
  });

  it('disables switch when AI is unavailable in manual mode', () => {
    mockUseAppSelector.mockImplementation((selector: (state: unknown) => unknown) =>
      selector({ clinical: { intakeMode: 'manual', aiAvailable: false } }),
    );

    render(<IntakeModeToggle currentData={null} />);

    expect(
      screen.getByRole('switch', { name: 'Toggle between AI and manual intake mode' }),
    ).toBeDisabled();
  });
});
