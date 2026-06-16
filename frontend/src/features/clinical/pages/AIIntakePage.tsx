import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../../app/hooks';
import type { ExtractedField, IntakeMessage } from '../api/intakeApi';
import { checkAiAvailability, sendIntakeMessage } from '../api/intakeApi';
import type { IntakeDraft } from '../clinicalSlice';
import {
  incrementLowConfidence,
  resetLowConfidence,
  setAiAvailable,
  setIntakeDraft,
  setIntakeMode,
} from '../clinicalSlice';
import { AIUnavailableBanner } from '../components/AIUnavailableBanner';
import { ChatBubble } from '../components/ChatBubble';
import { IntakeModeToggle } from '../components/IntakeModeToggle';
import { TypingIndicator } from '../components/TypingIndicator';

const LOW_CONFIDENCE_THRESHOLD = 0.5;
const MAX_LOW_CONFIDENCE_STREAK = 3;

/** Convert extracted fields to IntakeDraft shape for cross-mode persistence. */
function fieldsToDraft(fields: ExtractedField[]): IntakeDraft {
  const draft: IntakeDraft = {
    allergies: { name: '', type: '', severity: '', reaction: '' },
    medications: { name: '', dosage: '', notes: '' },
    history: { conditions: [], additionalNotes: '' },
    symptoms: { chiefComplaint: '', duration: '', severity: '' },
  };

  for (const f of fields) {
    switch (f.category) {
      case 'allergy':
        if (f.key.includes('name')) draft.allergies.name = f.value;
        else if (f.key.includes('type')) draft.allergies.type = f.value;
        else if (f.key.includes('severity')) draft.allergies.severity = f.value;
        else if (f.key.includes('reaction')) draft.allergies.reaction = f.value;
        else draft.allergies.name = f.value;
        break;
      case 'medication':
        if (f.key.includes('name')) draft.medications.name = f.value;
        else if (f.key.includes('dosage')) draft.medications.dosage = f.value;
        else if (f.key.includes('notes')) draft.medications.notes = f.value;
        else draft.medications.name = f.value;
        break;
      case 'history':
        if (f.key.includes('conditions'))
          draft.history.conditions = f.value
            .split(',')
            .map((c) => c.trim())
            .filter(Boolean);
        else draft.history.additionalNotes = f.value;
        break;
      case 'symptom':
        if (f.key.includes('complaint'))
          draft.symptoms.chiefComplaint = f.value;
        else if (f.key.includes('duration')) draft.symptoms.duration = f.value;
        else if (f.key.includes('severity')) draft.symptoms.severity = f.value;
        else draft.symptoms.chiefComplaint = f.value;
        break;
      default:
        break;
    }
  }
  return draft;
}

