import { getAccessToken } from '../../../shared/api/authInterceptor';

const API_BASE = '/api';

export type DocumentProcessingStatus =
  | 'Queued'
  | 'Processing'
  | 'Complete'
  | 'Failed';

export interface UploadDocumentResponse {
  documentId: string;
  fileName: string;
  processingStatus: DocumentProcessingStatus;
}

export interface DocumentApiError {
  message: string;
}

/**
 * Uploads a single clinical document via multipart/form-data.
 * Uses XMLHttpRequest to support per-file upload progress reporting (UXR-106).
 * Resolves with success or error — never rejects — for safe caller handling.
 */
export function uploadDocument(
  file: File,
  onProgress: (percent: number) => void,
  signal?: AbortSignal,
): Promise<
  | { success: true; data: UploadDocumentResponse }
  | { success: false; error: string }
> {
  return new Promise((resolve) => {
    const xhr = new XMLHttpRequest();
    const formData = new FormData();
    formData.append('file', file);

    xhr.upload.addEventListener('progress', (event) => {
      if (event.lengthComputable) {
        onProgress(Math.round((event.loaded / event.total) * 100));
      }
    });

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          const data = JSON.parse(xhr.responseText) as UploadDocumentResponse;
          resolve({ success: true, data });
        } catch {
          resolve({ success: false, error: 'Invalid response from server.' });
        }
      } else if (xhr.status === 413) {
        resolve({ success: false, error: 'File exceeds the maximum allowed size.' });
      } else {
        resolve({
          success: false,
          error: `Upload failed with status ${xhr.status}. Please retry.`,
        });
      }
    });

    xhr.addEventListener('error', () => {
      resolve({ success: false, error: 'Network error. Please check your connection and retry.' });
    });

    xhr.addEventListener('abort', () => {
      resolve({ success: false, error: 'Upload was cancelled.' });
    });

    if (signal) {
      signal.addEventListener('abort', () => {
        xhr.abort();
      });
    }

    xhr.open('POST', `${API_BASE}/clinical/documents/upload`);

    const token = getAccessToken();
    if (token) {
      xhr.setRequestHeader('Authorization', `Bearer ${token}`);
    }

    xhr.send(formData);
  });
}

/** Pipeline status for a document being processed. */
export interface DocumentPipelineStatus {
  documentId: string;
  fileName: string;
  currentStep: string;
  completedSteps: string[];
  status: DocumentProcessingStatus;
  progress: number;
}

/**
 * Fetches processing status for all documents belonging to the current user.
 * Used by SCR-015 Processing Status page.
 */
export async function fetchDocumentProcessingStatus(): Promise<
  | { success: true; data: DocumentPipelineStatus[] }
  | { success: false; error: string }
> {
  try {
    const token = getAccessToken();
    const response = await fetch(`${API_BASE}/clinical/documents/processing-status`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) {
      return { success: false, error: `Failed to fetch status: ${response.status}` };
    }

    const data = (await response.json()) as DocumentPipelineStatus[];
    return { success: true, data };
  } catch (error) {
    return { success: false, error: 'Network error. Unable to fetch document status.' };
  }
}
