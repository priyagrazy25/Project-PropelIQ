import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { FileProgressBar } from './FileProgressBar';

describe('FileProgressBar', () => {
  afterEach(() => {
    cleanup();
  });

  it('renders upload progress and cancel action while uploading', () => {
    const onRemove = vi.fn();

    render(
      <FileProgressBar
        id="f1"
        fileName="scan.pdf"
        fileSize={1024}
        progress={42}
        status="uploading"
        onRemove={onRemove}
        onRetry={vi.fn()}
      />,
    );

    expect(screen.getByText('42%')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Cancel scan.pdf' }));
    expect(onRemove).toHaveBeenCalledWith('f1');
  });

  it('renders error state and triggers retry callback', () => {
    const onRetry = vi.fn();

    render(
      <FileProgressBar
        id="f2"
        fileName="broken.pdf"
        fileSize={2048}
        progress={0}
        status="error"
        errorMessage="Upload failed"
        onRemove={vi.fn()}
        onRetry={onRetry}
      />,
    );

    expect(screen.getByText('Failed')).toBeInTheDocument();
    expect(screen.getByText('Upload failed')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Retry broken.pdf' }));
    expect(onRetry).toHaveBeenCalledWith('f2');
  });
});
