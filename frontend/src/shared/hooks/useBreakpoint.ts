import { useEffect, useMemo, useState } from 'react';

export type Breakpoint = 'mobile' | 'tablet' | 'desktop';

const MOBILE_MAX = 767;
const TABLET_MAX = 1439;

function getWidth(): number {
  if (typeof window === 'undefined') {
    return 1440;
  }
  return window.innerWidth;
}

function getBreakpoint(width: number): Breakpoint {
  if (width <= MOBILE_MAX) {
    return 'mobile';
  }
  if (width <= TABLET_MAX) {
    return 'tablet';
  }
  return 'desktop';
}

export function useBreakpoint() {
  const [width, setWidth] = useState<number>(getWidth);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const onResize = () => setWidth(window.innerWidth);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  const breakpoint = useMemo(() => getBreakpoint(width), [width]);

  return {
    width,
    breakpoint,
    isMobile: breakpoint === 'mobile',
    isTablet: breakpoint === 'tablet',
    isDesktop: breakpoint === 'desktop',
    isCompactMobile: width <= 390,
  };
}
