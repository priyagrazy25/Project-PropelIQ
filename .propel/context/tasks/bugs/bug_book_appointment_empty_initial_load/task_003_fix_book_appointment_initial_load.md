---
post_title: "BUG_003 - Book Appointment Page Shows Empty Grid on Initial Load"
author1: "AI Senior Developer"
post_slug: "bug-003-fix-book-appointment-initial-load"
categories: "Healthcare, Bug Fix"
tags: "BUG-003, scheduling, frontend, React, ProviderSearchPage"
ai_note: "Generated from bug report: book appointment dashboard empty on initial load; should show earliest available slots"
summary: "The Book Appointment page (ProviderSearchPage) renders an empty grid on initial load because no search is triggered automatically. Users must manually click Search before any providers or slots appear. The page should auto-load all providers with the earliest available slots on first mount."
post_date: "2026-06-24"
---

# Bug Fix Task - BUG_003

## Bug Report Reference
- **Bug ID**: BUG-003
- **Source**: Manual observation — book appointment page is blank on initial load
- **Reported**: 2026-06-24

---

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Poor UX — page appears broken to users; primary booking flow is unusable until search is triggered
- **Affected Version**: Current (frontend main branch)
- **Environment**: Web browser, all platforms, Development environment

### Steps to Reproduce
1. Log in as a patient or staff user
2. Navigate to **Book Appointment** (`/book` or via the dashboard button)
3. **Expected**: Provider cards with earliest available time slots are shown immediately
4. **Actual**: The page shows only the search filter bar with no provider results; the grid body is empty

**Error Output**:
```text
No runtime error. The provider grid is simply empty because hasSearched=false
and no automatic search is triggered on mount.
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/pages/ProviderSearchPage.tsx`
- **Component**: Book Appointment / Provider Search
- **Cause**: `hasSearched` starts as `false`. The providers grid is gated behind the user clicking the **Search** button:
  ```ts
  // Line 44
  const [hasSearched, setHasSearched] = useState(false);

  // No useEffect triggers executeSearch on mount — only handleSearch (button click) does
  const handleSearch = useCallback(() => {
    executeSearch(1);   // Only called when user clicks Search
  }, [executeSearch]);
  ```
  There is no `useEffect` that fires `executeSearch(1)` on component mount. Without it the
  page waits indefinitely for user interaction before loading any data.

### Impact Assessment
- **Affected Features**: Book Appointment page — initial provider grid
- **User Impact**: Every user landing on the booking page sees a blank list; must manually click Search to see any providers — adds unnecessary friction to the primary scheduling flow
- **Data Integrity Risk**: None
- **Security Implications**: None

---

## Fix Overview

Add a `useEffect` with an empty dependency array that fires `executeSearch(1)` on mount.
The page already holds an `executeSearchRef` for stale-closure-safe access to the latest
`executeSearch` callback — the initial load should go through that ref to remain consistent
with the existing pattern.

---

## Fix Dependencies
- No external dependencies
- No backend changes required (frontend-only fix)

---

## Impacted Components

### Frontend (React / TypeScript)
- `frontend/src/features/scheduling/pages/ProviderSearchPage.tsx` — MODIFY

---

## Expected Changes

| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | `frontend/src/features/scheduling/pages/ProviderSearchPage.tsx` | Add `useEffect` on mount to trigger initial provider search with default filters (today, earliest sort) |

---

## Implementation Plan

**`ProviderSearchPage.tsx`** — add one `useEffect` after the `executeSearchRef` setup block:

```ts
// Existing ref setup (already present — do NOT change)
const executeSearchRef = useRef(executeSearch);
useEffect(() => {
  executeSearchRef.current = executeSearch;
}, [executeSearch]);

// ADD — trigger initial load on mount
useEffect(() => {
  executeSearchRef.current(1);
}, []); // empty deps → runs once on mount only
```

**Why `executeSearchRef` instead of `executeSearch` directly?**
Using the ref avoids adding `executeSearch` to the dependency array (which would re-fire on
every filter keystroke). The ref is synchronously initialised by `useRef(executeSearch)` on
the first render, so it holds the correct function when the mount effect runs.

**Default behaviour on initial load:**
- No filters set (`name`, `specialty`, `location`, `date` all empty)
- Backend defaults the date to `DateTime.UtcNow` → today's future slots
- `sortBy = 'earliest'` → providers ordered by next available slot
- Returns up to `PAGE_SIZE = 20` providers with their earliest slots

---

## Regression Prevention Strategy
- [ ] Unit/integration test: `ProviderSearchPage` calls `searchProviders` on mount without user interaction
- [ ] Unit test: provider grid renders skeleton loaders on mount before data arrives
- [ ] Unit test: provider cards render after initial load resolves
- [ ] Manual: navigating to `/book` shows providers immediately without pressing Search

---

## Rollback Procedure
1. Remove the mount `useEffect` from `ProviderSearchPage.tsx`
2. The page returns to the search-button-only pattern (restores the bug but unblocks the page)

---

## External References
- `searchProviders` — `schedulingApi.ts`; accepts optional `name`, `specialty`, `location`, `date` — omitting all defaults to today's UTC available slots
- `executeSearchRef` — already defined in `ProviderSearchPage.tsx`; safe-ref pattern for stale closure avoidance
- `PAGE_SIZE = 20` — results per page constant already defined in `ProviderSearchPage.tsx`

---

## Build Commands
```bash
# Frontend (from /frontend)
npm run dev        # start dev server
npm run build      # production build
npm run lint       # ESLint check
```

---

## Implementation Validation Strategy
- [x] Navigating to Book Appointment shows provider cards immediately on page load
- [x] Skeleton loaders are visible during the initial fetch
- [x] Providers are sorted by earliest available slot
- [x] Slot time buttons are visible on each provider card
- [x] Search button still works for filtered searches after initial load

---

## Implementation Checklist
- [x] Add mount `useEffect` calling `executeSearchRef.current(1)` in `ProviderSearchPage.tsx`
- [x] Modify `SearchProvidersQueryHandler.cs` — when no date is specified, expand window to next 7 days from `DateTime.UtcNow` so results always appear regardless of time of day
- [x] Verify skeleton loaders appear during initial fetch
- [x] Verify providers with earliest available slots render after load
- [x] Verify Search button re-queries correctly with filter values applied
