import { cn } from '@/lib/utils';
import { Upload } from 'lucide-react';
import { useCallback, useRef, useState } from 'react';

const ACCEPTED_TYPES = [
  'application/pdf',
  'image/jpeg',
  'image/png',
  'image/tiff',
];
const ACCEPTED_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png', '.tiff', '.tif'];
const MAX_FILE_SIZE_MB = 25;
const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;
const MAX_FILES = 10;

export interface DropZoneProps {
  /** Callback when valid files are selected/dropped. */
  onFilesSelected: (files: File[]) => void;
  /** Currently disabled (e.g., during batch upload). */
  disabled?: boolean;
  /** Error message to display (e.g., rejection feedback). */
  errorMessage?: string;
  /** Clear the error message. */
  onClearError?: () => void;
}

interface ValidationResult {
  valid: File[];
  errors: string[];
}

function validateFiles(files: FileList | File[], existingCount: number = 0): ValidationResult {
  const valid: File[] = [];
  const errors: string[] = [];
  const fileArray = Array.from(files);

  // Check max files limit
  if (existingCount + fileArray.length > MAX_FILES) {
    errors.push(`Maximum ${MAX_FILES} files allowed. You have ${existingCount} file(s) already.`);
    return { valid: [], errors };
  }

  for (const file of fileArray) {
    const isAccepted =
      ACCEPTED_TYPES.includes(file.type) ||
      ACCEPTED_EXTENSIONS.some((ext) =>
        file.name.toLowerCase().endsWith(ext),
      );

    if (!isAccepted) {
      errors.push(`"${file.name}" is not a supported format. Accepted: PDF, JPEG, PNG, TIFF.`);
      continue;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      errors.push(
        `"${file.name}" exceeds ${MAX_FILE_SIZE_MB} MB size limit.`,
      );
      continue;
    }

    valid.push(file);
  }

  return { valid, errors };
}

/**
 * File drop zone supporting drag-and-drop and click-to-browse (UXR-106).
 * Validates files for PDF type and max size, providing per-file error feedback.
 */
export function DropZone({
  onFilesSelected,
  disabled = false,
  errorMessage,
  onClearError,
}: DropZoneProps) {
  const [isDragOver, setIsDragOver] = useState(false);
  const [localErrors, setLocalErrors] = useState<string[]>([]);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleDragEnter = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      if (!disabled) {
        setIsDragOver(true);
        onClearError?.();
      }
    },
    [disabled, onClearError],
  );

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);
  }, []);

  const handleDragOver = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      if (!disabled) {
        setIsDragOver(true);
      }
    },
    [disabled],
  );

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragOver(false);

      if (disabled) return;

      const { valid, errors } = validateFiles(e.dataTransfer.files);
      setLocalErrors(errors);

      if (valid.length > 0) {
        onFilesSelected(valid);
      }
    },
    [disabled, onFilesSelected],
  );

  const handleClick = useCallback(() => {
    if (!disabled) {
      inputRef.current?.click();
    }
  }, [disabled]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if ((e.key === 'Enter' || e.key === ' ') && !disabled) {
        e.preventDefault();
        inputRef.current?.click();
      }
    },
    [disabled],
  );

  const handleInputChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const files = e.target.files;
      if (!files || files.length === 0) return;

      const { valid, errors } = validateFiles(files);
      setLocalErrors(errors);

      if (valid.length > 0) {
        onFilesSelected(valid);
      }

      // Reset input to allow re-selecting the same file
      e.target.value = '';
    },
    [onFilesSelected],
  );

  const displayErrors = errorMessage
    ? [errorMessage, ...localErrors]
    : localErrors;

  return (
    <div className="space-y-2">
      <div
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-label="Upload files. Drag and drop or click to browse. Only PDF files, max 25 MB each."
        aria-disabled={disabled}
        onClick={handleClick}
        onKeyDown={handleKeyDown}
        onDragEnter={handleDragEnter}
        onDragLeave={handleDragLeave}
        onDragOver={handleDragOver}
        onDrop={handleDrop}
        className={cn(
          'flex min-h-40 cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed bg-card px-6 py-8 text-center transition-colors',
          isDragOver && !disabled && 'border-primary bg-accent',
          !isDragOver && !disabled && 'border-border hover:border-primary hover:bg-accent',
          disabled && 'cursor-not-allowed opacity-50',
          displayErrors.length > 0 && !isDragOver && 'border-destructive',
        )}
      >
        <Upload
          className={cn(
            'mb-3 size-12',
            isDragOver ? 'text-primary' : 'text-muted-foreground',
          )}
          aria-hidden="true"
        />
        <p className="text-base font-medium text-foreground">
          Drag &amp; drop files here
        </p>
        <p className="mt-1 text-sm text-muted-foreground">
          or click to browse · PDF, JPEG, PNG, TIFF · Max {MAX_FILE_SIZE_MB} MB each
        </p>
      </div>

      {displayErrors.length > 0 && (
        <div
          role="alert"
          className="space-y-1 rounded-md bg-destructive/10 p-3 text-sm text-destructive"
        >
          {displayErrors.map((err, idx) => (
            <p key={idx}>{err}</p>
          ))}
        </div>
      )}

      <input
        ref={inputRef}
        type="file"
        accept=".pdf,.jpg,.jpeg,.png,.tiff,.tif,application/pdf,image/jpeg,image/png,image/tiff"
        multiple
        onChange={handleInputChange}
        className="sr-only"
        aria-hidden="true"
        disabled={disabled}
      />
    </div>
  );
}
