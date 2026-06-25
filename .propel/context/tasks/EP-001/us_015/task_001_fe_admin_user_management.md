---
post_title: "TASK_001 - Admin User Management UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-admin-user-management"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_015, frontend, React, admin, user-management, CRUD"
ai_note: "Generated with AI assistance from user story US_015"
summary: "Implement admin user management dashboard with CRUD operations, role assignment, status management, and data table with search/filter."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_ADMIN_USER_MANAGEMENT

## Requirement Reference
- User Story: us_015
- Story Location: .propel/context/tasks/EP-001/us_015/us_015.md
- Acceptance Criteria:
    - AC-1: Admin dashboard displays paginated user list with search and filter per FR-004
    - AC-2: Create user form with role selection (Patient/Staff/Admin)
    - AC-3: Edit user details and change roles inline
    - AC-4: Deactivate user accounts (soft-delete) with confirmation dialog
    - AC-5: Admin-only access enforced (non-admins see 403)
- Edge Cases:
    - Admin tries to deactivate their own account → action blocked with warning
    - Search returns no results → empty state with "No users found" message

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-023-admin-user-management.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-023 |
| **UXR Requirements** | UXR-601 |
| **Design Tokens** | .propel/context/docs/designsystem.md#tables, #forms |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

## Task Overview
Build the admin user management dashboard with a paginated data table of users, search/filter capabilities, create/edit user forms with role assignment, and account deactivation with confirmation dialog. The page is restricted to Admin role only.

## Dependent Tasks
- task_001_fe_react_scaffolding (US_001) — Requires React project
- task_001_fe_login_interface (US_013) — Requires ProtectedRoute with role check

## Impacted Components
- NEW: AdminUserManagement page and components
- NEW: UserTable, UserForm, DeactivateDialog components
- MODIFY: App router with admin-protected route

## Implementation Plan
1. Create AdminUserManagement page accessible only to Admin role
2. Create UserTable with pagination, search by name/email, filter by role/status
3. Create UserForm dialog for create and edit operations with role dropdown
4. Implement deactivation flow with confirmation dialog and success toast
5. Add skeleton loading states during data fetch per UXR-501
6. Implement toast notifications for CRUD success/failure
7. Handle self-deactivation prevention
8. Add responsive table-to-card layout for mobile per UXR-303

## Current Project State
```
[PLACEHOLDER - Updated after US_001, US_013 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/identity/pages/AdminUserManagementPage.tsx | Admin management page |
| CREATE | frontend/src/features/identity/components/UserTable.tsx | Paginated user data table |
| CREATE | frontend/src/features/identity/components/UserForm.tsx | Create/edit user form |
| CREATE | frontend/src/features/identity/components/DeactivateDialog.tsx | Confirmation dialog |
| CREATE | frontend/src/features/identity/api/adminApi.ts | Admin API service |
| MODIFY | frontend/src/App.tsx | Add admin-protected route |

## External References
- React Table patterns: https://react.dev/learn/rendering-lists
- React Dialog accessibility: https://www.w3.org/WAI/ARIA/apd/patterns/dialog-modal/

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] User table displays with pagination, search, and filter
- [x] Create user form adds new user with selected role
- [x] Edit user form updates details inline
- [x] Deactivate shows confirmation; success updates table
- [x] Non-admin access returns 403 / redirect
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-023

## Implementation Checklist
- [x] Create AdminUserManagement page with Admin role guard
- [x] Build UserTable with pagination, search, and role/status filters
- [x] Create UserForm dialog for create/edit with role dropdown
- [x] Implement deactivation with confirmation dialog and success toast
- [x] Add skeleton loading states and empty state handling
- [x] Block self-deactivation with inline warning
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-023 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
