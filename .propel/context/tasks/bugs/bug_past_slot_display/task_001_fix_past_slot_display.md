---
post_title: "BUG_001 - Past Time and Date Shown in Booking Card"
author1: "AI Senior Developer"
post_slug: "bug-001-fix-past-slot-display"
categories: "Healthcare, Bug Fix"
tags: "BUG-001, scheduling, frontend, React, SlotGrid, BookingConfirmationCard"
ai_note: "Generated from bug report: past time and date visible in booking card"
summary: "Slot picker components do not filter out past slots, allowing users to select and book appointments in the past, which are then displayed in the BookingConfirmationCard."
post_date: "2026-06-24"
---

# Bug Fix Task - BUG_001

## Bug Report Reference
- **Bug ID**: BUG-001
- **Source**: Manual observation — booking card shows past date/time after slot selection
- **Reported**: 2026-06-24

---

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Core scheduling feature broken — users can book and confirm past appointments
- **Affected Version**: Current (frontend main branch)
- **Environment**: Web browser, all platforms, Development environment

### Steps to Reproduce
1. Navigate to the appointment booking page as a patient or staff user
2. Browse available slots — slots from earlier today or previous days may appear as selectable
3. Select a past time slot (one whose `startTime` is before the current time)
4. Proceed through booking confirmation
5. **Expected**: Past slots should not be selectable; only future slots should be shown
6. **Actual**: Past time slots appear as available and can be selected; the `BookingConfirmationCard` displays the past date and time

**Error Output**:
```text
No runtime error. The BookingConfirmationCard renders the past slotStartTime/slotEndTime values
without any validation, e.g.:
  Date: June 20, 2026
  Time: 9:00 AM – 9:30 AM  (slot that has already passed)
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/components/SlotGrid.tsx` — line 27
- **File**: `frontend/src/features/scheduling/components/SlotSelectionPanel.tsx` — line 33
- **Component**: Scheduling / Slot Selection
- **Function**: `availableSlots` filter in both `SlotGrid` and `SlotSelectionPanel`
- **Cause**: Both components filter slots using only `s.isAvailable`, with no check that `s.startTime` is in the future. Slots marked `isAvailable: true` by the API with a `startTime` that has already passed are rendered and made selectable. The `BookingConfirmationCard` then faithfully displays whatever `slotStartTime`/`slotEndTime` values are on the booking response — including past ones.

### Impact Assessment
- **Affected Features**: Appointment booking flow, slot selection, booking confirmation card
- **User Impact**: Patients and staff can inadvertently book past appointments; confirmation card shows past date/time causing confusion
- **Data Integrity Risk**: Yes — appointments with past slot times can be persisted to the database
- **Security Implications**: None

---

## Fix Overview

Add a `startTime > now` guard to the slot filtering logic in both `SlotGrid` and `SlotSelectionPanel`. Slots whose `startTime` is before the current moment should be excluded from the selectable list, regardless of their `isAvailable` flag.

---

## Fix Dependencies
- No external dependencies
- No backend changes required (frontend-only fix)

---

## Impacted Components

### Frontend (React / TypeScript)
- `frontend/src/features/scheduling/components/SlotGrid.tsx` — MODIFY
- `frontend/src/features/scheduling/components/SlotSelectionPanel.tsx` — MODIFY

---

## Expected Changes

| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | `frontend/src/features/scheduling/components/SlotGrid.tsx` | Change `availableSlots` filter to also exclude slots where `startTime < Date.now()` |
| MODIFY | `frontend/src/features/scheduling/components/SlotSelectionPanel.tsx` | Same past-slot guard on the `availableSlots` filter |

---

## Implementation Plan

1. **SlotGrid.tsx** — Update the `availableSlots` filter:
   ```ts
   // Before
   const availableSlots = slots.filter((s) => s.isAvailable);

   // After
   const now = new Date();
   const availableSlots = slots.filter(
     (s) => s.isAvailable && new Date(s.startTime) > now
   );
   ```

2. **SlotSelectionPanel.tsx** — Apply the same change:
   ```ts
   // Before
   const availableSlots = slots.filter((s) => s.isAvailable);

   // After
   const now = new Date();
   const availableSlots = slots.filter(
     (s) => s.isAvailable && new Date(s.startTime) > now
   );
   ```

3. Verify both components show "No slots available" when all remaining slots are in the past.

---

## Regression Prevention Strategy
- [ ] Unit test: `SlotGrid` renders no slots when all `startTime` values are in the past
- [ ] Unit test: `SlotGrid` renders only future slots when mix of past and future slots is provided
- [ ] Unit test: `SlotSelectionPanel` applies the same past-slot guard
- [ ] Integration test: booking flow cannot reach `BookingConfirmationCard` with a past `slotStartTime`

---

## Rollback Procedure
1. Revert the `availableSlots` filter lines in both files to the original `s.isAvailable`-only check
2. Verify slots render again (regression would restore the bug but unblock the booking flow)

---

## External References
- `BookingConfirmationCard.tsx` — displays `booking.slotStartTime` / `booking.slotEndTime` (no change needed; root fix is upstream)
- `schedulingApi.ts` — `ProviderSlot.startTime` is an ISO string field

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
- [x] Select only future-dated slots are shown in the slot picker
- [x] Past slots do not appear in `SlotGrid` or `SlotSelectionPanel`
- [x] "No slots available" message is shown when all slots for a date are in the past
- [x] `BookingConfirmationCard` never shows a past date/time after the fix

---

## Implementation Checklist
- [x] Modify `SlotGrid.tsx` — add `new Date(s.startTime) > now` guard
- [x] Modify `SlotSelectionPanel.tsx` — add `new Date(s.startTime) > now` guard
- [x] Modify `SearchProvidersQueryHandler.cs` — use `DateTime.UtcNow` as `startCutoff` for today's slots
- [x] Modify `GetProviderSlotsQueryHandler.cs` — same `startCutoff` logic
- [x] Verify "No slots available" fallback renders correctly when all past
- [ ] Run `npm run lint` — no new lint errors
- [ ] Run `npm run build` — build passes
- [ ] Manually verify booking flow with dev server
