import { cn } from '@/lib/utils';

export interface ChatBubbleProps {
  role: 'user' | 'ai' | 'system';
  content: string;
  timestamp?: string;
}

export function ChatBubble({ role, content, timestamp }: ChatBubbleProps) {
  if (role === 'system') {
    return (
      <div
        role="article"
        className={cn(
          'self-center rounded-full px-4 py-2',
          'bg-(--surface-info) text-primary text-[13px]',
        )}
      >
        {content}
      </div>
    );
  }

  const isAi = role === 'ai';
  const label = isAi ? 'AI' : 'You';

  return (
    <div
      role="article"
      className={cn(
        'max-w-[75%] rounded-xl px-4 py-3 text-sm leading-relaxed',
        isAi
          ? 'self-start rounded-bl-sm bg-muted text-foreground'
          : 'self-end rounded-br-sm bg-primary text-primary-foreground',
      )}
    >
      <div>{content}</div>
      {timestamp && (
        <div className="mt-1 text-[11px] opacity-60">
          {label} &middot; {timestamp}
        </div>
      )}
    </div>
  );
}
