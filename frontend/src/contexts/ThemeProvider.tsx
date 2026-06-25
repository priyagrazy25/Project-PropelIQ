import { createContext, useContext, useMemo, type ReactNode } from 'react';

export interface ThemeTokens {
  primary: string;
  success: string;
  warning: string;
  destructive: string;
  info: string;
  focusRingWidth: string;
  focusRingOffset: string;
  minTouchTarget: string;
}

interface ThemeContextValue {
  tokens: ThemeTokens;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

const defaultTokens: ThemeTokens = {
  primary: 'var(--primary)',
  success: 'var(--success)',
  warning: 'var(--warning)',
  destructive: 'var(--destructive)',
  info: 'var(--info)',
  focusRingWidth: 'var(--focus-ring-width)',
  focusRingOffset: 'var(--focus-ring-offset)',
  minTouchTarget: 'var(--min-touch-target)',
};

export function ThemeProvider({ children }: { children: ReactNode }) {
  const value = useMemo<ThemeContextValue>(
    () => ({
      tokens: defaultTokens,
    }),
    [],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useThemeTokens() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useThemeTokens must be used within ThemeProvider');
  }
  return context.tokens;
}
