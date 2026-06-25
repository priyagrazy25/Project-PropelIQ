import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { SearchFilters, type SearchFilterValues } from './SearchFilters';

describe('SearchFilters', () => {
  it('renders filter inputs and calls onSearch on submit', () => {
    const onChange = vi.fn();
    const onSearch = vi.fn();

    const filters: SearchFilterValues = {
      name: '',
      specialty: '',
      location: '',
      date: '',
    };

    render(
      <SearchFilters
        filters={filters}
        onChange={onChange}
        onSearch={onSearch}
      />,
    );

    expect(screen.getByLabelText('Search by provider name')).toBeInTheDocument();
    expect(screen.getByLabelText('Specialty')).toBeInTheDocument();
    expect(screen.getByLabelText('Location')).toBeInTheDocument();
    expect(screen.getByLabelText('Available Date')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Search by provider name'), {
      target: { value: 'Patel' },
    });
    expect(onChange).toHaveBeenCalledWith({ ...filters, name: 'Patel' });

    fireEvent.change(screen.getByLabelText('Specialty'), {
      target: { value: 'Cardiology' },
    });
    expect(onChange).toHaveBeenCalledWith({ ...filters, specialty: 'Cardiology' });

    fireEvent.change(screen.getByLabelText('Location'), {
      target: { value: 'Downtown' },
    });
    expect(onChange).toHaveBeenCalledWith({ ...filters, location: 'Downtown' });

    fireEvent.change(screen.getByLabelText('Available Date'), {
      target: { value: '2026-06-20' },
    });
    expect(onChange).toHaveBeenCalledWith({ ...filters, date: '2026-06-20' });

    fireEvent.click(screen.getByRole('button', { name: 'Search providers' }));
    expect(onSearch).toHaveBeenCalledTimes(1);
  });
});
