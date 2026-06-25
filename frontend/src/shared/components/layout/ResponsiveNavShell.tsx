import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';
import { ChevronLeft, ChevronRight, LogOut, Menu } from 'lucide-react';
import type { LucideProps } from 'lucide-react';
import { useCallback, useMemo, useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { AppHeader } from './AppHeader';
import { useBreakpoint } from '../../hooks/useBreakpoint';

export interface NavItem {
  to: string;
  icon: React.ComponentType<LucideProps>;
  label: string;
  end?: boolean;
}

interface ResponsiveNavShellProps {
  appTitle: string;
  homeTo: string;
  navAriaLabel: string;
  navItems: NavItem[];
  initials: string;
  onLogout: () => void;
}

export function ResponsiveNavShell({
  appTitle,
  homeTo,
  navAriaLabel,
  navItems,
  initials,
  onLogout,
}: ResponsiveNavShellProps) {
  const { isMobile, isTablet, isDesktop } = useBreakpoint();
  const [isTabletCollapsed, setIsTabletCollapsed] = useState(true);
  const [isMoreOpen, setIsMoreOpen] = useState(false);

  const isCollapsed = isTablet && isTabletCollapsed;

  const sidebarWidth = isDesktop ? 240 : isCollapsed ? 64 : 240;
  const showSidebar = isDesktop || isTablet;

  const primaryMobileItems = useMemo(() => navItems.slice(0, 4), [navItems]);
  const extraMobileItems = useMemo(() => navItems.slice(4), [navItems]);

  const toggleTablet = useCallback(() => {
    setIsTabletCollapsed((prev) => !prev);
  }, []);

  return (
    <div className="min-h-screen overflow-x-hidden bg-[var(--surface)]">
      <AppHeader
        title={appTitle}
        homeTo={homeTo}
        initials={initials}
        showSidebarToggle={isTablet}
        onToggleSidebar={toggleTablet}
      />

      {showSidebar ? (
        <nav
          className="fixed top-16 left-0 bottom-0 bg-card border-r border-border py-4 overflow-y-auto z-40 flex flex-col transition-[width] duration-200"
          style={{ width: `${sidebarWidth}px` }}
          role="navigation"
          aria-label={navAriaLabel}
        >
          {isTablet ? (
            <div className="px-2 pb-2">
              <Button
                type="button"
                variant="ghost"
                className="h-11 w-11"
                aria-label={isTabletCollapsed ? 'Expand navigation' : 'Collapse navigation'}
                onClick={toggleTablet}
              >
                {isTabletCollapsed ? <ChevronRight className="h-5 w-5" /> : <ChevronLeft className="h-5 w-5" />}
              </Button>
            </div>
          ) : null}

          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 px-4 py-3 text-sm font-medium text-muted-foreground no-underline border-l-[3px] border-transparent transition-colors duration-[var(--duration-micro)] hover:bg-muted min-h-11',
                  isActive && 'text-primary bg-accent border-l-primary font-semibold',
                  isCollapsed && 'justify-center px-2',
                )
              }
              title={isCollapsed ? item.label : undefined}
              aria-label={item.label}
            >
              <item.icon className="h-5 w-5 shrink-0" aria-hidden="true" />
              {!isCollapsed ? <span>{item.label}</span> : null}
            </NavLink>
          ))}

          <Separator className="my-2" />

          <button
            type="button"
            className={cn(
              'mt-auto flex items-center gap-3 px-4 py-3 text-sm font-medium text-muted-foreground border-l-[3px] border-transparent bg-transparent cursor-pointer w-full text-left hover:bg-muted transition-colors duration-[var(--duration-micro)] min-h-11',
              isCollapsed && 'justify-center px-2',
            )}
            onClick={onLogout}
            aria-label="Sign Out"
            title={isCollapsed ? 'Sign Out' : undefined}
          >
            <LogOut className="h-5 w-5 shrink-0" aria-hidden="true" />
            {!isCollapsed ? <span>Sign Out</span> : null}
          </button>
        </nav>
      ) : null}

      <main
        className={cn('mt-16 p-4 md:p-8 min-h-[calc(100vh-4rem)]', isMobile ? 'pb-24' : '')}
        style={showSidebar ? { marginLeft: `${sidebarWidth}px` } : undefined}
        role="main"
      >
        <Outlet />
      </main>

      {isMobile ? (
        <>
          <nav
            className="fixed bottom-0 left-0 right-0 h-16 bg-card border-t border-border z-50 grid grid-cols-5"
            aria-label="Bottom navigation"
          >
            {primaryMobileItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  cn(
                    'flex flex-col items-center justify-center gap-1 text-[11px] min-h-11 text-muted-foreground no-underline',
                    isActive && 'text-primary',
                  )
                }
                aria-label={item.label}
              >
                <item.icon className="h-4 w-4" aria-hidden="true" />
                <span className="truncate max-w-14">{item.label}</span>
              </NavLink>
            ))}

            <button
              type="button"
              className="flex flex-col items-center justify-center gap-1 text-[11px] min-h-11 text-muted-foreground bg-transparent border-0"
              onClick={() => setIsMoreOpen((prev) => !prev)}
              aria-label="More navigation options"
            >
              <Menu className="h-4 w-4" aria-hidden="true" />
              <span>More</span>
            </button>
          </nav>

          {isMoreOpen ? (
            <div
              className="fixed inset-0 z-[60] bg-black/30"
              onClick={() => setIsMoreOpen(false)}
            >
              <div
                className="absolute bottom-16 left-0 right-0 bg-card border-t border-border rounded-t-xl p-3 shadow-[var(--shadow-4)]"
                onClick={(e) => e.stopPropagation()}
                role="dialog"
                aria-label="More navigation menu"
              >
                <div className="grid grid-cols-2 gap-2">
                  {extraMobileItems.map((item) => (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      onClick={() => setIsMoreOpen(false)}
                      className="flex items-center gap-2 rounded-md border border-border px-3 py-3 text-sm text-foreground no-underline min-h-11"
                      aria-label={item.label}
                    >
                      <item.icon className="h-4 w-4" aria-hidden="true" />
                      <span>{item.label}</span>
                    </NavLink>
                  ))}

                  <button
                    type="button"
                    className="col-span-2 flex items-center gap-2 rounded-md border border-border px-3 py-3 text-sm text-foreground bg-transparent min-h-11"
                    onClick={onLogout}
                    aria-label="Sign Out"
                  >
                    <LogOut className="h-4 w-4" aria-hidden="true" />
                    <span>Sign Out</span>
                  </button>
                </div>
              </div>
            </div>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
