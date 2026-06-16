import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { AlertTriangle, Timer } from 'lucide-react';
import { useCallback, useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { refreshAccessToken } from '../../features/identity/api/loginApi';
import { setAccessToken } from '../api/authInterceptor';

interface SessionTimeoutModalProps {
  secondsLeft: number;
  onExtendSession: () => void;
  onLogout: () => void;
  hasUnsavedChanges?: boolean;
}

const TOTAL_COUNTDOWN = 60;

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${String(m)}:${String(s).padStart(2, '0')}`;
}

export function SessionTimeoutModal({
  secondsLeft,
  onExtendSession,
  onLogout,
  hasUnsavedChanges = false,
}: SessionTimeoutModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);
  const extendBtnRef = useRef<HTMLButtonElement>(null);

  const progressPercent = Math.max(0, (secondsLeft / TOTAL_COUNTDOWN) * 100);

  const handleExtendSession = useCallback(async () => {
    const result = await refreshAccessToken();
    if (result.success) {
      setAccessToken(result.accessToken);
      onExtendSession();
    } else {
      onLogout();
    }
  }, [onExtendSession, onLogout]);

  useEffect(() => {
    extendBtnRef.current?.focus();

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') return;
      if (e.key !== 'Tab') return;

      const modal = modalRef.current;
      if (!modal) return;

      const focusable = modal.querySelectorAll<HTMLElement>(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
      );
      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (!first || !last) return;

      if (e.shiftKey) {
        if (document.activeElement === first) {
          e.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, []);

  const announceRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (secondsLeft === 30 || secondsLeft === 10 || secondsLeft === 5) {
      if (announceRef.current) {
        announceRef.current.textContent = `${String(secondsLeft)} seconds remaining before automatic logout.`;
      }
    }
  }, [secondsLeft]);

  const modal = (
    <div
      className="fixed inset-0 z-[500] flex items-center justify-center bg-foreground/50 animate-in fade-in duration-300"
      role="dialog"
      aria-modal="true"
      aria-labelledby="session-timeout-title"
      aria-describedby="session-timeout-body"
    >
      <div
        className="bg-card rounded-lg shadow-[var(--shadow-4)] max-w-[420px] w-[90%] p-8 text-center animate-in zoom-in-95 duration-300"
        ref={modalRef}
      >
        <div className="mb-4" aria-hidden="true">
          <Timer className="h-12 w-12 text-destructive mx-auto" />
        </div>
        <h2
          className="text-[22px] font-bold text-foreground mb-3"
          id="session-timeout-title"
        >
          Session Expiring
        </h2>
        <p
          className="text-sm text-muted-foreground mb-5"
          id="session-timeout-body"
        >
          Your session is about to expire due to inactivity. You will be
          automatically logged out in:
        </p>

        <div
          className="flex items-center justify-center gap-2 mb-5"
          aria-live="polite"
          aria-atomic="true"
        >
          <span className="font-mono text-[32px] font-bold text-destructive">
            {formatTime(secondsLeft)}
          </span>
          <span className="text-[13px] text-muted-foreground">
            {secondsLeft === 1 ? 'second remaining' : 'seconds remaining'}
          </span>
        </div>

        <Progress
          value={progressPercent}
          className="mb-6 h-1.5 [&>div]:bg-destructive"
          aria-label="Session time remaining"
        />

        {hasUnsavedChanges && (
          <div className="flex items-start gap-2 rounded-md bg-[var(--surface-warning)] px-4 py-3 text-sm text-amber-700 mb-6">
            <AlertTriangle className="h-4 w-4 mt-0.5 shrink-0" />
            Unsaved changes will be lost if the session expires.
          </div>
        )}

        <div className="flex flex-col gap-3">
          <Button
            ref={extendBtnRef}
            type="button"
            size="lg"
            className="w-full"
            onClick={() => {
              void handleExtendSession();
            }}
          >
            Extend Session
          </Button>
          <Button
            type="button"
            variant="outline"
            size="lg"
            className="w-full"
            onClick={onLogout}
          >
            Log Out Now
          </Button>
        </div>

        <div
          ref={announceRef}
          className="sr-only"
          aria-live="assertive"
          role="alert"
        />
      </div>
    </div>
  );

  return createPortal(modal, document.body);
}
