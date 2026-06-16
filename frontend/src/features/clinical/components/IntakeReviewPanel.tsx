import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Check, Pencil, X } from 'lucide-react';
import { useCallback, useState } from 'react';
import type { ExtractedField } from '../api/intakeApi';
import { ConfidenceScoreBadge } from './ConfidenceScoreBadge';

export interface IntakeReviewPanelProps {
  fields: ExtractedField[];
  onFieldUpdate: (key: string, newValue: string) => void;
  onSubmit: () => void;
  isSubmitting: boolean;
}

const CATEGORY_LABELS: Record<string, string> = {
  allergy: 'Allergies',
  medication: 'Medications',
  symptom: 'Symptoms',
  history: 'Medical History',
  vital: 'Vitals',
};

export function IntakeReviewPanel({
  fields,
  onFieldUpdate,
  onSubmit,
  isSubmitting,
}: IntakeReviewPanelProps) {
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');

  const startEdit = useCallback((field: ExtractedField) => {
    setEditingKey(field.key);
    setEditValue(field.value);
  }, []);

  const cancelEdit = useCallback(() => {
    setEditingKey(null);
    setEditValue('');
  }, []);

  const saveEdit = useCallback(
    (key: string) => {
      onFieldUpdate(key, editValue);
      setEditingKey(null);
      setEditValue('');
    },
    [editValue, onFieldUpdate],
  );

  const grouped = fields.reduce<Record<string, ExtractedField[]>>(
    (acc, field) => {
      const cat = field.category;
      if (!acc[cat]) acc[cat] = [];
      acc[cat].push(field);
      return acc;
    },
    {},
  );

  return (
    <div className="flex flex-col gap-6 p-6">
      <div>
        <h2 className="text-xl font-semibold">Review Your Information</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Please review the extracted data below. Click the edit icon to correct
          any field before submitting.
        </p>
      </div>

      {Object.entries(grouped).map(([category, categoryFields]) => (
        <Card key={category}>
          <CardContent className="pt-6">
            <h3 className="mb-4 text-base font-semibold">
              {CATEGORY_LABELS[category] ?? category}
            </h3>
            <div className="flex flex-col gap-3">
              {categoryFields.map((field) => (
                <div
                  key={field.key}
                  className="flex items-center gap-3 rounded-lg border px-4 py-3"
                >
                  <div className="min-w-30 text-sm font-medium text-muted-foreground">
                    {field.label}
                  </div>

                  {editingKey === field.key ? (
                    <div className="flex flex-1 items-center gap-2">
                      <Input
                        value={editValue}
                        onChange={(e) => setEditValue(e.target.value)}
                        className="h-8"
                        autoFocus
                        aria-label={`Edit ${field.label}`}
                      />
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => saveEdit(field.key)}
                        aria-label="Save"
                      >
                        <Check className="size-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={cancelEdit}
                        aria-label="Cancel"
                      >
                        <X className="size-4" />
                      </Button>
                    </div>
                  ) : (
                    <>
                      <div className="flex-1 text-sm">{field.value}</div>
                      <ConfidenceScoreBadge score={field.confidence} />
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => startEdit(field)}
                        aria-label={`Edit ${field.label}`}
                      >
                        <Pencil className="size-4" />
                      </Button>
                    </>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      ))}

      <Button className="self-end" onClick={onSubmit} disabled={isSubmitting}>
        {isSubmitting ? 'Submitting...' : 'Confirm & Submit'}
      </Button>
    </div>
  );
}
