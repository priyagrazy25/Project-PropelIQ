import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { AlertCircle, AlertTriangle, Loader2 } from 'lucide-react';
import {
  type FormEvent,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../../app/hooks';
import { setIntakeDraft, setIntakeMode } from '../clinicalSlice';
import { AutosaveIndicator } from '../components/AutosaveIndicator';
import { IntakeFormSection } from '../components/IntakeFormSection';
import { IntakeModeToggle } from '../components/IntakeModeToggle';
import { NavigationGuard } from '../components/NavigationGuard';
import { useAutosave } from '../hooks/useAutosave';

// ── Form data shape ──

interface AllergyEntry {
  name: string;
  type: string;
  severity: string;
  reaction: string;
}

interface MedicationEntry {
  name: string;
  dosage: string;
  notes: string;
}

interface IntakeFormData {
  allergies: AllergyEntry;
  medications: MedicationEntry;
  history: {
    conditions: string[];
    additionalNotes: string;
  };
  symptoms: {
    chiefComplaint: string;
    duration: string;
    severity: string;
  };
}

const EMPTY_FORM: IntakeFormData = {
  allergies: { name: '', type: '', severity: '', reaction: '' },
  medications: { name: '', dosage: '', notes: '' },
  history: { conditions: [], additionalNotes: '' },
  symptoms: { chiefComplaint: '', duration: '', severity: '' },
};

const CONDITIONS = [
  'Diabetes',
  'Hypertension',
  'Heart Disease',
  'Asthma',
  'Cancer',
  'None of the above',
] as const;

const ALLERGY_TYPES = ['Medication', 'Food', 'Environmental'] as const;
const SEVERITY_OPTIONS = ['Mild', 'Moderate', 'Severe'] as const;
const DURATION_OPTIONS = [
  'Less than 1 week',
  '1-4 weeks',
  '1-3 months',
  'More than 3 months',
] as const;

// ── Validation ──

interface FormErrors {
  allergyName?: string;
  allergySeverity?: string;
  medicationName?: string;
  chiefComplaint?: string;
  symptomSeverity?: string;
}

function validate(data: IntakeFormData): FormErrors {
  const errors: FormErrors = {};
  if (!data.allergies.name.trim())
    errors.allergyName = 'Allergy name is required.';
  if (!data.allergies.severity)
    errors.allergySeverity = 'Severity is required.';
  if (!data.medications.name.trim())
    errors.medicationName = 'Medication name is required.';
  if (!data.symptoms.chiefComplaint.trim())
    errors.chiefComplaint = 'Chief complaint is required.';
  if (
    data.symptoms.severity &&
    (Number(data.symptoms.severity) < 1 ||
      Number(data.symptoms.severity) > 10 ||
      !Number.isInteger(Number(data.symptoms.severity)))
  ) {
    errors.symptomSeverity =
      'Severity must be a whole number between 1 and 10.';
  }
  return errors;
}

function isDirty(data: IntakeFormData): boolean {
  return JSON.stringify(data) !== JSON.stringify(EMPTY_FORM);
}

// ── Component ──

const STORAGE_KEY_PREFIX = 'intake_autosave_';

function getInitialForm(
  intakeDraft: IntakeFormData | null,
  userId: string | null,
): IntakeFormData {
  // Priority 1: Pre-fill from Redux (AI mode switch per UXR-103).
  if (intakeDraft) return intakeDraft;

  // Priority 2: Restore from localStorage backup.
  const key = `${STORAGE_KEY_PREFIX}${userId ?? 'anonymous'}`;
  try {
    const raw = localStorage.getItem(key);
    if (raw) {
      const parsed = JSON.parse(raw) as IntakeFormData;
      if (parsed && typeof parsed === 'object') return parsed;
    }
  } catch {
    // Corrupt data — fall through.
  }

  return EMPTY_FORM;
}

// ── Component ──

export function ManualIntakePage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const userId = useAppSelector((state) => state.identity.userId);
  const intakeDraft = useAppSelector(
    (state) => state.clinical.intakeDraft,
  ) as IntakeFormData | null;

  const [form, setForm] = useState<IntakeFormData>(() =>
    getInitialForm(intakeDraft, userId),
  );
  const [errors, setErrors] = useState<FormErrors>({});
  const [touched, setTouched] = useState<
    Partial<Record<keyof FormErrors, boolean>>
  >({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isNavigatingAwayRef = useRef(false);
  const isLoading = false;
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Set mode in Redux on mount (AC-2).
  useEffect(() => {
    dispatch(setIntakeMode('manual'));
  }, [dispatch]);

  const storageKey = userId ?? 'anonymous';
  const formDirty = isDirty(form);

  const { status, lastSavedAt, saveNow, clearLocal } = useAutosave({
    storageKey,
    endpoint: '/api/clinical/intake/draft',
    data: form,
    enabled: formDirty && !isSubmitting && !isLoading,
  });

  // ── Field helpers ──

  const setAllergyField = useCallback(
    (field: keyof AllergyEntry, value: string) => {
      setForm((prev) => ({
        ...prev,
        allergies: { ...prev.allergies, [field]: value },
      }));
    },
    [],
  );

  const setMedicationField = useCallback(
    (field: keyof MedicationEntry, value: string) => {
      setForm((prev) => ({
        ...prev,
        medications: { ...prev.medications, [field]: value },
      }));
    },
    [],
  );

  const setSymptomField = useCallback(
    (field: keyof IntakeFormData['symptoms'], value: string) => {
      setForm((prev) => ({
        ...prev,
        symptoms: { ...prev.symptoms, [field]: value },
      }));
    },
    [],
  );

  const toggleCondition = useCallback((condition: string) => {
    setForm((prev) => {
      const current = prev.history.conditions;
      const next = current.includes(condition)
        ? current.filter((c) => c !== condition)
        : [...current, condition];
      return { ...prev, history: { ...prev.history, conditions: next } };
    });
  }, []);

  const setHistoryNotes = useCallback((value: string) => {
    setForm((prev) => ({
      ...prev,
      history: { ...prev.history, additionalNotes: value },
    }));
  }, []);

  // ── Blur validation ──

  const handleBlur = useCallback(
    (field: keyof FormErrors) => {
      setTouched((prev) => ({ ...prev, [field]: true }));
      const allErrors = validate(form);
      setErrors((prev) => ({ ...prev, [field]: allErrors[field] }));
    },
    [form],
  );

  const hasError = useCallback(
    (field: keyof FormErrors) => touched[field] && errors[field],
    [touched, errors],
  );

  const errorClass = useCallback(
    (field: keyof FormErrors) =>
      cn(
        hasError(field) && 'border-destructive focus-visible:ring-destructive',
      ),
    [hasError],
  );

  // ── Submit ──

  const handleSubmit = useCallback(
    async (e: FormEvent) => {
      e.preventDefault();
      const formErrors = validate(form);
      const allFields = Object.keys(formErrors) as (keyof FormErrors)[];
      const newTouched: Partial<Record<keyof FormErrors, boolean>> = {};
      for (const f of allFields) newTouched[f] = true;

      // Also mark required fields touched even if no error.
      const requiredFields: (keyof FormErrors)[] = [
        'allergyName',
        'allergySeverity',
        'medicationName',
        'chiefComplaint',
      ];
      for (const f of requiredFields) newTouched[f] = true;

      setTouched(newTouched);
      setErrors(formErrors);

      if (Object.keys(formErrors).length > 0) return;

      // Navigate to summary page for review (AC-3) instead of direct submit.
      isNavigatingAwayRef.current = true;
      dispatch(setIntakeDraft(form));
      clearLocal();
      void navigate('/intake/summary');
    },
    [form, clearLocal, navigate, dispatch],
  );

  // ── Save Draft handler ──

  const handleSaveDraft = useCallback(async () => {
    isNavigatingAwayRef.current = true;
    await saveNow();
    void navigate('/dashboard');
  }, [saveNow, navigate]);

  // ── Memoized select class ──

  const selectClass = useMemo(
    () =>
      'flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50',
    [],
  );

  return (
    <>
      <NavigationGuard
        when={formDirty && !isSubmitting}
        skipRef={isNavigatingAwayRef}
      />

      <div className="max-w-200">
        {/* Breadcrumb */}
        <nav
          className="flex items-center gap-2 text-sm mb-6"
          aria-label="Breadcrumb"
        >
          <Link to="/dashboard" className="text-primary hover:underline">
            Dashboard
          </Link>
          <span className="text-muted-foreground" aria-hidden="true">
            ›
          </span>
          <span aria-current="page">Manual Intake</span>
        </nav>

        {/* Page header */}
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-[32px] font-bold">Patient Intake Form</h1>
          <div className="flex items-center gap-4">
            <AutosaveIndicator status={status} lastSavedAt={lastSavedAt} />
            <IntakeModeToggle currentData={form} />
          </div>
        </div>

        {/* Loading state */}
        {isLoading && (
          <div className="flex items-center justify-center py-16" role="status">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <span className="sr-only">Loading intake form…</span>
          </div>
        )}

        {/* Submit error banner */}
        {submitError && (
          <div
            className="flex items-center gap-2 rounded-lg border border-destructive/50 bg-destructive/10 p-4 mb-6 text-sm text-destructive"
            role="alert"
          >
            <AlertTriangle className="h-4 w-4 shrink-0" />
            <span>{submitError}</span>
          </div>
        )}

        {!isLoading && (
          <form
            onSubmit={(e) => void handleSubmit(e)}
            noValidate
            aria-label="Patient intake form"
          >
            {/* ── Allergies ── */}
            <IntakeFormSection title="Allergies">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
                <div className="space-y-1">
                  <Label htmlFor="allergy-name">
                    Allergy Name{' '}
                    <span className="text-destructive" aria-label="required">
                      *
                    </span>
                  </Label>
                  <Input
                    id="allergy-name"
                    placeholder="e.g., Penicillin"
                    value={form.allergies.name}
                    onChange={(e) => setAllergyField('name', e.target.value)}
                    onBlur={() => handleBlur('allergyName')}
                    aria-invalid={!!hasError('allergyName')}
                    aria-describedby="allergy-name-error"
                    className={errorClass('allergyName')}
                    disabled={isSubmitting}
                  />
                  {hasError('allergyName') && (
                    <p
                      className="flex items-center gap-1 text-xs text-destructive"
                      id="allergy-name-error"
                      role="alert"
                    >
                      <AlertCircle className="h-3 w-3 shrink-0" />
                      {errors.allergyName}
                    </p>
                  )}
                </div>

                <div className="space-y-1">
                  <Label htmlFor="allergy-type">Type</Label>
                  <select
                    id="allergy-type"
                    className={selectClass}
                    value={form.allergies.type}
                    onChange={(e) => setAllergyField('type', e.target.value)}
                    disabled={isSubmitting}
                  >
                    <option value="">Select</option>
                    {ALLERGY_TYPES.map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <Label htmlFor="allergy-severity">
                    Severity{' '}
                    <span className="text-destructive" aria-label="required">
                      *
                    </span>
                  </Label>
                  <select
                    id="allergy-severity"
                    className={cn(
                      selectClass,
                      hasError('allergySeverity') && 'border-destructive',
                    )}
                    value={form.allergies.severity}
                    onChange={(e) =>
                      setAllergyField('severity', e.target.value)
                    }
                    onBlur={() => handleBlur('allergySeverity')}
                    aria-invalid={!!hasError('allergySeverity')}
                    aria-describedby="allergy-severity-error"
                    disabled={isSubmitting}
                  >
                    <option value="">Select</option>
                    {SEVERITY_OPTIONS.map((s) => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </select>
                  {hasError('allergySeverity') && (
                    <p
                      className="flex items-center gap-1 text-xs text-destructive"
                      id="allergy-severity-error"
                      role="alert"
                    >
                      <AlertCircle className="h-3 w-3 shrink-0" />
                      {errors.allergySeverity}
                    </p>
                  )}
                </div>

                <div className="space-y-1">
                  <Label htmlFor="allergy-reaction">Reaction</Label>
                  <Input
                    id="allergy-reaction"
                    placeholder="e.g., Hives, swelling"
                    value={form.allergies.reaction}
                    onChange={(e) =>
                      setAllergyField('reaction', e.target.value)
                    }
                    disabled={isSubmitting}
                  />
                </div>
              </div>
            </IntakeFormSection>

            {/* ── Current Medications ── */}
            <IntakeFormSection title="Current Medications">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
                <div className="space-y-1">
                  <Label htmlFor="med-name">
                    Medication Name{' '}
                    <span className="text-destructive" aria-label="required">
                      *
                    </span>
                  </Label>
                  <Input
                    id="med-name"
                    placeholder="e.g., Lisinopril"
                    value={form.medications.name}
                    onChange={(e) => setMedicationField('name', e.target.value)}
                    onBlur={() => handleBlur('medicationName')}
                    aria-invalid={!!hasError('medicationName')}
                    aria-describedby="med-name-error"
                    className={errorClass('medicationName')}
                    disabled={isSubmitting}
                  />
                  {hasError('medicationName') && (
                    <p
                      className="flex items-center gap-1 text-xs text-destructive"
                      id="med-name-error"
                      role="alert"
                    >
                      <AlertCircle className="h-3 w-3 shrink-0" />
                      {errors.medicationName}
                    </p>
                  )}
                </div>

                <div className="space-y-1">
                  <Label htmlFor="med-dosage">Dosage</Label>
                  <Input
                    id="med-dosage"
                    placeholder="e.g., 10mg daily"
                    value={form.medications.dosage}
                    onChange={(e) =>
                      setMedicationField('dosage', e.target.value)
                    }
                    disabled={isSubmitting}
                  />
                </div>

                <div className="space-y-1 sm:col-span-2">
                  <Label htmlFor="med-notes">Notes</Label>
                  <textarea
                    id="med-notes"
                    className={cn(selectClass, 'h-auto min-h-20 resize-y py-2')}
                    placeholder="Additional details about medication..."
                    value={form.medications.notes}
                    onChange={(e) =>
                      setMedicationField('notes', e.target.value)
                    }
                    disabled={isSubmitting}
                  />
                </div>
              </div>
            </IntakeFormSection>

            {/* ── Medical History ── */}
            <IntakeFormSection title="Medical History">
              <div className="mb-4">
                <Label className="mb-2">
                  Conditions (select all that apply)
                </Label>
                <div className="flex flex-col gap-2">
                  {CONDITIONS.map((condition) => (
                    <label
                      key={condition}
                      className="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer"
                    >
                      <Checkbox
                        checked={form.history.conditions.includes(condition)}
                        onCheckedChange={() => toggleCondition(condition)}
                        disabled={isSubmitting}
                      />
                      {condition}
                    </label>
                  ))}
                </div>
              </div>

              <div className="space-y-1">
                <Label htmlFor="history-notes">Additional History</Label>
                <textarea
                  id="history-notes"
                  className={cn(selectClass, 'h-auto min-h-20 resize-y py-2')}
                  placeholder="Describe any other medical conditions, surgeries, or relevant history..."
                  value={form.history.additionalNotes}
                  onChange={(e) => setHistoryNotes(e.target.value)}
                  disabled={isSubmitting}
                />
              </div>
            </IntakeFormSection>

            {/* ── Current Symptoms ── */}
            <IntakeFormSection title="Current Symptoms">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
                <div className="space-y-1 sm:col-span-2">
                  <Label htmlFor="chief-complaint">
                    Chief Complaint{' '}
                    <span className="text-destructive" aria-label="required">
                      *
                    </span>
                  </Label>
                  <Input
                    id="chief-complaint"
                    placeholder="Primary reason for visit"
                    value={form.symptoms.chiefComplaint}
                    onChange={(e) =>
                      setSymptomField('chiefComplaint', e.target.value)
                    }
                    onBlur={() => handleBlur('chiefComplaint')}
                    aria-invalid={!!hasError('chiefComplaint')}
                    aria-describedby="chief-complaint-error"
                    className={errorClass('chiefComplaint')}
                    disabled={isSubmitting}
                  />
                  {hasError('chiefComplaint') && (
                    <p
                      className="flex items-center gap-1 text-xs text-destructive"
                      id="chief-complaint-error"
                      role="alert"
                    >
                      <AlertCircle className="h-3 w-3 shrink-0" />
                      {errors.chiefComplaint}
                    </p>
                  )}
                </div>

                <div className="space-y-1">
                  <Label htmlFor="symptom-duration">Duration</Label>
                  <select
                    id="symptom-duration"
                    className={selectClass}
                    value={form.symptoms.duration}
                    onChange={(e) =>
                      setSymptomField('duration', e.target.value)
                    }
                    disabled={isSubmitting}
                  >
                    <option value="">Select</option>
                    {DURATION_OPTIONS.map((d) => (
                      <option key={d} value={d}>
                        {d}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <Label htmlFor="symptom-severity">Severity (1-10)</Label>
                  <Input
                    type="number"
                    id="symptom-severity"
                    min={1}
                    max={10}
                    placeholder="1-10"
                    value={form.symptoms.severity}
                    onChange={(e) =>
                      setSymptomField('severity', e.target.value)
                    }
                    onBlur={() => handleBlur('symptomSeverity')}
                    aria-invalid={!!hasError('symptomSeverity')}
                    aria-describedby="symptom-severity-error"
                    className={errorClass('symptomSeverity')}
                    disabled={isSubmitting}
                  />
                  {hasError('symptomSeverity') && (
                    <p
                      className="flex items-center gap-1 text-xs text-destructive"
                      id="symptom-severity-error"
                      role="alert"
                    >
                      <AlertCircle className="h-3 w-3 shrink-0" />
                      {errors.symptomSeverity}
                    </p>
                  )}
                </div>
              </div>
            </IntakeFormSection>

            {/* ── Action bar ── */}
            <div className="flex gap-3 mt-8">
              <Button
                type="submit"
                size="lg"
                disabled={isSubmitting}
                className="h-12 px-6 text-base"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Submitting…
                  </>
                ) : (
                  'Review & Submit'
                )}
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="lg"
                disabled={isSubmitting}
                className="h-12 px-6 text-base text-muted-foreground"
                onClick={() => void handleSaveDraft()}
              >
                Save Draft
              </Button>
            </div>
          </form>
        )}
      </div>
    </>
  );
}
