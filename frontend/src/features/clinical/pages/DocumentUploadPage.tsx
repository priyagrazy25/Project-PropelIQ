import { Button } from '@/components/ui/button';
import * as signalR from '@microsoft/signalr';
import { ArrowLeft, ChevronRight, FileUp } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { getAccessToken } from '../../../shared/api/authInterceptor';
import type { DocumentProcessingStatus } from '../api/documentApi';
import { uploadDocument } from '../api/documentApi';
import { DropZone } from '../components/DropZone';
import type { FileUploadStatus } from '../components/FileProgressBar';
import { FileProgressBar } from '../components/FileProgressBar';

interface UploadFileEntry {
  id: string;
  file: File;
  progress: number;
  status: FileUploadStatus;
  errorMessage?: string;
  documentId?: string;
  processingStatus?: DocumentProcessingStatus;
  abortController?: AbortController;
}

function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

/**
 * Clinical document upload page with drag-and-drop support (SCR-014, UXR-106).
 * Implements per-file progress, retry on failure, and SignalR status updates.
 */
export function DocumentUploadPage() {
  const navigate = useNavigate();
  const [files, setFiles] = useState<UploadFileEntry[]>([]);
  const [dropzoneError, setDropzoneError] = useState<string>();
  const hubConnectionRef = useRef<signalR.HubConnection | null>(null);
  const connectionAbortedRef = useRef(false);

  // Connect to SignalR hub for document status updates
  useEffect(() => {
    connectionAbortedRef.current = false;
    
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/documents', {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('DocumentStatusUpdated', (documentId: string, status: DocumentProcessingStatus) => {
      setFiles((prev) =>
        prev.map((f) =>
          f.documentId === documentId ? { ...f, processingStatus: status } : f,
        ),
      );

      if (status === 'Complete') {
        toast.success('Document processed successfully.');
      } else if (status === 'Failed') {
        toast.error('Document processing failed.');
      }
    });

    connection
      .start()
      .then(() => {
        if (!connectionAbortedRef.current) {
          hubConnectionRef.current = connection;
        }
      })
      .catch(() => {
        // Silently ignore connection errors - React StrictMode causes double-mount
      });

    return () => {
      connectionAbortedRef.current = true;
      hubConnectionRef.current = null;
      void connection.stop();
    };
  }, []);

  // Upload a single file (with mock fallback for demo when backend unavailable)
  const uploadFile = useCallback((entry: UploadFileEntry) => {
    const abortController = new AbortController();

    setFiles((prev) =>
      prev.map((f) =>
        f.id === entry.id
          ? { ...f, status: 'uploading' as FileUploadStatus, progress: 0, abortController, errorMessage: undefined }
          : f,
      ),
    );

    // Simulate progress for better UX
    let mockProgress = 0;
    const progressInterval = setInterval(() => {
      mockProgress += 10;
      if (mockProgress <= 90) {
        setFiles((prev) =>
          prev.map((f) => (f.id === entry.id ? { ...f, progress: mockProgress } : f)),
        );
      }
    }, 100);

    uploadDocument(
      entry.file,
      (percent) => {
        clearInterval(progressInterval);
        setFiles((prev) =>
          prev.map((f) => (f.id === entry.id ? { ...f, progress: percent } : f)),
        );
      },
      abortController.signal,
    ).then((result) => {
      clearInterval(progressInterval);
      if (result.success) {
        setFiles((prev) =>
          prev.map((f) =>
            f.id === entry.id
              ? {
                  ...f,
                  status: 'complete' as FileUploadStatus,
                  progress: 100,
                  documentId: result.data.documentId,
                  processingStatus: result.data.processingStatus,
                  abortController: undefined,
                }
              : f,
          ),
        );
        toast.success(`"${entry.file.name}" uploaded.`);
      } else {
        setFiles((prev) =>
          prev.map((f) =>
            f.id === entry.id
              ? {
                  ...f,
                  status: 'error' as FileUploadStatus,
                  errorMessage: result.error,
                  abortController: undefined,
                }
              : f,
          ),
        );
      }
    });
  }, []);

  // Handle new files selected via DropZone
  const handleFilesSelected = useCallback(
    (newFiles: File[]) => {
      setDropzoneError(undefined);

      const newEntries: UploadFileEntry[] = newFiles.map((file) => ({
        id: generateId(),
        file,
        progress: 0,
        status: 'pending' as FileUploadStatus,
      }));

      setFiles((prev) => [...prev, ...newEntries]);

      // Start upload for each new file
      for (const entry of newEntries) {
        uploadFile(entry);
      }
    },
    [uploadFile],
  );

  // Remove a file from the list (or cancel upload)
  const handleRemove = useCallback((id: string) => {
    setFiles((prev) => {
      const file = prev.find((f) => f.id === id);
      if (file?.abortController) {
        file.abortController.abort();
      }
      return prev.filter((f) => f.id !== id);
    });
  }, []);

  // Retry a failed upload (UXR-604: retain successful uploads)
  const handleRetry = useCallback(
    (id: string) => {
      const entry = files.find((f) => f.id === id);
      if (entry) {
        uploadFile(entry);
      }
    },
    [files, uploadFile],
  );

  const hasCompletedDocuments = files.some((f) => f.status === 'complete');
  const isUploading = files.some((f) => f.status === 'uploading');

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-1 text-sm" aria-label="Breadcrumb">
        <Link to="/dashboard" className="text-primary hover:underline">
          Dashboard
        </Link>
        <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
        <span className="text-muted-foreground" aria-current="page">
          Upload Documents
        </span>
      </nav>

      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold text-foreground">
          Upload Clinical Documents
        </h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Upload PDF, JPEG, PNG, or TIFF files. Max 10 files, 25 MB each.
        </p>
      </div>

      {/* Drop Zone */}
      <DropZone
        onFilesSelected={handleFilesSelected}
        disabled={isUploading}
        errorMessage={dropzoneError}
        onClearError={() => setDropzoneError(undefined)}
      />

      {/* File List - All files (uploading, complete, error) */}
      {files.length > 0 && (
        <div className="space-y-3" aria-label="Uploaded files">
          {files.map((entry) => (
            <FileProgressBar
              key={entry.id}
              id={entry.id}
              fileName={entry.file.name}
              fileSize={entry.file.size}
              progress={entry.progress}
              status={entry.status}
              errorMessage={entry.errorMessage}
              onRemove={handleRemove}
              onRetry={handleRetry}
            />
          ))}
        </div>
      )}

      {/* Action Buttons */}
      <div className="flex flex-wrap gap-3 pt-4">
        <Button
          disabled={!hasCompletedDocuments || isUploading}
          onClick={() => void navigate('/documents/processing')}
          className="gap-2"
        >
          <FileUp className="size-4" />
          Process Documents
        </Button>
        <Button
          variant="ghost"
          onClick={() => void navigate('/dashboard')}
          className="gap-2"
        >
          <ArrowLeft className="size-4" />
          Back to Dashboard
        </Button>
      </div>
    </div>
  );
}
