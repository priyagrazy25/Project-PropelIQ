import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { FileText, Image, RefreshCw, X } from 'lucide-react';

export type FileUploadStatus = 'pending' | 'uploading' | 'complete' | 'error';

export interface FileProgressBarProps {
  /** Unique identifier for this file upload. */
  id: string;
  /** Original file name. */
  fileName: string;
  /** File size in bytes. */
  fileSize: number;
  /** Current upload progress (0–100). */
  progress: number;
  /** Upload status. */
  status: FileUploadStatus;
  /** Error message (if status is 'error'). */
  errorMessage?: string;
  /** Called when user clicks Remove/Cancel. */
  onRemove: (id: string) => void;
  /** Called when user clicks Retry (only for error state). */
  onRetry: (id: string) => void;
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function isImageFile(fileName: string): boolean {
  const ext = fileName.toLowerCase();
  return ext.endsWith('.jpg') || ext.endsWith('.jpeg') || ext.endsWith('.png') || ext.endsWith('.tiff') || ext.endsWith('.tif');
}

/**
 * Per-file progress bar with status indicators (UXR-106, UXR-604).
 * Shows upload percentage, completion checkmark, or error with retry button.
 */
export function FileProgressBar({
  id,
  fileName,
  fileSize,
  progress,
  status,
  errorMessage,
  onRemove,
  onRetry,
}: FileProgressBarProps) {
  const isComplete = status === 'complete';
  const isError = status === 'error';
  const isUploading = status === 'uploading';
  const isImage = isImageFile(fileName);

  const progressBarColorClass = isComplete
    ? 'bg-secondary'
    : isError
      ? 'bg-destructive'
      : 'bg-primary';

  const statusText = isComplete
    ? '✓ Complete'
    : isError
      ? 'Failed'
      : `${progress}%`;

  const statusColorClass = isComplete
    ? 'text-secondary'
    : isError
      ? 'text-destructive'
      : 'text-primary';

  const FileIcon = isImage ? Image : FileText;

  return (
    <div className="flex items-center gap-3 rounded-lg border bg-card px-4 py-3">
      <FileIcon
        className="size-5 shrink-0 text-muted-foreground"
        aria-hidden="true"
      />

      <div className="min-w-0 flex-1">
        <div
          className="truncate text-sm font-medium text-foreground"
          title={fileName}
        >
          {fileName}
        </div>
        <div className="text-xs text-muted-foreground">
          {formatFileSize(fileSize)}
        </div>
        <div
          className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-muted"
          role="progressbar"
          aria-valuenow={progress}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={`${fileName} upload progress`}
        >
          <div
            className={cn(
              'h-full rounded-full transition-all duration-200',
              progressBarColorClass,
            )}
            style={{ width: `${progress}%` }}
          />
        </div>
        {isError && errorMessage && (
          <p className="mt-1 text-xs text-destructive">{errorMessage}</p>
        )}
      </div>

      <span className={cn('shrink-0 text-xs font-medium', statusColorClass)}>
        {statusText}
      </span>

      {isError ? (
        <Button
          variant="ghost"
          size="icon"
          className="size-8 shrink-0 text-primary hover:text-primary"
          onClick={() => onRetry(id)}
          aria-label={`Retry ${fileName}`}
        >
          <RefreshCw className="size-4" />
        </Button>
      ) : (
        <Button
          variant="ghost"
          size="icon"
          className="size-8 shrink-0 text-muted-foreground hover:text-destructive"
          onClick={() => onRemove(id)}
          aria-label={isUploading ? `Cancel ${fileName}` : `Remove ${fileName}`}
        >
          <X className="size-4" />
        </Button>
      )}
    </div>
  );
}
