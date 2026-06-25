import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import type { LucideProps } from 'lucide-react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ResponsiveNavShell, type NavItem } from './ResponsiveNavShell';

function DummyIcon(props: LucideProps) {
  return <svg {...props} viewBox="0 0 10 10" />;
}

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: DummyIcon },
  { to: '/search', label: 'Book Appointment', icon: DummyIcon },
  { to: '/intake', label: 'Intake', icon: DummyIcon },
  { to: '/documents', label: 'Documents', icon: DummyIcon },
  { to: '/health-profile', label: 'Health Profile', icon: DummyIcon },
  { to: '/waitlist', label: 'Waitlist', icon: DummyIcon },
];

function setViewport(width: number) {
  Object.defineProperty(window, 'innerWidth', {
    configurable: true,
    writable: true,
    value: width,
  });
  window.dispatchEvent(new Event('resize'));
}

function renderShell(initialPath = '/dashboard') {
  const onLogout = vi.fn();

  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route
          path="/"
          element={
            <ResponsiveNavShell
              appTitle="Unified Patient Access"
              homeTo="/dashboard"
              navAriaLabel="Sidebar navigation"
              navItems={navItems}
              initials="UP"
              onLogout={onLogout}
            />
          }
        >
          <Route path="dashboard" element={<div>Dashboard Page</div>} />
          <Route path="search" element={<div>Search Page</div>} />
          <Route path="intake" element={<div>Intake Page</div>} />
          <Route path="documents" element={<div>Documents Page</div>} />
          <Route path="health-profile" element={<div>Profile Page</div>} />
          <Route path="waitlist" element={<div>Waitlist Page</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );

  return { onLogout };
}

afterEach(() => {
  cleanup();
});

describe('ResponsiveNavShell', () => {
  it('renders bottom navigation on mobile with touch-target classes', async () => {
    setViewport(375);
    renderShell('/dashboard');

    expect(
      screen.getByRole('navigation', { name: 'Bottom navigation' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('navigation', { name: 'Sidebar navigation' }),
    ).not.toBeInTheDocument();

    const dashboardLink = screen.getByRole('link', { name: 'Dashboard' });
    expect(dashboardLink).toHaveClass('min-h-11');

    fireEvent.click(screen.getByRole('button', { name: 'More navigation options' }));
    expect(
      await screen.findByRole('dialog', { name: 'More navigation menu' }),
    ).toBeInTheDocument();
    expect(await screen.findByRole('link', { name: 'Health Profile' })).toBeInTheDocument();
  });

  it('renders collapsible sidebar on tablet', () => {
    setViewport(1024);
    renderShell('/dashboard');

    expect(
      screen.getByRole('navigation', { name: 'Sidebar navigation' }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('navigation', { name: 'Bottom navigation' }),
    ).not.toBeInTheDocument();

    const expandButton = screen.getByRole('button', { name: 'Expand navigation' });
    fireEvent.click(expandButton);

    expect(screen.getByRole('button', { name: 'Collapse navigation' })).toBeInTheDocument();
    expect(screen.getByText('Book Appointment')).toBeInTheDocument();
  });

  it('renders full sidebar on desktop without tablet toggle', () => {
    setViewport(1600);
    renderShell('/search');

    expect(
      screen.getByRole('navigation', { name: 'Sidebar navigation' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Book Appointment')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Expand navigation' }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'Collapse navigation' }),
    ).not.toBeInTheDocument();
  });
});
