---
post_title: "BUG_004 - View Profile Button on Provider Card Has No Functionality"
author1: "AI Senior Developer"
post_slug: "bug-004-fix-view-profile-button"
categories: "Healthcare, Bug Fix"
tags: "BUG-004, scheduling, frontend, React, ProviderCard, ProviderProfilePage"
ai_note: "Generated from bug report: View Profile button in ProviderCard does nothing when clicked"
summary: "The View Profile button rendered on every ProviderCard in the Book Appointment page has no onClick handler, no navigation logic, and no target route or page. Clicking it does nothing. A ProviderProfilePage must be created and the button must navigate to it."
post_date: "2026-06-25"
---

# Bug Fix Task - BUG_004

## Bug Report Reference
- **Bug ID**: BUG-004
- **Source**: Manual observation — View Profile button is inert on every provider card
- **Reported**: 2026-06-25

---

## Bug Summary

### Issue Classification
- **Priority**: Medium
- **Severity**: Feature gap — button is visible and implied to be interactive but does nothing
- **Affected Version**: Current (frontend main branch)
- **Environment**: Web browser, all platforms, Development environment

### Steps to Reproduce
1. Log in as a Patient and navigate to **Book Appointment** (`/search`)
2. Wait for provider cards to load
3. Click the **View Profile** button on any provider card
4. **Expected**: Navigate to a provider profile page showing the provider's full details (name, specialty, location, rating, accepting status, next available date)
5. **Actual**: Nothing happens — no navigation, no modal, no feedback

**Error Output**:
```text
No runtime error. The button renders but has no onClick handler:

// ProviderCard.tsx — current (broken)
<Button
  variant="outline"
  size="sm"
  className="border-[#1E6F9F] text-[#1E6F9F] hover:bg-blue-50"
>
  View Profile
</Button>
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/components/ProviderCard.tsx` — bottom of component
- **Missing**: `onClick` handler and `useNavigate` wiring on the View Profile button
- **Missing**: `/providers/:id` route in `frontend/src/App.tsx`
- **Missing**: `ProviderProfilePage` component

### Impact Assessment
- **Affected Features**: Book Appointment → provider card → View Profile button
- **User Impact**: Patients cannot view provider details before booking; CTA appears broken
- **Data Integrity Risk**: None
- **Security Implications**: None

---

## Fix Overview

1. Create `ProviderProfilePage` — displays full provider details passed via router state
2. Add `/providers/:id` route to `App.tsx` under the Patient shell
3. Wire the View Profile button in `ProviderCard` to `navigate('/providers/:id', { state: { provider } })`

---

## Fix Dependencies
- No backend changes required — all data is already available on the `ProviderResult` object
- No new API calls needed for the initial implementation (use data passed via router state)

---

## Impacted Components

### Frontend (React / TypeScript)
- `frontend/src/features/scheduling/components/ProviderCard.tsx` — MODIFY (add navigation)
- `frontend/src/features/scheduling/pages/ProviderProfilePage.tsx` — CREATE
- `frontend/src/App.tsx` — MODIFY (add route)

---

## Expected Changes

| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | `frontend/src/features/scheduling/components/ProviderCard.tsx` | Add `onViewProfile` prop and wire `onClick` on the View Profile button |
| CREATE | `frontend/src/features/scheduling/pages/ProviderProfilePage.tsx` | New page displaying provider details and available slot summary |
| MODIFY | `frontend/src/App.tsx` | Register `/providers/:id` route under the Patient `AppShell` |

---

## Implementation Plan

### 1. `ProviderCard.tsx` — add `onViewProfile` prop and wire button

```tsx
// Add to ProviderCardProps
onViewProfile?: (provider: ProviderResult) => void;

// Wire the button
<Button
  variant="outline"
  size="sm"
  className="border-[#1E6F9F] text-[#1E6F9F] hover:bg-blue-50"
  aria-label={`View profile for ${provider.fullName}`}
  onClick={() => onViewProfile?.(provider)}
>
  View Profile
</Button>
```

### 2. `ProviderSearchPage.tsx` — pass handler to `ProviderCard`

```tsx
const handleViewProfile = useCallback(
  (provider: ProviderResult) => {
    void navigate(`/providers/${provider.id}`, { state: { provider } });
  },
  [navigate],
);

// In render:
<ProviderCard
  provider={provider}
  onBookAppointment={handleBookAppointment}
  onJoinWaitlist={handleJoinWaitlist}
  onViewProfile={handleViewProfile}   // ← add
/>
```

### 3. `ProviderProfilePage.tsx` — new page

```tsx
import { useLocation, useNavigate, Navigate } from 'react-router-dom';
import type { ProviderResult } from '../api/schedulingApi';

interface ProfileLocationState {
  provider: ProviderResult;
}

export function ProviderProfilePage() {
  const location = useLocation();
  const navigate = useNavigate();
  const state = location.state as ProfileLocationState | null;

  if (!state?.provider) {
    return <Navigate to="/search" replace />;
  }

  const { provider } = state;

  return (
    <main className="max-w-2xl mx-auto space-y-6">
      {/* Back button, provider name, specialty, location, rating,
          accepting status, next available date, available slots summary */}
    </main>
  );
}
```

### 4. `App.tsx` — register route

```tsx
// Inside the Patient AppShell children array:
{ path: '/providers/:id', element: <ProviderProfilePage /> },
```

---

## Regression Prevention Strategy
- [ ] Unit test: clicking View Profile calls `onViewProfile` with the correct provider object
- [ ] Unit test: `ProviderProfilePage` renders provider name, specialty, location
- [ ] Unit test: `ProviderProfilePage` redirects to `/search` when accessed with no state
- [ ] Manual: clicking View Profile on a card navigates to the profile page
- [ ] Manual: Back button on the profile page returns to the search page

---

## Rollback Procedure
1. Remove the `onViewProfile` prop from `ProviderCard` and revert the button to have no handler
2. Remove the `/providers/:id` route from `App.tsx`
3. Delete `ProviderProfilePage.tsx`

---

## External References
- `ProviderResult` interface — `schedulingApi.ts` (id, fullName, specialty, location, rating, isAcceptingPatients, availableSlots, nextAvailableDate)
- Navigation pattern precedent — `BookingConfirmationPage` receives `{ provider, slot }` via `navigate('/booking/confirm', { state: ... })`
- Patient route shell — `App.tsx` lines 173–196 (children of the `AppShell` ProtectedRoute)

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
- [x] View Profile button is clickable on every provider card
- [x] Clicking navigates to `/providers/:id`
- [x] Profile page displays: full name, specialty, location, rating, accepting patients badge, next available date, slot summary
- [x] Accessing `/providers/:id` directly with no state redirects to `/search`
- [x] Back navigation returns to the search page

---

## Implementation Checklist
- [x] Add `onViewProfile` prop to `ProviderCardProps` and wire `onClick`
- [x] Add `handleViewProfile` callback in `ProviderSearchPage` and pass to both `ProviderCard` usages (virtual and non-virtual)
- [x] Create `ProviderProfilePage.tsx` with provider detail layout and safe state guard
- [x] Register `/providers/:id` route in `App.tsx` under the Patient `AppShell`
- [x] Verify navigation works end-to-end from card → profile → back to search
