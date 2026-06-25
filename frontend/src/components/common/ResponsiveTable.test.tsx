import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { ResponsiveTable, type ResponsiveTableColumn } from './ResponsiveTable';

type Item = {
  id: string;
  name: string;
  status: string;
};

const columns: ResponsiveTableColumn<Item>[] = [
  {
    key: 'name',
    header: 'Name',
    render: (item) => item.name,
  },
  {
    key: 'status',
    header: 'Status',
    render: (item) => item.status,
  },
];

const rows: Item[] = [
  { id: '1', name: 'Alice Carter', status: 'High' },
  { id: '2', name: 'Brian Shaw', status: 'Low' },
];

function setViewport(width: number) {
  Object.defineProperty(window, 'innerWidth', {
    configurable: true,
    writable: true,
    value: width,
  });
  window.dispatchEvent(new Event('resize'));
}

afterEach(() => {
  cleanup();
});

describe('ResponsiveTable', () => {
  it('renders table layout on desktop widths', () => {
    setViewport(1440);

    render(
      <ResponsiveTable
        data={rows}
        columns={columns}
        getRowKey={(item) => item.id}
        ariaLabel="Patient risk table"
      />,
    );

    expect(screen.getByRole('grid', { name: 'Patient risk table' })).toBeInTheDocument();
    expect(screen.getByText('Alice Carter')).toBeInTheDocument();
    expect(screen.queryAllByLabelText('Row details')).toHaveLength(0);
  });

  it('renders card layout on mobile widths', () => {
    setViewport(375);

    render(
      <ResponsiveTable
        data={rows}
        columns={columns}
        getRowKey={(item) => item.id}
        ariaLabel="Patient risk table"
      />,
    );

    expect(screen.queryByRole('grid', { name: 'Patient risk table' })).not.toBeInTheDocument();
    expect(screen.getAllByLabelText('Row details')).toHaveLength(2);
    expect(screen.getByText('Alice Carter')).toBeInTheDocument();
    expect(screen.getByText('Brian Shaw')).toBeInTheDocument();
  });
});
