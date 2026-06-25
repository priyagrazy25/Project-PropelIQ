import { cn } from '@/lib/utils';
import { useBreakpoint } from '@/shared/hooks/useBreakpoint';
import type { ReactNode } from 'react';

export interface ResponsiveTableColumn<T> {
  key: string;
  header: string;
  render: (item: T) => ReactNode;
  className?: string;
  cardLabel?: string;
}

interface ResponsiveTableProps<T> {
  data: T[];
  columns: ResponsiveTableColumn<T>[];
  getRowKey: (item: T) => string;
  ariaLabel: string;
  emptyState?: ReactNode;
  className?: string;
}

export function ResponsiveTable<T>({
  data,
  columns,
  getRowKey,
  ariaLabel,
  emptyState,
  className,
}: ResponsiveTableProps<T>) {
  const { isMobile } = useBreakpoint();

  if (data.length === 0) {
    return emptyState ?? null;
  }

  if (isMobile) {
    return (
      <div className={cn('grid gap-3', className)} aria-label={ariaLabel}>
        {data.map((item) => (
          <article
            key={getRowKey(item)}
            className="rounded-lg border border-border bg-card p-4 shadow-[var(--shadow-1)]"
            aria-label="Row details"
          >
            <dl className="space-y-2">
              {columns.map((column) => (
                <div key={column.key} className="grid grid-cols-[112px_1fr] gap-2">
                  <dt className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    {column.cardLabel ?? column.header}
                  </dt>
                  <dd className={cn('text-sm text-foreground break-words', column.className)}>
                    {column.render(item)}
                  </dd>
                </div>
              ))}
            </dl>
          </article>
        ))}
      </div>
    );
  }

  return (
    <div className={cn('overflow-x-auto', className)}>
      <table className="w-full" role="grid" aria-label={ariaLabel}>
        <thead>
          <tr className="bg-gray-50 border-b border-gray-200">
            {columns.map((column) => (
              <th
                key={column.key}
                className="text-left text-xs font-semibold uppercase tracking-wider text-gray-500 px-4 py-3"
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((item) => (
            <tr
              key={getRowKey(item)}
              className="border-b border-gray-100 hover:bg-gray-50 transition-colors"
            >
              {columns.map((column) => (
                <td key={column.key} className={cn('px-4 py-3', column.className)}>
                  {column.render(item)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
