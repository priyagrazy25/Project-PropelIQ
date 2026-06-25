---
post_title: "BUG_001 - Patient Search Not Working in Walk-In Booking"
author1: "AI QA Engineer"
post_slug: "bug-001-patient-search-walkin-booking"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Bug Fix"
tags: "EP-003, US_021, bug, frontend, backend, walk-in, patient-search"
ai_note: "Bug report created from observed defect in Walk-In Booking patient search flow"
summary: "Patient search in the Walk-In Booking page fails to return results or errors silently, preventing staff from finding existing patients."
post_date: "2026-06-22"
---

# Bug Fix Task - [BUG_001]

## Bug Report Reference
- Bug ID: BUG_001
- Source: Walk-In Booking page — patient search component (SCR-019)
- Related User Story: US_021
- Related Tasks: task_001_fe_walkin_booking.md, task_002_be_walkin_api.md

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Functional — staff cannot complete walk-in bookings for existing patients
- **Affected Version**: Current (main branch)
- **Environment**: All environments; reproducible on localhost dev stack

### Steps to Reproduce
1. Log in as a Staff user (`admin@upap.com` / `Admin@123` in dev)
2. Navigate to **Walk-In Booking** (`/staff/walk-in` or `/management/walk-in`)
3. In the **Patient Search** field, type at least 2 characters (e.g., a patient name, phone, or MRN)
4. **Expected**: Matching patient results appear in the dropdown within the debounce window (350ms)
5. **Actual**: No results are returned — either the list stays empty, a generic error is shown, or the request silently fails

**Error Output**:
```text
[Possible outcomes]
- Empty state: "No patients found." with no network request visible
- API error: "Patient search failed." with HTTP 4xx/5xx from /api/scheduling/walk-in/patients/search
- Network error: "Unable to connect. Please check your network."
- MRN field always blank in results (backend does not return MRN in search response)
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/api/schedulingApi.ts:608`
- **Component**: `PatientSearchBar` → `searchPatients` API call
- **Function**: `searchPatients(query, signal)`
- **Cause (multi-hypothesis)**:
  1. **Backend endpoint missing or not reachable** — `GET /api/scheduling/walk-in/patients/search?q={query}` may return 404 if the `WalkInController` patient search route is not registered or mapped correctly.
  2. **Authorization failure** — endpoint requires Staff role; JWT token may be missing, expired, or not carrying the correct claim, causing a 401/403 silently swallowed by the API handler.
  3. **Response shape mismatch** — the API normalizer in `schedulingApi.ts` handles both `{ value: [...] }` (OData) and plain array shapes; if the backend returns an unexpected structure the map will produce empty results.
  4. **MRN not returned** — `mrn` is hard-coded to `''` because the backend search projection does not include it; this is a known gap noted inline in the code.
  5. **Query length gate** — `PatientSearchBar` requires `query.trim().length >= 2` before triggering search; if input has leading/trailing whitespace the search may not fire.

### Impact Assessment
- **Affected Features**: Walk-In Booking (SCR-019), same-day queue enrollment (US_021 AC-2)
- **User Impact**: Staff cannot look up existing patients; forced to create duplicate records or abandon booking
- **Data Integrity Risk**: Yes — duplicate patient records may be created if staff work around the bug using "Create New Patient"
- **Security Implications**: None directly; endpoint already enforces Staff role

## Fix Overview
Diagnose whether the failure is a backend routing/auth issue or a frontend data-mapping issue. Patch the specific layer (or both) and add MRN to the search projection so the field is no longer hard-coded to blank.

## Fix Dependencies
- Backend `WalkInController` must expose `GET /api/scheduling/walk-in/patients/search`
- Staff JWT must include a valid role claim passed through `authenticatedFetch`
- EF query in patient search must project `MedicalRecordNumber` (MRN)

## Impacted Components
### Frontend
- `frontend/src/features/scheduling/api/schedulingApi.ts` — MODIFY `searchPatients` response mapping to handle MRN and validate response shape
- `frontend/src/features/scheduling/components/PatientSearchBar.tsx` — VERIFY minimum query length and debounce logic; add better error messaging

