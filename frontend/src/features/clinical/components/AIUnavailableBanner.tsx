import { Button } from '@/components/ui/button';
import { AlertTriangle } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export function AIUnavailableBanner() {
  const navigate = useNavigate();

  return (
    <div
      role="alert"
      className="flex items-center gap-2 bg-(--surface-warning) px-6 py-3 text-sm text-amber-700"
    >
      <AlertTriangle className="size-4 shrink-0" aria-hidden="true" />
      <span>
        AI intake is temporarily unavailable. You can continue with the manual
        form.
      </span>
      <Button
        variant="outline"
        size="sm"
        className="ml-auto shrink-0"
        onClick={() => void navigate('/intake/manual')}
      >
        Switch to Manual
      </Button>
    </div>
  );
}
