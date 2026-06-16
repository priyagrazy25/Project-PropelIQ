import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Search } from 'lucide-react';
import { type ChangeEvent, type FormEvent, useCallback } from 'react';

export interface SearchFilterValues {
  name: string;
  specialty: string;
  location: string;
  date: string;
}

interface SearchFiltersProps {
  filters: SearchFilterValues;
  onChange: (filters: SearchFilterValues) => void;
  onSearch: () => void;
  disabled?: boolean;
}

const SPECIALTIES = [
  { value: '', label: 'All Specialties' },
  { value: 'Primary Care', label: 'Primary Care' },
  { value: 'Cardiology', label: 'Cardiology' },
  { value: 'Dermatology', label: 'Dermatology' },
  { value: 'Orthopedics', label: 'Orthopedics' },
  { value: 'Pediatrics', label: 'Pediatrics' },
] as const;

const LOCATIONS = [
  { value: '', label: 'All Locations' },
  { value: 'Downtown', label: 'Downtown Clinic' },
  { value: 'North', label: 'North Medical Center' },
  { value: 'South', label: 'South Health Campus' },
] as const;

export function SearchFilters({
  filters,
  onChange,
  onSearch,
  disabled,
}: SearchFiltersProps) {
  const handleChange = useCallback(
    (field: keyof SearchFilterValues) =>
      (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        onChange({ ...filters, [field]: e.target.value });
      },
    [filters, onChange],
  );

  const handleSubmit = useCallback(
    (e: FormEvent<HTMLFormElement>) => {
      e.preventDefault();
      onSearch();
    },
    [onSearch],
  );

  return (
    <form
      className="flex flex-wrap items-end gap-4 rounded-lg border bg-card p-4"
      role="search"
      aria-label="Provider search filters"
      onSubmit={handleSubmit}
    >
      <div className="flex flex-col gap-1.5 flex-1 min-w-[180px]">
        <Label htmlFor="search-name">Provider Name</Label>
        <Input
          type="text"
          id="search-name"
          placeholder="Search by name..."
          aria-label="Search by provider name"
          value={filters.name}
          onChange={handleChange('name')}
          disabled={disabled}
        />
      </div>
      <div className="flex flex-col gap-1.5 flex-1 min-w-[160px]">
        <Label htmlFor="search-specialty">Specialty</Label>
        <select
          id="search-specialty"
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
          value={filters.specialty}
          onChange={handleChange('specialty')}
          disabled={disabled}
        >
          {SPECIALTIES.map((s) => (
            <option key={s.value} value={s.value}>
              {s.label}
            </option>
          ))}
        </select>
      </div>
      <div className="flex flex-col gap-1.5 flex-1 min-w-[160px]">
        <Label htmlFor="search-location">Location</Label>
        <select
          id="search-location"
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
          value={filters.location}
          onChange={handleChange('location')}
          disabled={disabled}
        >
          {LOCATIONS.map((l) => (
            <option key={l.value} value={l.value}>
              {l.label}
            </option>
          ))}
        </select>
      </div>
      <div className="flex flex-col gap-1.5 flex-1 min-w-[160px]">
        <Label htmlFor="search-date">Available Date</Label>
        <Input
          type="date"
          id="search-date"
          value={filters.date}
          onChange={handleChange('date')}
          disabled={disabled}
        />
      </div>
      <Button type="submit" aria-label="Search providers" disabled={disabled}>
        <Search className="h-4 w-4 mr-2" />
        Search
      </Button>
    </form>
  );
}
