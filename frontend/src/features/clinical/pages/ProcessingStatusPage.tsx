import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import * as signalR from '@microsoft/signalr';
import { ArrowLeft, CheckCircle2, ChevronRight, FileText, Image, Loader2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getAccessToken } from '../../../shared/api/authInterceptor';
import { fetchDocumentProcessingStatus, type DocumentPipelineStatus } from '../api/documentApi';

/** Processing pipeline steps in order. */
const PIPELINE_STEPS = ['OCR', 'NER', 'Coding', 'Validation'] as const;
type PipelineStep = (typeof PIPELINE_STEPS)[number];

interface DocumentProcessingItem {
  documentId: string;
  fileName: string;
  currentStep: PipelineStep;
  completedSteps: PipelineStep[];
  status: 'Queued' | 'Processing' | 'Complete' | 'Failed';
  progress: number;
}

function getStepStatus(
  step: PipelineStep,
  currentStep: PipelineStep,
  completedSteps: PipelineStep[],
): 'done' | 'active' | 'pending' {
  if (completedSteps.includes(step)) return 'done';
  if (step === currentStep) return 'active';
  return 'pending';
}

function isImageFile(fileName: string): boolean {
  const ext = fileName.toLowerCase();
  return ext.endsWith('.jpg') || ext.endsWith('.jpeg') || ext.endsWith('.png') || ext.endsWith('.tiff') || ext.endsWith('.tif');
}

function getStatusBadge(status: DocumentProcessingItem['status']) {
  switch (status) {
    case 'Complete':
      return (
        <Badge variant="secondary" className="bg-emerald-100 text-emerald-700 hover:bg-emerald-100">
          Complete
        </Badge>
      );
    case 'Processing':
      return (
        <Badge variant="secondary" className="bg-blue-100 text-blue-700 hover:bg-blue-100">
          Processing
        </Badge>
      );
    case 'Queued':
      return (
        <Badge variant="secondary" className="bg-gray-100 text-gray-600 hover:bg-gray-100">
          Queued
        </Badge>
      );
    case 'Failed':
      return (
        <Badge variant="destructive">
          Failed
        </Badge>
      );
    default:
      return null;
  }
}

/**
 * Document Processing Status page (SCR-015).
 * Shows real-time pipeline progress for uploaded documents via SignalR.
 * Pipeline: OCR → NER → Coding → Validation (UXR-403: Processing transparency).
 */
