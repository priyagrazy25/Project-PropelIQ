import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { Search } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';

export interface PatientSearchResult {
  id: string;
  fullName: string;
  dateOfBirth: string;
  contactNumber: string;
  email: string;
  mrn: string;
}

interface PatientSearchBarProps {
  onSelect: (patient: PatientSearchResult) => void;
  onCreateNew: () => void;
  searchFn: (
    query: string,
    signal?: AbortSignal,
  ) => Promise<
    | { success: true; data: PatientSearchResult[] }
    | { success: false; error: { message: string } }
  >;
}

const DEBOUNCE_MS = 350;

export function PatientSearchBar({
  onSelect,
  onCreateNew,
  searchFn,
}: PatientSearchBarProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<PatientSearchResult[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const executeSearch = useCallback(
    (searchQuery: string) => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setLoading(true);
      setError(null);
      setHasSearched(true);

      searchFn(searchQuery, controller.signal)
        .then((result) => {
          if (controller.signal.aborted) return;
          if (result.success) {
            setResults(result.data);
          } else {
            setError(result.error.message);
          }
        })
        .catch(() => {
          if (!controller.signal.aborted) {
            setError('Search failed. Please try again.');
          }
        })
        .finally(() => {
          if (!controller.signal.aborted) {
            setLoading(false);
          }
        });
    },
    [searchFn],
  );

  useEffect(() => {
    // Auto-load all patients on mount so list is visible immediately (like provider dropdown)
    executeSearch('');
    return () => {
      abortRef.current?.abort();
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [executeSearch]);

  const handleInputChange = (value: string) => {
    setQuery(value);
    setSelectedId(null);

    if (timerRef.current) clearTimeout(timerRef.current);

    if (value.trim().length >= 2) {
      timerRef.current = setTimeout(() => {
        executeSearch(value.trim());
      }, DEBOUNCE_MS);
    } else if (value.trim().length === 0) {
      // Show all patients when search box is cleared
      timerRef.current = setTimeout(() => {
        executeSearch('');
      }, DEBOUNCE_MS);
    }
  };

  const handleSearchClick = () => {
    executeSearch(query.trim());
  };

  const handleSelect = (patient: PatientSearchResult) => {
    setSelectedId(patient.id);
    onSelect(patient);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSearchClick();
    }
  };

  return (
    <div>
      <div className="flex gap-3 mb-5">
        <Input
          type="text"
          placeholder="Search by name, phone, or MRN..."
          aria-label="Search patient"
          value={query}
          onChange={(e) => handleInputChange(e.target.value)}
          onKeyDown={handleKeyDown}
          className="flex-1 h-10"
        />
        <Button onClick={handleSearchClick}>
          <Search className="h-4 w-4 mr-1" aria-hidden="true" />
          Search
        </Button>
      </div>

      {loading && (
        <div className="space-y-3" aria-busy="true">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      )}

      {error && (
        <div
          className="flex items-center gap-2 text-destructive text-sm mb-3"
          role="alert"
        >
          <span aria-hidden="true">!</span>
          <span>{error}</span>
        </div>
      )}

      {!loading && hasSearched && results.length === 0 && !error && (
        <p className="text-sm text-muted-foreground mb-3">No patients found.</p>
      )}

      {!loading &&
        results.map((patient) => (
          <button
            key={patient.id}
            type="button"
            role="option"
            aria-selected={selectedId === patient.id}
            className={cn(
              'w-full text-left p-3 border rounded-md flex justify-between items-center mb-3 cursor-pointer transition-colors duration-(--duration-micro)',
              selectedId === patient.id
                ? 'bg-accent border-primary'
                : 'border-border hover:bg-accent hover:border-primary',
            )}
            onClick={() => handleSelect(patient)}
          >
            <div>
              <div className="text-sm font-medium">{patient.fullName}</div>
              <div className="text-xs text-muted-foreground">
                DOB: {patient.dateOfBirth} · MRN: {patient.mrn}
              </div>
            </div>
            {selectedId === patient.id && (
              <span className="text-xs font-medium text-secondary">
                Selected
              </span>
            )}
          </button>
        ))}

      <Button variant="outline" className="mt-3" onClick={onCreateNew}>
        + New Patient
      </Button>
    </div>
  );
}
