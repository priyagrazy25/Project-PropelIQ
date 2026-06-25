import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { ConflictBanner, ConflictWarningBanner } from './ConflictBanner';

describe('ConflictBanner', () => {
  afterEach(() => {
    cleanup();
  });

  it('renders critical conflict summary and resolve link for high severity conflicts', () => {
    render(
      <MemoryRouter>
        <ConflictBanner
          conflicts={[
            {
              conflictId: 'c1',
              field: 'Allergy',
              category: 'allergy',
              values: ['Penicillin', 'None'],
              severity: 'high',
            },
          ]}
        />
      </MemoryRouter>,
    );

    expect(screen.getByText('1 Critical Conflict Detected')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Resolve critical conflicts now' })).toHaveAttribute(
      'href',
      '/clinical/conflicts/c1',
    );
  });

  it('renders nothing when there are no critical conflicts', () => {
    const { container } = render(
      <MemoryRouter>
        <ConflictBanner
          conflicts={[
            {
              conflictId: 'c2',
              field: 'Address',
              category: 'history',
              values: ['A', 'B'],
              severity: 'medium',
            },
          ]}
        />
      </MemoryRouter>,
    );

    expect(container).toBeEmptyDOMElement();
  });
});

describe('ConflictWarningBanner', () => {
  afterEach(() => {
    cleanup();
  });

  it('renders warning conflict count and resolve link for medium severity conflicts', () => {
    render(
      <MemoryRouter>
        <ConflictWarningBanner
          conflicts={[
            {
              conflictId: 'c3',
              field: 'Medication',
              category: 'medication',
              values: ['5mg', '10mg'],
              severity: 'medium',
            },
          ]}
        />
      </MemoryRouter>,
    );

    expect(screen.getByText(/1 data conflict detected/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Resolve now' })).toHaveAttribute(
      'href',
      '/clinical/conflicts/c3',
    );
  });
});
