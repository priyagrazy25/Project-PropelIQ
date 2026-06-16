---
post_title: "TASK_002 - Patient Registration API"
author1: "AI Senior Developer"
post_slug: "task-002-be-registration-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_012, backend, API, registration, identity"
ai_note: "Generated with AI assistance from user story US_012"
summary: "Implement POST /api/auth/register endpoint with email validation, demographic capture, password hashing, and duplicate detection."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_REGISTRATION_API

## Requirement Reference
- User Story: us_012
- Story Location: .propel/context/tasks/EP-001/us_012/us_012.md
- Acceptance Criteria:
    - AC-1: POST /api/auth/register accepts name, email, DOB, phone, address per FR-001
    - AC-2: Email uniqueness enforced; 409 Conflict on duplicate per DR-009
    - AC-3: Password hashed with Argon2id (3 iterations, 64MB, 1 parallelism) per NFR-007
    - AC-4: New user created with Patient role and Active status per DR-001
    - AC-5: Response returns 201 Created with user ID
- Edge Cases:
    - Malformed request body → 400 Bad Request with validation errors
    - Database unavailable → 503 Service Unavailable with retry-after header

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Backend | ASP.NET Core Web API | 8.0 LTS |
| Authentication | ASP.NET Identity | 8.0 |
| ORM | Entity Framework Core | 8.0 |
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
Implement the patient registration API endpoint. Create POST /api/auth/register that accepts demographic data, validates email uniqueness, hashes the password with Argon2id, creates a User entity with Patient role, and returns 201 Created. Enforce input validation at the API boundary per NFR-010.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Identity module structure
- task_001_db_identity_scheduling_entities (US_008) — Requires User entity

## Impacted Components
- NEW: AuthController with Register endpoint
- NEW: RegisterCommand / RegisterCommandHandler (CQRS)
- NEW: RegisterValidator (FluentValidation)
- NEW: UserService for identity operations

## Implementation Plan
1. Create RegisterRequest DTO with validation attributes
2. Create RegisterCommand and RegisterCommandHandler in Identity.Application
3. Implement FluentValidation rules for all input fields
4. Configure ASP.NET Identity with Argon2id password hasher (3 iterations, 64MB, 1 parallelism)
5. Implement email uniqueness check against database
6. Create AuthController with POST /api/auth/register endpoint
7. Return 201 Created with user ID on success, 409 on duplicate, 400 on validation failure
8. Add input sanitization at API boundary per NFR-010

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_008 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Modules/Identity/Identity.Application/Commands/Register/RegisterCommand.cs | Registration command |
| CREATE | backend/src/Modules/Identity/Identity.Application/Commands/Register/RegisterCommandHandler.cs | Handler with Argon2id hashing |
| CREATE | backend/src/Modules/Identity/Identity.Application/Commands/Register/RegisterValidator.cs | FluentValidation rules |
| CREATE | backend/src/Modules/Identity/Identity.API/Controllers/AuthController.cs | Registration endpoint |
| CREATE | backend/src/Modules/Identity/Identity.Infrastructure/Services/Argon2idPasswordHasher.cs | Custom Argon2id hasher |

## External References
- ASP.NET Identity: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity
- FluentValidation: https://docs.fluentvalidation.net/en/latest/aspnet.html

## Build Commands
- `dotnet build` — Build solution
- `dotnet run --project src/Host` — Run API

## Implementation Validation Strategy
- [x] POST /api/auth/register returns 201 with valid data
- [x] Duplicate email returns 409 Conflict
- [x] Invalid input returns 400 with validation details
- [x] Password stored as Argon2id hash (verified in database)

## Implementation Checklist
- [x] Create RegisterRequest DTO and FluentValidation rules
- [x] Create RegisterCommand/Handler in Identity.Application
- [x] Configure Argon2id password hasher (3 iterations, 64MB, 1 parallelism)
- [x] Implement email uniqueness check with 409 Conflict response
- [x] Create AuthController with POST /api/auth/register returning 201 Created
- [x] Add input sanitization and parameterized queries (EF Core LINQ) per NFR-010
- [x] Handle edge cases: 400 validation errors, 503 database unavailable
- [x] Verify Swagger documents the endpoint with request/response examples
