import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';
import { AlertCircle, Check, Loader2, X } from 'lucide-react';
import { useCallback, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../../app/hooks';
import { authenticatedFetch } from '../../../shared/api/authInterceptor';
import type { IntakeDraft } from '../clinicalSlice';
import {
  clearIntakeDraft,
  markFieldManuallyEdited,
  setIntakeDraft,
} from '../clinicalSlice';

// ── Severity badge (per wireframe SCR-012) ──

function SeverityBadge({ severity }: { severity: string }) {
  const cls = (() => {
    switch (severity.toLowerCase()) {
      case 'severe':
        return 'bg-[#FDECEC] text-[#D32F2F]';
      case 'moderate':
        return 'bg-[#FFF3E0] text-[#EF6C00]';
      case 'mild':
        return 'bg-[#E6F7F1] text-[#2D9F83]';
      default:
        return 'bg-[#F3F4F6] text-[#374151]';
    }
  })();

  return (
    <span
      className={cn(
        'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium',
        cls,
      )}
    >
      {severity}
    </span>
  );
}

// ── Reusable row ──

function SummaryRow({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex items-baseline justify-between py-2 text-sm">
      <span className="text-[#6B7280] shrink-0">{label}</span>
      <span className="font-medium text-right max-w-[60%]">{children}</span>
    </div>
  );
}

// ── Card wrapper ──

function SummaryCard({
  title,
  onEdit,
  children,
}: {
  title: string;
  onEdit: () => void;
  children: React.ReactNode;
}) {
  return (
    <section className="bg-white border border-[#D1D5DB] rounded-lg p-5 shadow-[0_1px_3px_rgba(0,0,0,0.1)] mb-5">
      <div className="flex items-center justify-between mb-4 pb-3 border-b border-[#F3F4F6]">
        <h2 className="text-base font-semibold text-[#111827]">{title}</h2>
        <button
          type="button"
          className="text-[13px] font-medium text-[#1E6F9F] hover:underline focus-visible:outline-2 focus-visible:outline-[#1E6F9F] focus-visible:rounded-sm"
          onClick={onEdit}
          aria-label={`Edit ${title.toLowerCase()}`}
        >
          Edit
        </button>
      </div>
      {children}
    </section>
  );
}

// ── Inline edit state ──

interface EditState {
  section: string;
  field: string;
}

// ── Component ──

export function IntakeSummaryPage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const intakeDraft = useAppSelector(
    (state) => state.clinical.intakeDraft,
  ) as IntakeDraft | null;

  const [editing, setEditing] = useState<EditState | null>(null);
  const [editValue, setEditValue] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const [draft, setDraft] = useState<IntakeDraft>(
    () =>
      intakeDraft ?? {
        allergies: { name: '', type: '', severity: '', reaction: '' },
        medications: { name: '', dosage: '', notes: '' },
        history: { conditions: [], additionalNotes: '' },
        symptoms: { chiefComplaint: '', duration: '', severity: '' },
      },
  );

  // ── Inline edit handlers (UXR-104) ──

  const startEdit = useCallback(
    (section: string, field: string, value: string) => {
      setEditing({ section, field });
      setEditValue(value);
    },
    [],
  );

  const cancelEdit = useCallback(() => {
    setEditing(null);
    setEditValue('');
  }, []);

  const saveEdit = useCallback(() => {
    if (!editing) return;
    const { section, field } = editing;

    setDraft((prev) => {
      const next = { ...prev };
      if (section === 'allergies') {
        next.allergies = { ...prev.allergies, [field]: editValue };
      } else if (section === 'medications') {
        next.medications = { ...prev.medications, [field]: editValue };
      } else if (section === 'history') {
        if (field === 'additionalNotes') {
          next.history = { ...prev.history, additionalNotes: editValue };
        }
      } else if (section === 'symptoms') {
        next.symptoms = { ...prev.symptoms, [field]: editValue };
      }
      return next;
    });

    dispatch(markFieldManuallyEdited(`${editing.section}.${editing.field}`));
    setEditing(null);
    setEditValue('');
  }, [editing, editValue, dispatch]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        saveEdit();
      } else if (e.key === 'Escape') {
        cancelEdit();
      }
    },
    [saveEdit, cancelEdit],
  );

  // ── Inline edit field ──

  const InlineEdit = () => {
    if (!editing) return null;
    return (
      <div className="flex items-center gap-2 py-2">
        <Input
          value={editValue}
          onChange={(e) => setEditValue(e.target.value)}
          onKeyDown={handleKeyDown}
          className="h-8 max-w-64"
          autoFocus
          aria-label={`Edit ${editing.field}`}
        />
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-8 w-8 p-0"
          onClick={saveEdit}
          aria-label="Save"
        >
          <Check className="size-4" />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-8 w-8 p-0"
          onClick={cancelEdit}
          aria-label="Cancel"
        >
          <X className="size-4" />
        </Button>
      </div>
    );
  };

  // ── Submit ──

  const handleSubmit = useCallback(async () => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const response = await authenticatedFetch('/api/clinical/intake/submit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(draft),
      });

      if (!response.ok) {
        throw new Error(`Server error: ${response.status}`);
      }

      dispatch(clearIntakeDraft());
      void navigate('/dashboard');
    } catch {
      setSubmitError('Failed to submit intake. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  }, [draft, dispatch, navigate]);

  // ── Back to edit ──

  const handleBackToEdit = useCallback(() => {
    dispatch(setIntakeDraft(draft));
    void navigate(-1);
  }, [draft, dispatch, navigate]);

  const isEditingSection = (section: string) => editing?.section === section;

  // ── Render ──

  return (
    <div className="max-w-[800px]">
      {/* Breadcrumb */}
      <nav
        className="flex items-center gap-2 text-sm mb-6"
        aria-label="Breadcrumb"
      >
        <Link to="/dashboard" className="text-[#1E6F9F] hover:underline">
          Dashboard
        </Link>
        <span className="text-[#6B7280]" aria-hidden="true">
          ›
        </span>
        <Link to="/intake" className="text-[#1E6F9F] hover:underline">
          Intake
        </Link>
        <span className="text-[#6B7280]" aria-hidden="true">
          ›
        </span>
        <span className="text-[#6B7280]" aria-current="page">
          Review
        </span>
      </nav>

      {/* Page title */}
      <h1 className="text-[32px] font-bold text-[#111827] mb-2">
        Review Your Intake
      </h1>
      <p className="text-sm text-[#6B7280] mb-8">
        Please review the information below. Click &quot;Edit&quot; to make
        corrections before submitting.
      </p>

      {/* Submit error banner */}
      {submitError && (
        <div
          className="flex items-center gap-2 rounded-lg border border-destructive/50 bg-destructive/10 p-4 mb-5 text-sm text-destructive"
          role="alert"
        >
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>{submitError}</span>
        </div>
      )}

      {/* ── Allergies ── */}
      <SummaryCard
        title="Allergies"
        onEdit={() => startEdit('allergies', 'name', draft.allergies.name)}
      >
        {isEditingSection('allergies') ? (
          <InlineEdit />
        ) : draft.allergies.name ? (
          <SummaryRow label={draft.allergies.name}>
            <SeverityBadge severity={draft.allergies.severity} />
            {draft.allergies.reaction && (
              <span className="ml-1">&mdash; {draft.allergies.reaction}</span>
            )}
          </SummaryRow>
        ) : (
          <p className="text-sm text-[#6B7280] italic">
            No allergies recorded.
          </p>
        )}
      </SummaryCard>

      {/* ── Current Medications ── */}
      <SummaryCard
        title="Current Medications"
        onEdit={() => startEdit('medications', 'name', draft.medications.name)}
      >
        {isEditingSection('medications') ? (
          <InlineEdit />
        ) : draft.medications.name ? (
          <SummaryRow label={draft.medications.name}>
            {[draft.medications.dosage, draft.medications.notes]
              .filter(Boolean)
              .join(' — ') || '—'}
          </SummaryRow>
        ) : (
          <p className="text-sm text-[#6B7280] italic">
            No medications recorded.
          </p>
        )}
      </SummaryCard>

      {/* ── Medical History ── */}
      <SummaryCard
        title="Medical History"
        onEdit={() =>
          startEdit('history', 'additionalNotes', draft.history.additionalNotes)
        }
      >
        {isEditingSection('history') ? (
          <InlineEdit />
        ) : (
          <>
            {draft.history.conditions.length > 0 ? (
              <div className="flex flex-wrap gap-2 mt-1">
                {draft.history.conditions.map((c) => (
                  <span
                    key={c}
                    className="px-3 py-1 rounded bg-[#F3F4F6] text-[13px] text-[#374151]"
                  >
                    {c}
                  </span>
                ))}
              </div>
            ) : (
              <p className="text-sm text-[#6B7280] italic">
                No conditions recorded.
              </p>
            )}
            {draft.history.additionalNotes && (
              <p className="mt-3 text-sm text-[#6B7280]">
                {draft.history.additionalNotes}
              </p>
            )}
          </>
        )}
      </SummaryCard>

      {/* ── Current Symptoms ── */}
      <SummaryCard
        title="Current Symptoms"
        onEdit={() =>
          startEdit('symptoms', 'chiefComplaint', draft.symptoms.chiefComplaint)
        }
      >
        {isEditingSection('symptoms') ? (
          <InlineEdit />
        ) : (
          <>
            <SummaryRow label="Chief Complaint">
              {draft.symptoms.chiefComplaint || '—'}
            </SummaryRow>
            <SummaryRow label="Duration">
              {draft.symptoms.duration || '—'}
            </SummaryRow>
            <SummaryRow label="Severity">
              {draft.symptoms.severity ? `${draft.symptoms.severity}/10` : '—'}
            </SummaryRow>
          </>
        )}
      </SummaryCard>

      {/* ── Action bar ── */}
      <div className="flex items-center gap-3 mt-8">
        <Button
          type="button"
          size="lg"
          disabled={isSubmitting}
          className="h-12 px-6 text-base font-medium"
          onClick={() => void handleSubmit()}
        >
          {isSubmitting ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              Submitting…
            </>
          ) : (
            'Confirm & Submit'
          )}
        </Button>
        <Link
          to="/intake/insurance"
          className="inline-flex items-center justify-center h-12 px-6 text-base font-medium rounded-lg border border-[#1E6F9F] text-[#1E6F9F] bg-transparent hover:bg-[#E8F4FD] transition-colors"
        >
          Pre-Check Insurance
        </Link>
        <Button
          type="button"
          variant="ghost"
          size="lg"
          className="h-12 px-6 text-base font-medium text-[#6B7280] hover:bg-[#F3F4F6]"
          onClick={handleBackToEdit}
        >
          Back to Edit
        </Button>
      </div>
    </div>
  );
}