export function ProcessingStatusPage() {
  const navigate = useNavigate();
  const [documents, setDocuments] = useState<DocumentProcessingItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const hubConnectionRef = useRef<signalR.HubConnection | null>(null);
  const connectionAbortedRef = useRef(false);

  // Load initial document status and connect to SignalR
  useEffect(() => {
    connectionAbortedRef.current = false;
    
    const loadDocuments = async () => {
      setIsLoading(true);
      const result = await fetchDocumentProcessingStatus();
      if (result.success && !connectionAbortedRef.current) {
        setDocuments(mapApiToDocuments(result.data));
      } else if (!connectionAbortedRef.current) {
        // Backend unavailable - show empty state
        setDocuments([]);
      }
      if (!connectionAbortedRef.current) setIsLoading(false);
    };

    void loadDocuments();

    // Connect to SignalR for real-time updates
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/documents', {
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on(
      'DocumentStepCompleted',
      (documentId: string, step: string, progress: number) => {
        setDocuments((prev) =>
          prev.map((doc) => {
            if (doc.documentId !== documentId) return doc;
            const stepTyped = step as PipelineStep;
            const stepIndex = PIPELINE_STEPS.indexOf(stepTyped);
            const nextStep = PIPELINE_STEPS[stepIndex + 1] ?? stepTyped;
            return {
              ...doc,
              completedSteps: [...doc.completedSteps, stepTyped],
              currentStep: nextStep,
              progress,
              status: progress >= 100 ? 'Complete' : 'Processing',
            };
          }),
        );
      },
    );

    connection.on('DocumentProcessingFailed', (documentId: string) => {
      setDocuments((prev) =>
        prev.map((doc) =>
          doc.documentId === documentId ? { ...doc, status: 'Failed' } : doc,
        ),
      );
    });

    connection
      .start()
      .then(() => {
        if (!connectionAbortedRef.current) {
          hubConnectionRef.current = connection;
        }
      })
      .catch(() => {
        // Silently ignore - React StrictMode causes double-mount race conditions
      });

    return () => {
      connectionAbortedRef.current = true;
      hubConnectionRef.current = null;
      void connection.stop();
    };
  }, []);

  const allComplete = documents.every((d) => d.status === 'Complete');
  const hasProcessing = documents.some((d) => d.status === 'Processing' || d.status === 'Queued');

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-1 text-sm" aria-label="Breadcrumb">
        <Link to="/dashboard" className="text-primary hover:underline">
          Dashboard
        </Link>
        <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
        <Link to="/documents" className="text-primary hover:underline">
          Documents
        </Link>
        <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
        <span className="text-muted-foreground" aria-current="page">
          Processing
        </span>
      </nav>

      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold text-foreground">Document Processing</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          AI is extracting clinical data from your uploaded documents.
        </p>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="size-8 animate-spin text-primary" />
          <span className="ml-3 text-muted-foreground">Loading document status...</span>
        </div>
      )}

      {/* Document Pipeline Cards */}
      {!isLoading && documents.length > 0 && (
        <div className="space-y-4" role="list" aria-label="Document processing status">
          {documents.map((doc) => (
            <Card key={doc.documentId} role="listitem">
              <CardContent className="p-5">
                {/* Header: File name + Status badge */}
                <div className="mb-3 flex items-center justify-between">
                  <div className="flex items-center gap-2 font-semibold text-sm">
                    {isImageFile(doc.fileName) ? (
                      <Image className="size-4 text-muted-foreground" aria-hidden="true" />
                    ) : (
                      <FileText className="size-4 text-muted-foreground" aria-hidden="true" />
                    )}
                    {doc.fileName}
                  </div>
                  {getStatusBadge(doc.status)}
                </div>

                {/* Progress Bar */}
                <div
                  className="mb-3 h-2 overflow-hidden rounded-full bg-muted"
                  role="progressbar"
                  aria-valuenow={doc.progress}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-label={`${doc.fileName} processing progress`}
                >
                  <div
                    className={cn(
                      'h-full rounded-full transition-all duration-300',
                      doc.status === 'Complete' && 'bg-emerald-500',
                      doc.status === 'Processing' && 'animate-pulse bg-primary',
                      doc.status === 'Failed' && 'bg-destructive',
                      doc.status === 'Queued' && 'bg-muted-foreground',
                    )}
                    style={{ width: `${doc.progress}%` }}
                  />
                </div>

                {/* Pipeline Steps */}
                <div className="flex flex-wrap gap-2 text-xs">
                  {PIPELINE_STEPS.map((step) => {
                    const stepStatus = getStepStatus(step, doc.currentStep, doc.completedSteps);
                    return (
                      <span
                        key={step}
                        className={cn(
                          'flex items-center gap-1 rounded px-2 py-1',
                          stepStatus === 'done' && 'bg-emerald-100 text-emerald-700',
                          stepStatus === 'active' && 'bg-blue-100 text-blue-700 font-medium',
                          stepStatus === 'pending' && 'bg-muted text-muted-foreground',
                        )}
                      >
                        {stepStatus === 'done' && <CheckCircle2 className="size-3" />}
                        {stepStatus === 'active' && <Loader2 className="size-3 animate-spin" />}
                        {step}
                      </span>
                    );
                  })}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Empty State */}
      {!isLoading && documents.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <FileText className="mb-4 size-12 text-muted-foreground" />
            <p className="text-muted-foreground">No documents are being processed.</p>
            <Button variant="link" onClick={() => void navigate('/documents')} className="mt-2">
              Upload Documents
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Action Buttons */}
      <div className="flex flex-wrap gap-3 pt-4">
        <Button
          disabled={!allComplete || hasProcessing}
          onClick={() => void navigate('/health-profile')}
          className="gap-2"
        >
          View Health Profile
        </Button>
        <Button
          variant="outline"
          onClick={() => void navigate('/documents')}
          className="gap-2"
        >
          <ArrowLeft className="size-4" />
          Upload More
        </Button>
      </div>
    </div>
  );
}

/** Maps API response to local document processing items. */
function mapApiToDocuments(data: DocumentPipelineStatus[]): DocumentProcessingItem[] {
  return data.map((item) => ({
    documentId: item.documentId,
    fileName: item.fileName,
    currentStep: (item.currentStep as PipelineStep) || 'OCR',
    completedSteps: (item.completedSteps as PipelineStep[]) || [],
    status: item.status,
    progress: item.progress,
  }));
}
