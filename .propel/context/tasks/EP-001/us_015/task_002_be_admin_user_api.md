---
post_title: "TASK_002 - Admin User Management API"
author1: "AI Senior Developer"
post_slug: "task-002-be-admin-user-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_015, backend, ASP.NET Core, admin, user-management, RBAC"
ai_note: "Generated with AI assistance from user story US_015"
summary: "Implement admin user management API endpoints for CRUD operations, role assignment, and account deactivation with audit logging."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_ADMIN_USER_API

## Requirement Reference
- User Story: us_015
- Story Location: .propel/context/tasks/EP-001/us_015/us_015.md
- Acceptance Criteria:
    - AC-1: GET /api/admin/users returns paginated user list with search/filter
    - AC-2: POST /api/admin/users creates user with assigned role
    - AC-3: PUT /api/admin/users/{id} updates user details and role
    - AC-4: DELETE /api/admin/users/{id} soft-deletes (deactivates) user account
    - AC-5: All endpoints require Admin role authorization
- Edge Cases:
    - Admin cannot deactivate their own account → return 400
    - Duplicate email on create → return 409 Conflict
    - Assign invalid role → return 422 Unprocessable Entity

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Backend | ASP.NET Core | 8.0 LTS |
| ORM | Entity Framework Core | 8.0 |
| Identity | ASP.NET Identity | 8.0 |
| Database | SQL Server Express | 2022 |

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
Build admin-only API endpoints for managing user accounts: paginated listing with search/filter, create with role assignment, update details, and soft-delete deactivation. All endpoints enforce Admin role via [Authorize(Roles = "Admin")] policy. Audit log every mutation.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Identity module
- task_001_db_identity_scheduling_entities (US_008) — Requires User entity
- task_002_be_jwt_auth_api (US_013) — Requires RBAC policies

## Impacted Components
- NEW: AdminUsersController in Identity module
- NEW: IAdminUserService, AdminUserService
- NEW: AdminUserDto, CreateUserRequest, UpdateUserRequest DTOs
- MODIFY: Identity module service registration

## Implementation Plan
1. Create AdminUsersController with [Authorize(Roles = "Admin")] attribute
2. Implement GET endpoint with pagination, search by name/email, filter by role and status
3. Implement POST endpoint to create user with ASP.NET Identity, assign role
4. Implement PUT endpoint to update user details and role
5. Implement DELETE endpoint for soft-delete with self-deactivation guard
6. Add FluentValidation validators for create/update requests
7. Emit audit log entries for all mutations via AuditLog entity
8. Return standardized ProblemDetails for all error responses

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_008, US_013 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Identity/Controllers/AdminUsersController.cs | Admin user CRUD controller |
| CREATE | src/Modules/Identity/Services/IAdminUserService.cs | Service interface |
| CREATE | src/Modules/Identity/Services/AdminUserService.cs | Service implementation |
| CREATE | src/Modules/Identity/DTOs/AdminUserDto.cs | User list/detail DTOs |
| CREATE | src/Modules/Identity/DTOs/CreateUserRequest.cs | Create request DTO |
| CREATE | src/Modules/Identity/DTOs/UpdateUserRequest.cs | Update request DTO |
| CREATE | src/Modules/Identity/Validators/CreateUserValidator.cs | FluentValidation rules |
| MODIFY | src/Modules/Identity/DependencyInjection.cs | Register admin service |

## External References
- ASP.NET Identity UserManager: https://learn.microsoft.com/aspnet/core/security/authentication/identity
- ProblemDetails: https://learn.microsoft.com/aspnet/core/web-api/handle-errors

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] GET /api/admin/users returns paginated results with search/filter
- [x] POST /api/admin/users creates user with role
- [x] PUT /api/admin/users/{id} updates user
- [x] DELETE /api/admin/users/{id} soft-deletes account
- [x] Self-deactivation returns 400
- [x] Non-admin access returns 403

## Implementation Checklist
- [x] Create AdminUsersController with Admin role authorization
- [x] Implement paginated GET with search and role/status filter
- [x] Implement POST creating user via UserManager with role assignment
- [x] Implement PUT updating user details and role change
- [x] Implement soft-delete DELETE with self-deactivation guard
- [x] Add FluentValidation for create/update requests
- [x] Emit audit log entries for all create/update/deactivate operations
- [x] Return ProblemDetails for 400, 404, 409, 422 error responses
