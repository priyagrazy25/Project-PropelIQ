import { useCallback, useEffect, useRef, useState } from 'react';
import { postSessionMessage, subscribeSessionChannel } from '../utils/broadcastChannel';

const INACTIVITY_TIMEOUT_MS = 15 * 60 * 1000; // 15 minutes
const COUNTDOWN_SECONDS = 60;
const ACTIVITY_EVENTS: Array<keyof DocumentEventMap> = [
  'mousedown',
  'keydown',
  'scroll',
  'touchstart',
];

export interface InactivityTimerState {
  showModal: boolean;
  secondsLeft: number;
  extendSession: () => void;
  logoutNow: () => void;
}

export function useInactivityTimer(
  isAuthenticated: boolean,
  onLogout: () => void,
): InactivityTimerState {
  const [showModal, setShowModal] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(COUNTDOWN_SECONDS);

  const inactivityTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const countdownInterval = useRef<ReturnType<typeof setInterval> | null>(null);
  const secondsLeftRef = useRef(COUNTDOWN_SECONDS);
  const showModalRef = useRef(false);
  const onLogoutRef = useRef(onLogout);

  useEffect(() => {
    onLogoutRef.current = onLogout;
  }, [onLogout]);

  const clearAllTimers = useCallback(() => {
    if (inactivityTimer.current) {
      clearTimeout(inactivityTimer.current);
      inactivityTimer.current = null;
    }
    if (countdownInterval.current) {
      clearInterval(countdownInterval.current);
      countdownInterval.current = null;
    }
  }, []);

  const startCountdown = useCallback(() => {
    secondsLeftRef.current = COUNTDOWN_SECONDS;
    showModalRef.current = true;
    setSecondsLeft(COUNTDOWN_SECONDS);
    setShowModal(true);

    countdownInterval.current = setInterval(() => {
      secondsLeftRef.current -= 1;
      setSecondsLeft(secondsLeftRef.current);

      if (secondsLeftRef.current <= 0) {
        clearAllTimers();
        showModalRef.current = false;
        setShowModal(false);
        postSessionMessage({ type: 'LOGOUT' });
        onLogoutRef.current();
      }
    }, 1000);
  }, [clearAllTimers]);

  /** Ref-only timer restart — safe to call from effects (no synchronous setState) */
  const scheduleInactivityTimer = useCallback(() => {
    clearAllTimers();
    inactivityTimer.current = setTimeout(() => {
      startCountdown();
    }, INACTIVITY_TIMEOUT_MS);
  }, [clearAllTimers, startCountdown]);

  /** Full reset including state — call from event handlers / callbacks only */
  const resetInactivityTimer = useCallback(() => {
    clearAllTimers();
    showModalRef.current = false;
    setShowModal(false);
    setSecondsLeft(COUNTDOWN_SECONDS);
    secondsLeftRef.current = COUNTDOWN_SECONDS;
    scheduleInactivityTimer();
  }, [clearAllTimers, scheduleInactivityTimer]);

  const extendSession = useCallback(() => {
    postSessionMessage({ type: 'SESSION_EXTENDED' });
    resetInactivityTimer();
  }, [resetInactivityTimer]);

  const logoutNow = useCallback(() => {
    clearAllTimers();
    showModalRef.current = false;
    setShowModal(false);
    postSessionMessage({ type: 'LOGOUT' });
    onLogoutRef.current();
  }, [clearAllTimers]);

  // Set up activity listeners and initial timer
  useEffect(() => {
    if (!isAuthenticated) {
      clearAllTimers();
      return;
    }

    const onActivity = () => {
      if (!showModalRef.current) {
        resetInactivityTimer();
        postSessionMessage({ type: 'ACTIVITY_DETECTED' });
      }
    };

    for (const event of ACTIVITY_EVENTS) {
      document.addEventListener(event, onActivity, { passive: true });
    }

    // scheduleInactivityTimer only touches refs — no synchronous setState
    scheduleInactivityTimer();

    return () => {
      for (const event of ACTIVITY_EVENTS) {
        document.removeEventListener(event, onActivity);
      }
      clearAllTimers();
    };
  }, [isAuthenticated, clearAllTimers, resetInactivityTimer, scheduleInactivityTimer]);

  // Cross-tab synchronization
  useEffect(() => {
    if (!isAuthenticated) return;

    const unsubscribe = subscribeSessionChannel((message) => {
      switch (message.type) {
        case 'ACTIVITY_DETECTED':
        case 'SESSION_EXTENDED':
          resetInactivityTimer();
          break;
        case 'LOGOUT':
          clearAllTimers();
          showModalRef.current = false;
          setShowModal(false);
          onLogoutRef.current();
          break;
      }
    });

    return unsubscribe;
  }, [isAuthenticated, resetInactivityTimer, clearAllTimers]);

  return { showModal: isAuthenticated && showModal, secondsLeft, extendSession, logoutNow };
}