export function AIIntakePage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const lowConfidenceCount = useAppSelector(
    (state) => state.clinical.lowConfidenceCount,
  );
  const chatEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  /** Abort controller for in-flight AI requests — cancelled on mode switch (edge case). */
  const sendAbortRef = useRef<AbortController | null>(null);

  const [messages, setMessages] = useState<IntakeMessage[]>([
    {
      id: 'system-1',
      role: 'system',
      content:
        'AI-assisted intake started. Your data is processed locally and never leaves the platform.',
      timestamp: 'now',
    },
  ]);
  const [inputValue, setInputValue] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [extractedFields, setExtractedFields] = useState<ExtractedField[]>([]);
  const [showFallbackPrompt, setShowFallbackPrompt] = useState(false);
  const [aiUnavailable, setAiUnavailableLocal] = useState(false);

  /** Mark AI unavailable both locally and in Redux (AC-4). */
  const markAiUnavailable = useCallback(() => {
    setAiUnavailableLocal(true);
    dispatch(setAiAvailable(false));
  }, [dispatch]);

  /** Auto-switch to manual on AI unavailability with notification (AC-4). */
  const autoSwitchToManual = useCallback(() => {
    // Edge case: Abort any in-flight AI response on mode switch.
    sendAbortRef.current?.abort();
    setIsTyping(false);
    // Snapshot current AI-parsed data into Redux before switching.
    if (extractedFields.length > 0) {
      dispatch(setIntakeDraft(fieldsToDraft(extractedFields)));
    }
    dispatch(setIntakeMode('manual'));
    void navigate('/intake/manual');
  }, [extractedFields, dispatch, navigate]);

  useEffect(() => {
    dispatch(setIntakeMode('ai'));
  }, [dispatch]);

  useEffect(() => {
    const controller = new AbortController();
    let cancelled = false;
    checkAiAvailability(controller.signal).then((available) => {
      if (cancelled) return;
      if (available) {
        dispatch(setAiAvailable(true));
      } else {
        markAiUnavailable();
        // Show the unavailable banner (UXR-605) — user can switch manually.
      }
    });
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [markAiUnavailable, dispatch]);

  // Abort in-flight AI request on unmount (e.g. mode switch via toggle).
  useEffect(() => {
    return () => {
      sendAbortRef.current?.abort();
    };
  }, []);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isTyping]);

  const formatTimestamp = () =>
    new Date().toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
    });

  const handleSend = useCallback(async () => {
    const text = inputValue.trim();
    if (!text || isTyping) return;

    const userMessage: IntakeMessage = {
      id: `user-${Date.now()}`,
      role: 'user',
      content: text,
      timestamp: formatTimestamp(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInputValue('');
    setIsTyping(true);

    // Create abort controller for this request (edge case: mode switch during response).
    const controller = new AbortController();
    sendAbortRef.current = controller;

    const result = await sendIntakeMessage(
      {
        message: text,
        conversationId: conversationId ?? undefined,
      },
      controller.signal,
    );

    // If aborted (mode switch happened), don't process the result.
    if (controller.signal.aborted) return;

    setIsTyping(false);

    if (!result.success) {
      if (result.error.status === 0 || result.error.status >= 500) {
        markAiUnavailable();
        autoSwitchToManual();
        return;
      }
      const errorMessage: IntakeMessage = {
        id: `ai-err-${Date.now()}`,
        role: 'ai',
        content:
          'Sorry, I encountered an issue processing your response. Could you try again?',
        timestamp: formatTimestamp(),
      };
      setMessages((prev) => [...prev, errorMessage]);
      return;
    }

    const { data } = result;
    if (!conversationId) setConversationId(data.conversationId);

    setMessages((prev) => [...prev, data.reply]);

    if (data.extractedFields.length > 0) {
      setExtractedFields((prev) => {
        const updated = [...prev];
        for (const field of data.extractedFields) {
          const idx = updated.findIndex((f) => f.key === field.key);
          if (idx >= 0) {
            updated[idx] = field;
          } else {
            updated.push(field);
          }
        }
        return updated;
      });

      const hasLowConfidence = data.extractedFields.some(
        (f) => f.confidence < LOW_CONFIDENCE_THRESHOLD,
      );

      if (hasLowConfidence) {
        dispatch(incrementLowConfidence());
        // AC-5 / AIR-008: 3 low-confidence → auto fallback to manual.
        if (lowConfidenceCount + 1 >= MAX_LOW_CONFIDENCE_STREAK) {
          setShowFallbackPrompt(true);
        }
      } else {
        dispatch(resetLowConfidence());
      }
    }

    if (data.isComplete) {
      // Navigate to summary page with data in Redux (AC-3).
      dispatch(
        setIntakeDraft(
          fieldsToDraft([...extractedFields, ...data.extractedFields]),
        ),
      );
      void navigate('/intake/summary');
    }
  }, [
    inputValue,
    isTyping,
    conversationId,
    dispatch,
    lowConfidenceCount,
    markAiUnavailable,
    autoSwitchToManual,
    extractedFields,
    navigate,
  ]);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      void handleSend();
    }
  };

  /** Build the current draft from extracted fields for the toggle snapshot. */
  const currentDraft =
    extractedFields.length > 0 ? fieldsToDraft(extractedFields) : null;

  return (
    <div className="flex h-full flex-col">
      {/* Mode bar */}
      <div className="flex items-center justify-between border-b bg-card px-6 py-3">
        <h1 className="text-xl font-semibold">Patient Intake</h1>
        <IntakeModeToggle currentData={currentDraft} />
      </div>

      {/* AI unavailable banner */}
      {aiUnavailable && <AIUnavailableBanner />}

      {/* Fallback prompt (AC-5: 3 low-confidence → offer manual switch) */}
      {showFallbackPrompt && !aiUnavailable && (
        <div
          role="alert"
          className="flex items-center gap-2 bg-(--surface-warning) px-6 py-3 text-sm text-amber-700"
        >
          <span>
            The AI is having difficulty understanding your responses. Would you
            like to switch to the manual form?
          </span>
          <Button
            variant="outline"
            size="sm"
            className="ml-auto shrink-0"
            onClick={() => {
              if (currentDraft) dispatch(setIntakeDraft(currentDraft));
              dispatch(setIntakeMode('manual'));
              void navigate('/intake/manual');
            }}
          >
            Switch to Manual
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowFallbackPrompt(false)}
          >
            Continue
          </Button>
        </div>
      )}

      {/* Chat area */}
      <div
        role="log"
        aria-label="Intake conversation"
        aria-live="polite"
        className="flex flex-1 flex-col gap-4 overflow-y-auto p-6"
      >
        {messages.map((msg) => (
          <ChatBubble
            key={msg.id}
            role={msg.role}
            content={msg.content}
            timestamp={msg.timestamp}
          />
        ))}
        {isTyping && <TypingIndicator />}
        <div ref={chatEndRef} />
      </div>

      {/* Input bar */}
      <div className="flex gap-3 border-t bg-card px-6 py-4">
        <Input
          ref={inputRef}
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type your response..."
          disabled={aiUnavailable}
          aria-label="Type your response"
          className="h-11 flex-1 rounded-xl"
        />
        <Button
          onClick={() => void handleSend()}
          disabled={!inputValue.trim() || isTyping || aiUnavailable}
          className="h-11 rounded-xl px-5"
          aria-label="Send message"
        >
          Send
        </Button>
      </div>
    </div>
  );
}
