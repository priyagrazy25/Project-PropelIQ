import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Bell, PanelLeftOpen, Plus } from 'lucide-react';
import { NavLink } from 'react-router-dom';

interface AppHeaderProps {
  title: string;
  homeTo: string;
  initials: string;
  onToggleSidebar?: () => void;
  showSidebarToggle?: boolean;
}

export function AppHeader({
  title,
  homeTo,
  initials,
  onToggleSidebar,
  showSidebarToggle = false,
}: AppHeaderProps) {
  return (
    <header
      className="fixed top-0 left-0 right-0 h-16 bg-card border-b border-border flex items-center justify-between px-4 md:px-6 z-50 shadow-[var(--shadow-1)]"
      role="banner"
    >
      <div className="flex items-center gap-2">
        {showSidebarToggle && onToggleSidebar ? (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Toggle navigation"
            className="h-11 w-11"
            onClick={onToggleSidebar}
          >
            <PanelLeftOpen className="h-5 w-5" />
          </Button>
        ) : null}

        <NavLink
          to={homeTo}
          className="flex items-center gap-2 text-lg md:text-xl font-bold text-primary no-underline hover:text-primary/80 transition-colors"
        >
          <span className="w-8 h-8 bg-primary rounded-md flex items-center justify-center text-primary-foreground text-lg font-bold">
            <Plus className="h-5 w-5" />
          </span>
          <span className="truncate">{title}</span>
        </NavLink>
      </div>

      <div className="flex items-center gap-2 md:gap-4">
        <Button variant="ghost" size="icon" aria-label="Notifications" className="relative h-11 w-11">
          <Bell className="h-5 w-5 text-muted-foreground" />
        </Button>
        <Avatar className="h-9 w-9 cursor-pointer">
          <AvatarFallback className="bg-secondary text-secondary-foreground text-sm font-semibold">
            {initials}
          </AvatarFallback>
        </Avatar>
      </div>
    </header>
  );
}
