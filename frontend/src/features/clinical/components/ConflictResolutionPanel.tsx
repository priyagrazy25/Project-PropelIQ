import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { CheckCircle2 } from 'lucide-react';

export interface ConflictSource {
  /** Source identifier (e.g., "AI Intake", "Insurance Card OCR"). */
  sourceLabel: string;
  /** Extracted value from this source. */
  value: string;
  /** Confidence score (0-1). */
  confidence: number;
  /** Date when data was extracted. */
  extractedAt: string;
  /** Source document name or reference. */
  documentName?: string;
}

export interface ConflictDetails {
  /** Unique conflict identifier. */
  conflictId: string;
  /** Field name with conflict (e.g., "Address", "Aspirin Dosage"). */
  field: string;
  /** Category of the conflicting data. */
  category: string;
  /** Source A details. */
  sourceA: ConflictSource;
  /** Source B details. */
  sourceB: ConflictSource;
}

export interface ConflictResolutionPanelProps {
  /** Conflict details to display. */
  conflict: ConflictDetails;
  /** Currently selected resolution option. */
  selectedOption: 'A' | 'B' | 'manual' | null;
  /** Callback when user selects a panel. */
  onSelectSource?: (source: 'A' | 'B') => void;
}

/**
 * Side-by-side conflict comparison panel (SCR-017, UXR-108).
 * Displays conflicting values from two sources with confidence scores
 * and allows selection via panel click or radio buttons.
 */
export function ConflictResolutionPanel({
  conflict,
  selectedOption,
  onSelectSource,
}: ConflictResolutionPanelProps) {
  return (
    <div className="grid gap-6 md:grid-cols-2">
      <SourcePanel
        label="Source A"
        source={conflict.sourceA}
        field={conflict.field}
        isSelected={selectedOption === 'A'}
        isConflicting={selectedOption === 'B'}
        onClick={() => onSelectSource?.('A')}
      />
      <SourcePanel
        label="Source B"
        source={conflict.sourceB}
        field={conflict.field}
        isSelected={selectedOption === 'B'}
        isConflicting={selectedOption === 'A'}
        onClick={() => onSelectSource?.('B')}
      />
    </div>
  );
}

interface SourcePanelProps {
  label: string;
  source: ConflictSource;
  field: string;
  isSelected: boolean;
  isConflicting: boolean;
  onClick?: () => void;
}

/**
 * Individual source panel within the comparison view.
 * Highlights when selected, shows conflict indicator when opposing source is selected.
 */
function SourcePanel({
  label,
  source,
  field,
  isSelected,
  isConflicting,
  onClick,
}: SourcePanelProps) {
  const confidencePercent = Math.round(source.confidence * 100);
  const confidenceColor = getConfidenceColor(source.confidence);

  return (
    <Card
      className={cn(
        'cursor-pointer transition-all duration-150',
        isSelected && 'border-primary ring-2 ring-primary/20',
        !isSelected && !isConflicting && 'hover:border-primary/50'
      )}
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick?.();
        }
      }}
      aria-pressed={isSelected}
      aria-label={`Select ${label}: ${source.sourceLabel}`}
    >
      <CardHeader className="flex flex-row items-center justify-between space-y-0 border-b pb-3">
        <div className="flex items-center gap-2">
          <CardTitle className="text-base font-semibold">
            {label}: {source.sourceLabel}
          </CardTitle>
          {isSelected && (
            <CheckCircle2
              className="size-5 text-primary"
              aria-label="Selected"
            />
          )}
        </div>
        <span className="text-xs text-muted-foreground">
          {formatDate(source.extractedAt)}
        </span>
      </CardHeader>
      <CardContent className="space-y-3 pt-4">
        <FieldRow label="Field" value={field} />
        <FieldRow
          label="Value"
          value={source.value}
          valueClassName={cn(
            isConflicting && 'text-red-600 underline decoration-wavy decoration-red-500'
          )}
        />
        <FieldRow
          label="Confidence"
          value={`${confidencePercent}%`}
          valueClassName={confidenceColor}
        />
        {source.documentName && (
          <FieldRow
            label="Document"
            value={source.documentName}
            valueClassName="text-primary underline hover:text-primary/80"
            isLink
          />
        )}
      </CardContent>
    </Card>
  );
}

interface FieldRowProps {
  label: string;
  value: string;
  valueClassName?: string;
  isLink?: boolean;
}

function FieldRow({ label, value, valueClassName, isLink }: FieldRowProps) {
  return (
    <div className="flex items-center justify-between text-sm">
      <span className="text-muted-foreground">{label}</span>
      {isLink ? (
        <button
          type="button"
          className={cn('font-medium', valueClassName)}
          onClick={(e) => e.stopPropagation()}
        >
          {value}
        </button>
      ) : (
        <span className={cn('font-medium', valueClassName)}>{value}</span>
      )}
    </div>
  );
}

function getConfidenceColor(confidence: number): string {
  if (confidence >= 0.9) return 'text-emerald-600';
  if (confidence >= 0.8) return 'text-amber-600';
  return 'text-red-600';
}

function formatDate(dateString: string): string {
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  } catch {
    return dateString;
  }
}