### Backend
- `backend/src/Modules/Scheduling/Scheduling.API/Controllers/WalkInController.cs` — VERIFY patient search route is registered and returns correct shape including MRN
- `backend/src/Modules/Scheduling/Scheduling.Application/` — VERIFY patient search query/handler projects MRN field

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | `backend/src/Modules/Scheduling/Scheduling.API/Controllers/WalkInController.cs` | Confirm/add `GET walk-in/patients/search` endpoint returning MRN in projection |
| MODIFY | `backend/src/Modules/Scheduling/Scheduling.Application/WalkIn/Queries/SearchPatients*.cs` | Add `MedicalRecordNumber` to result DTO and EF projection |
| MODIFY | `frontend/src/features/scheduling/api/schedulingApi.ts` | Map `mrn` from backend response; remove hard-coded `''` fallback |
| MODIFY | `frontend/src/features/scheduling/components/PatientSearchBar.tsx` | Improve error state messaging; validate debounce fires correctly |

> Only list concrete, verifiable file operations.

## Implementation Plan
1. Reproduce the bug in the running dev stack; capture the exact HTTP response from `/api/scheduling/walk-in/patients/search`
2. If 404 → fix backend route registration in `WalkInController`
3. If 401/403 → verify `authenticatedFetch` sends Bearer token and Staff role claim is present
4. If 200 but empty → inspect response shape vs. the dual-path normalizer logic in `schedulingApi.ts`
5. Add `MedicalRecordNumber` to backend DTO and EF projection
6. Update frontend mapping to consume `mrn` field
7. Run existing unit tests; add regression test for search returning MRN

## Regression Prevention Strategy
- [ ] Unit test: `searchPatients` correctly maps `mrn` from backend response
- [ ] Unit test: `PatientSearchBar` fires search on 2+ character input after debounce
- [ ] Integration test: `GET /api/scheduling/walk-in/patients/search?q=...` returns 200 with patient list including MRN for Staff role
- [ ] E2E test: Staff types patient name in Walk-In Booking → results appear → patient is selectable

## Rollback Procedure
1. Revert backend DTO/controller changes via `git revert`
2. Confirm `GET /api/scheduling/walk-in/patients/search` still responds (may return empty MRN)
3. Frontend mapping falls back to `mrn: ''` — no crash, just blank MRN field

## External References
- Wireframe: `.propel/context/wireframes/Hi-Fi/wireframe-SCR-019-walkin-booking.html`
- Screen Spec: `.propel/context/docs/figma_spec.md#SCR-019`
- User Story: `.propel/context/tasks/EP-003/us_021/us_021.md`
- Frontend Task: `.propel/context/tasks/EP-003/us_021/task_001_fe_walkin_booking.md`
- Backend Task: `.propel/context/tasks/EP-003/us_021/task_002_be_walkin_api.md`

## Build Commands
```bash
# Frontend
cd frontend && npm.cmd run dev

# Backend
$env:DOTNET_ROLL_FORWARD="Major"; dotnet run --project backend/src/Host/Host.csproj

# Frontend unit tests
cd frontend && npm.cmd run test:run

# Backend unit tests
$env:DOTNET_ROLL_FORWARD="Major"; dotnet test backend/tests/UnitTests/UnitTests.csproj
```

## Implementation Validation Strategy
- [x] Bug no longer reproducible: patient search returns results in Walk-In Booking
- [x] MRN field populated correctly in search results
- [x] All existing frontend Vitest tests pass
- [x] All existing backend unit tests pass
- [x] New regression tests pass

## Implementation Checklist
- [x] Reproduce and confirm root cause via HTTP inspection
- [x] Fix backend patient search endpoint (route + DTO + MRN projection)
- [x] Fix frontend `searchPatients` mapping for MRN
- [x] Verify `PatientSearchBar` debounce and minimum-length gate behave correctly
- [x] Write unit tests for API mapping and component search trigger
- [x] Write integration test for patient search endpoint
- [x] Validate end-to-end walk-in booking flow completes successfully
