import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import type { RefObject } from 'react';
import { useEffect } from 'react';
import { useBlocker } from 'react-router-dom';

interface NavigationGuardProps {
  /** Block navigation when true (i.e. form has unsaved changes). */
  when: boolean;
  /** Optional ref — when current value is true, skip blocking (for intentional navigations). */
  skipRef?: RefObject<boolean>;
  message?: string;
}

export function NavigationGuard({
  when,
  skipRef,
  message = 'You have unsaved changes. Are you sure you want to leave? Your progress will be lost.',
}: NavigationGuardProps) {
  const blocker = useBlocker(() => {
    if (skipRef?.current) return false;
    return when;
  });

  // Handle browser back / tab close via the native beforeunload event.
  useEffect(() => {
    if (!when) return;

    const handler = (e: BeforeUnloadEvent) => {
      e.preventDefault();
    };

    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [when]);

  if (blocker.state !== 'blocked') return null;

  return (
    <AlertDialog open>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Leave this page?</AlertDialogTitle>
          <AlertDialogDescription>{message}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel onClick={() => blocker.reset()}>
            Stay
          </AlertDialogCancel>
          <AlertDialogAction onClick={() => blocker.proceed()}>
            Leave
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
