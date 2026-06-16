export function TypingIndicator() {
  return (
    <div
      aria-label="AI is typing"
      className="flex gap-1 self-start rounded-xl rounded-bl-sm bg-muted px-4 py-3"
    >
      <span className="size-2 animate-[blink_1.4s_infinite_both] rounded-full bg-muted-foreground" />
      <span className="size-2 animate-[blink_1.4s_0.2s_infinite_both] rounded-full bg-muted-foreground" />
      <span className="size-2 animate-[blink_1.4s_0.4s_infinite_both] rounded-full bg-muted-foreground" />
    </div>
  );
}
