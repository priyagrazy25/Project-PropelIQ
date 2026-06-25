import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { DocumentStatusList } from './DocumentStatusList';

describe('DocumentStatusList', () => {
  afterEach(() => {
    cleanup();
  });

  it('renders nothing when there are no documents', () => {
    const { container } = render(<DocumentStatusList documents={[]} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('renders each document and its processing status', () => {
    render(
      <DocumentStatusList
        documents={[
          {
            documentId: 'd1',
            fileName: 'lab-result.pdf',
            processingStatus: 'Queued',
          },
          {
            documentId: 'd2',
            fileName: 'summary.pdf',
            processingStatus: 'Complete',
          },
        ]}
      />,
    );

    expect(screen.getByText('Processing Status')).toBeInTheDocument();
    expect(screen.getByText('lab-result.pdf')).toBeInTheDocument();
    expect(screen.getByText('summary.pdf')).toBeInTheDocument();
    expect(screen.getByText('Queued')).toBeInTheDocument();
    expect(screen.getByText('Complete')).toBeInTheDocument();
  });
});
