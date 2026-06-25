import { cleanup, fireEvent, render, screen, act } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { PatientSearchResult } from './PatientSearchBar';
import { PatientSearchBar } from './PatientSearchBar';

const mockPatient: PatientSearchResult = {
  id: 'pat-001',
  fullName: 'Alice Nguyen',
  dateOfBirth: '1990-05-14',
  contactNumber: '555-0100',
  email: 'alice@example.com',
  mrn: 'MRN-PAT00011',
};

describe('PatientSearchBar', () => {
  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it('renders search input and Search button', () => {
    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={vi.fn().mockResolvedValue({ success: true, data: [] })}
      />,
    );

    expect(screen.getByRole('textbox', { name: /search patient/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /search/i })).toBeDisabled();
  });

  it('enables the Search button once 2 or more characters are typed', () => {
    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={vi.fn().mockResolvedValue({ success: true, data: [] })}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'Al' } });

    expect(screen.getByRole('button', { name: /search/i })).not.toBeDisabled();
  });

  it('does not fire search for a single character', () => {
    vi.useFakeTimers();
    const searchFn = vi.fn().mockResolvedValue({ success: true, data: [] });

    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={searchFn}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'A' } });

    act(() => { vi.runAllTimers(); });

    expect(searchFn).not.toHaveBeenCalled();
  });

  it('fires search via debounce for 2+ characters', () => {
    vi.useFakeTimers();
    const searchFn = vi.fn().mockResolvedValue({ success: true, data: [] });

    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={searchFn}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'Ali' } });

    act(() => { vi.runAllTimers(); });

    expect(searchFn).toHaveBeenCalledWith('Ali', expect.any(AbortSignal));
  });

  it('fires search immediately when Search button is clicked', () => {
    const searchFn = vi.fn().mockResolvedValue({ success: true, data: [] });

    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={searchFn}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'Ali' } });
    fireEvent.click(screen.getByRole('button', { name: /search/i }));

    expect(searchFn).toHaveBeenCalledWith('Ali', expect.any(AbortSignal));
  });

  it('displays MRN from search result — not blank', async () => {
    const searchFn = vi.fn().mockResolvedValue({ success: true, data: [mockPatient] });

    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={searchFn}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'Alice' } });
    fireEvent.click(screen.getByRole('button', { name: /search/i }));

    await screen.findByText('Alice Nguyen');

    expect(screen.getByText(/MRN: MRN-PAT00011/)).toBeInTheDocument();
  });

  it('shows "No patients found." when search returns empty list', async () => {
    const searchFn = vi.fn().mockResolvedValue({ success: true, data: [] });

    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={searchFn}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'ZZ' } });
    fireEvent.click(screen.getByRole('button', { name: /search/i }));

    await screen.findByText('No patients found.');
  });

  it('shows error message when searchFn returns failure', async () => {
    const searchFn = vi.fn().mockResolvedValue({
      success: false,
      error: { message: 'Patient search failed.' },
    });

    render(
      <PatientSearchBar
        onSelect={vi.fn()}
        onCreateNew={vi.fn()}
        searchFn={searchFn}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'Ali' } });
    fireEvent.click(screen.getByRole('button', { name: /search/i }));

    await screen.findByRole('alert');
    expect(screen.getByText('Patient search failed.')).toBeInTheDocument();
  });

  it('calls onSelect with the patient when a result is clicked', async () => {
    const onSelect = vi.fn();
    const searchFn = vi.fn().mockResolvedValue({ success: true, data: [mockPatient] });

    render(
      <PatientSearchBar
        onSelect={onSelect}
        onCreateNew={vi.fn()}
        searchFn={searchFn}
      />,
    );

    const input = screen.getByRole('textbox', { name: /search patient/i });
    fireEvent.change(input, { target: { value: 'Alice' } });
    fireEvent.click(screen.getByRole('button', { name: /search/i }));

    const result = await screen.findByRole('option', { name: /Alice Nguyen/i });
    fireEvent.click(result);

    expect(onSelect).toHaveBeenCalledWith(mockPatient);
  });
});
