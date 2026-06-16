import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { CheckCircle2, Clock, FileText, Loader2, XCircle } from 'lucide-react';
import type { DocumentProcessingStatus } from '../api/documentApi';

export interface DocumentStatus {
  documentId: string;
  fileName: string;
  processingStatus: DocumentProcessingStatus;
}

interface DocumentStatusListProps {
  /** List of documents with their processing status. */
  documents: DocumentStatus[];
}

const STATUS_CONFIG: Record<
  DocumentProcessingStatus,
  { icon: React.ElementType; label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }
> = {
  Queued: { icon: Clock, label: 'Queued', variant: 'outline' },
  Processing: { icon: Loader2, label: 'Processing', variant: 'default' },
  Complete: { icon: CheckCircle2, label: 'Complete', variant: 'secondary' },
  Failed: { icon: XCircle, label: 'Failed', variant: 'destructive' },
};

/**
 * Displays a list of uploaded documents with their processing status.
 * Used after upload to show queued/processing/complete/failed documents.
 */
export function DocumentStatusList({ documents }: DocumentStatusListProps) {
  if (documents.length === 0) {
    return null;
  }

  return (
    <div className="space-y-2" aria-label="Document processing status">
      <h3 className="text-sm font-medium text-foreground">Processing Status</h3>
      <div className="divide-y rounded-lg border bg-card">
        {documents.map((doc) => {
          const config = STATUS_CONFIG[doc.processingStatus];
          const Icon = config.icon;
          const isProcessing = doc.processingStatus === 'Processing';

          return (
            <div
              key={doc.documentId}
              className="flex items-center gap-3 px-4 py-3"
            >
              <FileText
                className="size-5 shrink-0 text-muted-foreground"
                aria-hidden="true"
              />
              <span
                className="min-w-0 flex-1 truncate text-sm font-medium text-foreground"
                title={doc.fileName}
              >
                {doc.fileName}
              </span>
              <Badge
                variant={config.variant}
                className="flex shrink-0 items-center gap-1"
              >
                <Icon
                  className={cn('size-3', isProcessing && 'animate-spin')}
                  aria-hidden="true"
                />
                <span>{config.label}</span>
              </Badge>
            </div>
          );
        })}
      </div>
    </div>
  );
}
