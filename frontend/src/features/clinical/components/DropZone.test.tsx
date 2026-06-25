import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { DropZone } from './DropZone';

function createFile(name: string, type: string, size?: number): File {
  const file = new File(['content'], name, { type });
  if (typeof size === 'number') {
    Object.defineProperty(file, 'size', { value: size });
  }
  return file;
}

describe('DropZone', () => {
  afterEach(() => {
    cleanup();
  });

  it('passes valid PDF file to onFilesSelected', () => {
    const onFilesSelected = vi.fn();

    const { container } = render(<DropZone onFilesSelected={onFilesSelected} />);

    const input = container.querySelector('input[type="file"]') as HTMLInputElement;
    const pdf = createFile('report.pdf', 'application/pdf');

    fireEvent.change(input, { target: { files: [pdf] } });

    expect(onFilesSelected).toHaveBeenCalledTimes(1);
    expect(onFilesSelected).toHaveBeenCalledWith([pdf]);
  });

  it('shows validation error for unsupported file type', async () => {
    const onFilesSelected = vi.fn();

    const { container } = render(<DropZone onFilesSelected={onFilesSelected} />);

    const input = container.querySelector('input[type="file"]') as HTMLInputElement;
    const txt = createFile('notes.txt', 'text/plain');

    fireEvent.change(input, { target: { files: [txt] } });

    expect(onFilesSelected).not.toHaveBeenCalled();
    expect(
      await screen.findByText('"notes.txt" is not a supported format. Accepted: PDF, JPEG, PNG, TIFF.'),
    ).toBeInTheDocument();
  });

  it('shows validation error for oversized file', async () => {
    const onFilesSelected = vi.fn();

    const { container } = render(<DropZone onFilesSelected={onFilesSelected} />);

    const input = container.querySelector('input[type="file"]') as HTMLInputElement;
    const hugePdf = createFile('large.pdf', 'application/pdf', 26 * 1024 * 1024);

    fireEvent.change(input, { target: { files: [hugePdf] } });

    expect(onFilesSelected).not.toHaveBeenCalled();
    expect(await screen.findByText('"large.pdf" exceeds 25 MB size limit.')).toBeInTheDocument();
  });
});
