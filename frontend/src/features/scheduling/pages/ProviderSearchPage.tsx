import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { useVirtualizer } from '@tanstack/react-virtual';
import { ChevronLeft, ChevronRight, SearchIcon, WifiOff } from 'lucide-react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import {
  searchProviders,
  type ProviderResult,
  type ProviderSlot,
  type SwapExecutedEvent,
  type WaitlistEntry,
} from '../api/schedulingApi';
import { ProviderCard } from '../components/ProviderCard';
import {
  SearchFilters,
  type SearchFilterValues,
} from '../components/SearchFilters';
import { WaitlistJoinDialog } from '../components/WaitlistJoinDialog';
import type { SlotUpdate } from '../hooks/useSignalRSlots';
import { useSignalRSlots } from '../hooks/useSignalRSlots';

const PAGE_SIZE = 20;
const VIRTUAL_THRESHOLD = 100;
const GRID_COLUMNS = 3;
const ESTIMATED_ROW_HEIGHT = 340;

const INITIAL_FILTERS: SearchFilterValues = {
  name: '',
  specialty: '',
  location: '',
  date: '',
};

export function ProviderSearchPage() {
  const [filters, setFilters] = useState<SearchFilterValues>(INITIAL_FILTERS);
  const [providers, setProviders] = useState<ProviderResult[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState('earliest');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  const [waitlistProvider, setWaitlistProvider] =
    useState<ProviderResult | null>(null);
  const navigate = useNavigate();

  const providerIds = useMemo(() => providers.map((p) => p.id), [providers]);

  const handleSlotUpdate = useCallback((update: SlotUpdate) => {
    setProviders((prev) =>
      prev.map((provider) => {
        if (provider.id !== update.providerId) return provider;
        return {
          ...provider,
          availableSlots: provider.availableSlots.map((slot) =>
            slot.id === update.slotId
              ? { ...slot, isAvailable: update.isAvailable }
              : slot,
          ),
        };
      }),
    );
  }, []);

  const executeSearch = useCallback(
    (searchPage: number) => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setLoading(true);
      setError(null);
      setHasSearched(true);

      searchProviders({
        name: filters.name || undefined,
        specialty: filters.specialty || undefined,
        location: filters.location || undefined,
        date: filters.date || undefined,
        page: searchPage,
        pageSize: PAGE_SIZE,
        sortBy,
      })
        .then((result) => {
          if (controller.signal.aborted) return;
          if (result.success) {
            setProviders(result.data.providers);
            setTotalCount(result.data.totalCount);
            setPage(result.data.page);
          } else {
            setError(result.error.message);
          }
          setLoading(false);
        })
        .catch(() => {
          if (!controller.signal.aborted) {
            setError('An unexpected error occurred.');
            setLoading(false);
          }
        });
    },
    [filters, sortBy],
  );

  // Ref for reconnect callback to access latest executeSearch/page
  const executeSearchRef = useRef(executeSearch);
  useEffect(() => {
    executeSearchRef.current = executeSearch;
  }, [executeSearch]);

  // Auto-load on mount — show earliest available slots without requiring a manual search
  useEffect(() => {
    executeSearchRef.current(1);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const pageRef = useRef(page);
  useEffect(() => {
    pageRef.current = page;
  }, [page]);

  const handleReconnected = useCallback(() => {
    executeSearchRef.current(pageRef.current);
  }, []);

  const handleSwapExecuted = useCallback((event: SwapExecutedEvent) => {
    toast.success(
      `Your appointment with ${event.providerName} has been swapped to your preferred slot!`,
    );
  }, []);

  const { connectionState } = useSignalRSlots({
    providerIds,
    onSlotUpdate: handleSlotUpdate,
    onSwapExecuted: handleSwapExecuted,
    onReconnected: handleReconnected,
    enabled: providers.length > 0,
  });

  const handleSearch = useCallback(() => {
    executeSearch(1);
  }, [executeSearch]);

  const handlePageChange = useCallback(
    (newPage: number) => {
      executeSearch(newPage);
    },
    [executeSearch],
  );

  const handleResetFilters = useCallback(() => {
    setFilters(INITIAL_FILTERS);
    setProviders([]);
    setTotalCount(0);
    setHasSearched(false);
    setError(null);
  }, []);

  // Booking flow handler — navigate to full confirmation page (SCR-005)
  const handleBookAppointment = useCallback(
    (provider: ProviderResult, slot: ProviderSlot) => {
      void navigate('/booking/confirm', { state: { provider, slot } });
    },
    [navigate],
  );

  const handleViewProfile = useCallback(
    (provider: ProviderResult) => {
      void navigate(`/providers/${provider.id}`, { state: { provider } });
    },
    [navigate],
  );

  const handleJoinWaitlist = useCallback((provider: ProviderResult) => {
    setWaitlistProvider(provider);
  }, []);

  const handleWaitlistJoined = useCallback((_entry: WaitlistEntry) => {
    setWaitlistProvider(null);
  }, []);

  const handleCloseWaitlistDialog = useCallback(() => {
    setWaitlistProvider(null);
  }, []);

  // Re-fetch when sort changes after initial search — handled via handleSortChange
  const handleSortChange = useCallback(
    (newSort: string) => {
      setSortBy(newSort);
      if (hasSearched) {
        // executeSearch uses sortBy from closure, but we need the new value.
        // Since sortBy is in deps of executeSearch, we trigger search on next render via ref.
        // Instead, inline the search here with the new sort.
        abortRef.current?.abort();
        const controller = new AbortController();
        abortRef.current = controller;
        setLoading(true);
        setError(null);
        searchProviders({
          name: filters.name || undefined,
          specialty: filters.specialty || undefined,
          location: filters.location || undefined,
          date: filters.date || undefined,
          page: 1,
          pageSize: PAGE_SIZE,
          sortBy: newSort,
        })
          .then((result) => {
            if (controller.signal.aborted) return;
            if (result.success) {
              setProviders(result.data.providers);
              setTotalCount(result.data.totalCount);
              setPage(result.data.page);
            } else {
              setError(result.error.message);
            }
            setLoading(false);
          })
          .catch(() => {
            if (!controller.signal.aborted) {
              setError('An unexpected error occurred.');
              setLoading(false);
            }
          });
      }
    },
    [hasSearched, filters],
  );

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  // Virtual scrolling for 100+ results (AC-5)
  const useVirtual = totalCount > VIRTUAL_THRESHOLD;
  const virtualRowCount = Math.ceil(providers.length / GRID_COLUMNS);
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const virtualizer = useVirtualizer({
    count: virtualRowCount,
    getScrollElement: () => scrollContainerRef.current,
    estimateSize: () => ESTIMATED_ROW_HEIGHT,
    overscan: 2,
    enabled: useVirtual,
  });

  return (
    <main className="space-y-6" role="main">
      <nav
        className="flex items-center gap-1 text-sm text-muted-foreground"
        aria-label="Breadcrumb"
      >
        <a href="/dashboard" className="hover:text-foreground">
          Dashboard
        </a>
        <span aria-hidden="true">›</span>
        <span aria-current="page" className="text-foreground">
          Book Appointment
        </span>
      </nav>

      <h1 className="text-2xl font-bold text-foreground">Find a Provider</h1>

      <SearchFilters
        filters={filters}
        onChange={setFilters}
        onSearch={handleSearch}
        disabled={loading}
      />

      {connectionState === 'reconnecting' && (
        <div
          className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
          role="status"
          aria-live="polite"
        >
          <WifiOff className="h-4 w-4 shrink-0" />
          Reconnecting to real-time updates…
        </div>
      )}

      {error && (
        <div
          className="flex items-center justify-between rounded-lg border border-destructive/50 bg-destructive/10 p-4"
          role="alert"
        >
          <p className="text-sm text-destructive">{error}</p>
          <Button variant="outline" size="sm" onClick={handleSearch}>
            Retry
          </Button>
        </div>
      )}

      {loading && (
        <div
          className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
          aria-busy="true"
          aria-label="Loading providers"
        >
          {Array.from({ length: 6 }, (_, i) => (
            <div
              key={i}
              className="rounded-lg border border-border bg-card p-5 shadow-sm"
              aria-hidden="true"
            >
              <div className="flex gap-3 mb-4">
                <Skeleton className="h-12 w-12 rounded-full shrink-0" />
                <div className="space-y-2 flex-1">
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/2" />
                  <Skeleton className="h-3 w-1/3" />
                </div>
              </div>
              <Skeleton className="h-5 w-28 mb-3" />
              <div className="space-y-2 mt-3">
                <Skeleton className="h-3 w-1/4" />
                <div className="flex gap-2">
                  <Skeleton className="h-8 w-16 rounded" />
                  <Skeleton className="h-8 w-16 rounded" />
                  <Skeleton className="h-8 w-16 rounded" />
                </div>
              </div>
              <div className="flex gap-2 mt-4">
                <Skeleton className="h-8 w-32 rounded" />
                <Skeleton className="h-8 w-24 rounded" />
              </div>
            </div>
          ))}
        </div>
      )}

      {!loading && hasSearched && providers.length === 0 && !error && (
        <div
          className="flex flex-col items-center py-16 text-center"
          role="status"
        >
          <SearchIcon className="h-12 w-12 text-muted-foreground mb-3" />
          <h2 className="text-lg font-semibold mb-1">
            No providers match your criteria
          </h2>
          <p className="text-sm text-muted-foreground mb-4">
            Try adjusting your filters or search for a different specialty.
          </p>
          <Button onClick={handleResetFilters}>Reset Filters</Button>
        </div>
      )}

      {!loading && providers.length > 0 && (
        <>
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">
              Showing {providers.length} of {totalCount} provider
              {totalCount !== 1 ? 's' : ''}
            </span>
            <select
              className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              aria-label="Sort results"
              value={sortBy}
              onChange={(e) => {
                handleSortChange(e.target.value);
              }}
            >
              <option value="earliest">Earliest Available</option>
              <option value="name">Name A-Z</option>
              <option value="rating">Highest Rated</option>
            </select>
          </div>

          {useVirtual ? (
            <div
              ref={scrollContainerRef}
              className="h-[600px] overflow-auto rounded-lg"
              role="list"
              aria-label="Provider results"
            >
              <div
                className="relative w-full"
                style={{ height: `${virtualizer.getTotalSize()}px` }}
              >
                {virtualizer.getVirtualItems().map((virtualRow) => {
                  const rowStart = virtualRow.index * GRID_COLUMNS;
                  const rowProviders = providers.slice(
                    rowStart,
                    rowStart + GRID_COLUMNS,
                  );
                  return (
                    <div
                      key={virtualRow.key}
                      className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 absolute left-0 w-full"
                      style={{
                        top: 0,
                        transform: `translateY(${virtualRow.start}px)`,
                      }}
                    >
                      {rowProviders.map((provider) => (
                        <ProviderCard
                          key={provider.id}
                          provider={provider}
                          onBookAppointment={handleBookAppointment}
                          onJoinWaitlist={handleJoinWaitlist}
                          onViewProfile={handleViewProfile}
                        />
                      ))}
                    </div>
                  );
                })}
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {providers.map((provider) => (
                <ProviderCard
                  key={provider.id}
                  provider={provider}
                  onBookAppointment={handleBookAppointment}
                  onJoinWaitlist={handleJoinWaitlist}
                  onViewProfile={handleViewProfile}
                />
              ))}
            </div>
          )}

          {totalPages > 1 && (
            <nav
              className="flex items-center justify-center gap-1"
              aria-label="Pagination"
            >
              <Button
                variant="ghost"
                size="icon"
                disabled={page <= 1}
                aria-label="Previous page"
                onClick={() => {
                  handlePageChange(page - 1);
                }}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              {Array.from({ length: totalPages }, (_, i) => i + 1)
                .filter(
                  (p) => p === 1 || p === totalPages || Math.abs(p - page) <= 2,
                )
                .map((p, idx, arr) => {
                  const prev = arr[idx - 1];
                  const showEllipsis = prev !== undefined && p - prev > 1;
                  return (
                    <span key={p} className="flex items-center">
                      {showEllipsis && (
                        <span className="px-1 text-muted-foreground">…</span>
                      )}
                      <Button
                        variant={page === p ? 'default' : 'ghost'}
                        size="icon"
                        aria-label={`Page ${p}`}
                        aria-current={page === p ? 'page' : undefined}
                        onClick={() => {
                          handlePageChange(p);
                        }}
                      >
                        {p}
                      </Button>
                    </span>
                  );
                })}
              <Button
                variant="ghost"
                size="icon"
                disabled={page >= totalPages}
                aria-label="Next page"
                onClick={() => {
                  handlePageChange(page + 1);
                }}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </nav>
          )}
        </>
      )}

      {waitlistProvider && (
        <WaitlistJoinDialog
          provider={waitlistProvider}
          onJoined={handleWaitlistJoined}
          onClose={handleCloseWaitlistDialog}
        />
      )}
    </main>
  );
}
