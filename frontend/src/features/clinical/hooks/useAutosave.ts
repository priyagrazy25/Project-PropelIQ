import { useCallback, useEffect, useRef, useState } from 'react';
import { authenticatedFetch } from '../../../shared/api/authInterceptor';

const AUTOSAVE_INTERVAL_MS = 30_000;
const STORAGE_KEY_PREFIX = 'intake_autosave_';

export type AutosaveStatus = 'idle' | 'saving' | 'saved' | 'error';

interface UseAutosaveOptions<T> {
  /** Unique key to namespace localStorage backup (e.g. appointmentId or a constant). */
  storageKey: string;
  /** API endpoint for persisting drafts. */
  endpoint: string;
  /** Current form data to persist. */
  data: T;
  /** Whether autosave is active. Disable while the form is pristine or submitting. */
  enabled: boolean;
}

interface UseAutosaveReturn<T> {
  status: AutosaveStatus;
  lastSavedAt: Date | null;
  /** Restore data previously saved to localStorage (returns null if nothing stored). */
  restoreLocal: () => T | null;
  /** Manually trigger a save (e.g. before navigation). */
  saveNow: () => Promise<void>;
  /** Clear the localStorage backup. */
  clearLocal: () => void;
}

export function useAutosave<T>({
  storageKey,
  endpoint,
  data,
  enabled,
}: UseAutosaveOptions<T>): UseAutosaveReturn<T> {
  const [status, setStatus] = useState<AutosaveStatus>('idle');
  const [lastSavedAt, setLastSavedAt] = useState<Date | null>(null);
  const dataRef = useRef(data);
  useEffect(() => {
    dataRef.current = data;
  });

  const fullKey = `${STORAGE_KEY_PREFIX}${storageKey}`;

  const saveToLocal = useCallback(
    (payload: T) => {
      try {
        localStorage.setItem(fullKey, JSON.stringify(payload));
      } catch {
        // Storage full or unavailable — silently skip.
      }
    },
    [fullKey],
  );

  const saveNow = useCallback(async () => {
    const payload = dataRef.current;
    setStatus('saving');

    try {
      const response = await authenticatedFetch(endpoint, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error(`Server responded with ${response.status}`);
      }

      setStatus('saved');
      setLastSavedAt(new Date());
      // Clear localStorage backup on successful network save.
      localStorage.removeItem(fullKey);
    } catch {
      // Network failure — fall back to localStorage.
      saveToLocal(payload);
      setStatus('error');
    }
  }, [endpoint, fullKey, saveToLocal]);

  // 30-second interval autosave.
  useEffect(() => {
    if (!enabled) return;

    const id = setInterval(() => {
      void saveNow();
    }, AUTOSAVE_INTERVAL_MS);

    return () => clearInterval(id);
  }, [enabled, saveNow]);

  // Retry pending localStorage data when coming back online.
  useEffect(() => {
    if (!enabled) return;

    const handleOnline = () => {
      const pending = localStorage.getItem(fullKey);
      if (pending) {
        void saveNow();
      }
    };

    window.addEventListener('online', handleOnline);
    return () => window.removeEventListener('online', handleOnline);
  }, [enabled, fullKey, saveNow]);

  // Reset the "saved" badge after 3 seconds so it doesn't stay forever.
  useEffect(() => {
    if (status !== 'saved') return;
    const id = setTimeout(() => setStatus('idle'), 3_000);
    return () => clearTimeout(id);
  }, [status]);

  const restoreLocal = useCallback((): T | null => {
    try {
      const raw = localStorage.getItem(fullKey);
      if (!raw) return null;
      return JSON.parse(raw) as T;
    } catch {
      return null;
    }
  }, [fullKey]);

  const clearLocal = useCallback(() => {
    localStorage.removeItem(fullKey);
  }, [fullKey]);

  return { status, lastSavedAt, restoreLocal, saveNow, clearLocal };
}
