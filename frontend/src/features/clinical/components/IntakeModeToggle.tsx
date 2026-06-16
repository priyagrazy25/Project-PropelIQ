import { cn } from '@/lib/utils';
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../../app/hooks';
import type { IntakeDraft, IntakeMode } from '../clinicalSlice';
import { setIntakeDraft, setIntakeMode } from '../clinicalSlice';

export interface IntakeModeToggleProps {
  /** Current form data to snapshot into Redux on mode switch (AC-1, AC-2). */
  currentData: IntakeDraft | null;
}

/**
 * Persistent toggle shown on both AI and Manual intake pages (UXR-103).
 * Dispatches shared Redux state so cross-mode data is preserved.
 */
export function IntakeModeToggle({ currentData }: IntakeModeToggleProps) {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const mode = useAppSelector((state) => state.clinical.intakeMode);
  const aiAvailable = useAppSelector((state) => state.clinical.aiAvailable);

  const isAi = mode === 'ai';

  const switchMode = useCallback(
    (target: IntakeMode) => {
      if (target === mode) return;
      // Snapshot current data into shared Redux state before navigating.
      if (currentData) {
        dispatch(setIntakeDraft(currentData));
      }
      dispatch(setIntakeMode(target));
      void navigate(target === 'ai' ? '/intake/ai' : '/intake/manual');
    },
    [mode, currentData, dispatch, navigate],
  );

  return (
    <div className="flex items-center gap-3">
      <span
        className={cn(
          'text-sm font-medium',
          isAi ? 'text-primary' : 'text-muted-foreground',
        )}
      >
        AI Assisted
      </span>

      <button
        type="button"
        role="switch"
        aria-checked={isAi}
        aria-label="Toggle between AI and manual intake mode"
        disabled={!aiAvailable && !isAi}
        className={cn(
          'relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-none transition-colors duration-150',
          isAi ? 'bg-primary' : 'bg-border',
          !aiAvailable && !isAi && 'cursor-not-allowed opacity-40',
        )}
        onClick={() => switchMode(isAi ? 'manual' : 'ai')}
      >
        <span
          className={cn(
            'pointer-events-none block size-5 rounded-full bg-white shadow-sm transition-transform duration-150',
            isAi ? 'translate-x-5.5' : 'translate-x-0.5',
          )}
        />
      </button>

      <span
        className={cn(
          'text-sm font-medium',
          !isAi ? 'text-primary' : 'text-muted-foreground',
        )}
      >
        Manual
      </span>
    </div>
  );
}
