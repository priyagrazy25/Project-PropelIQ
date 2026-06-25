import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ManualCodeOverrideInput } from './ManualCodeOverrideInput';

describe('ManualCodeOverrideInput', () => {
  afterEach(() => {
    cleanup();
  });

  it('shows required validation messages when submitting empty form', () => {
    const onSubmit = vi.fn();

    render(
      <ManualCodeOverrideInput
        open
        onOpenChange={vi.fn()}
        codeType="ICD-10"
        originalCode="E11.65"
        originalDescription="Type 2 diabetes mellitus with hyperglycemia"
        onSubmit={onSubmit}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Save Override' }));

    expect(screen.getByText('Code is required')).toBeInTheDocument();
    expect(screen.getByText('Description is required')).toBeInTheDocument();
    expect(screen.getByText('Please select a reason for override')).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('shows ICD-10 format error for invalid code', () => {
    render(
      <ManualCodeOverrideInput
        open
        onOpenChange={vi.fn()}
        codeType="ICD-10"
        originalCode="E11.65"
        originalDescription="Type 2 diabetes mellitus with hyperglycemia"
        onSubmit={vi.fn()}
      />,
    );

    fireEvent.change(screen.getByLabelText('ICD-10 Code *'), {
      target: { value: '12345' },
    });
    fireEvent.change(screen.getByLabelText('Description *'), {
      target: { value: 'Sufficiently long description' },
    });

    fireEvent.click(screen.getByRole('button', { name: 'Save Override' }));

    expect(
      screen.getByText('Invalid ICD-10 format. e.g., E11.65, I10, J45.909'),
    ).toBeInTheDocument();
  });

  it('calls onOpenChange(false) when cancel is clicked', () => {
    const onOpenChange = vi.fn();

    render(
      <ManualCodeOverrideInput
        open
        onOpenChange={onOpenChange}
        codeType="CPT"
        originalCode="99213"
        originalDescription="Office outpatient visit"
        onSubmit={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(onOpenChange).toHaveBeenCalledWith(false);
  });
});
