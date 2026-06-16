---
post_title: "TASK_002 - JWT Authentication API"
author1: "AI Senior Developer"
post_slug: "task-002-be-jwt-auth-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_013, backend, JWT, authentication, refresh-token, RBAC"
ai_note: "Generated with AI assistance from user story US_013"
summary: "Implement POST /api/auth/login with JWT Bearer tokens, refresh token rotation, Argon2id verification, and RBAC middleware."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_JWT_AUTH_API

## Requirement Reference
- User Story: us_013
- Story Location: .propel/context/tasks/EP-001/us_013/us_013.md
- Acceptance Criteria:
    - AC-1: POST /api/auth/login authenticates email/password and returns JWT + refresh token per FR-002
    - AC-2: JWT contains role claim for RBAC enforcement per NFR-009
    - AC-3: Refresh token is single-use with rotation per AD-006
    - AC-4: Rate limiting on login endpoint per NFR-010
    - AC-5: Deny-by-default RBAC middleware per NFR-009
- Edge Cases:
    - Refresh token reuse detected → all tokens for user revoked (token replay attack prevention)
    - Concurrent login from different devices → separate refresh tokens maintained per device

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
| Authentication | ASP.NET Identity + JWT Bearer | 8.0 |
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
Implement JWT Bearer authentication with refresh token rotation. Create login endpoint, JWT generation with role claims, single-use refresh token rotation (preventing replay attacks), rate limiting on auth endpoints, and deny-by-default RBAC middleware that enforces patient/staff/admin access scoping.

## Dependent Tasks
- task_002_be_registration_api (US_012) — Requires User entity and Argon2id hasher
- task_001_be_modular_monolith_setup (US_002) — Requires Identity module

## Impacted Components
- NEW: LoginCommand/Handler with JWT generation
- NEW: RefreshTokenCommand/Handler with rotation
- NEW: JwtTokenService for token generation/validation
- NEW: RBAC authorization middleware
- MODIFY: AuthController to add login and refresh endpoints

## Implementation Plan
1. Create JwtTokenService generating access tokens (15-min expiry) with role claims
2. Implement single-use refresh token generation and storage with rotation
3. Create POST /api/auth/login endpoint with Argon2id password verification
4. Create POST /api/auth/refresh endpoint with token rotation
5. Implement rate limiting middleware (e.g., 5 attempts per minute per IP) on auth endpoints
6. Create deny-by-default RBAC authorization policies (Patient, Staff, Admin)
7. Implement refresh token reuse detection → revoke all tokens for user
8. Configure JWT Bearer authentication middleware in Program.cs

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_012 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Modules/Identity/Identity.Infrastructure/Services/JwtTokenService.cs | JWT generation with role claims |
| CREATE | backend/src/Modules/Identity/Identity.Application/Commands/Login/LoginCommand.cs | Login command |
| CREATE | backend/src/Modules/Identity/Identity.Application/Commands/Login/LoginCommandHandler.cs | Auth handler |
| CREATE | backend/src/Modules/Identity/Identity.Application/Commands/RefreshToken/RefreshTokenCommand.cs | Refresh command |
| CREATE | backend/src/Shared/SharedKernel/Authorization/RbacAuthorizationHandler.cs | Deny-by-default RBAC |
| MODIFY | backend/src/Modules/Identity/Identity.API/Controllers/AuthController.cs | Add login/refresh endpoints |
| MODIFY | backend/src/Host/Program.cs | Register JWT auth and RBAC middleware |

## External References
- JWT Bearer in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt
- Rate Limiting Middleware: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit

## Build Commands
- `dotnet build` — Build solution
- `dotnet run --project src/Host` — Run API

## Implementation Validation Strategy
- [x] Login returns JWT with correct role claim
- [x] Refresh token rotation invalidates previous token
- [x] Reused refresh token triggers full revocation
- [x] Rate limiting blocks after threshold
- [x] Unauthorized routes return 401/403

## Implementation Checklist
- [x] Create JwtTokenService with 15-minute access token and role claims
- [x] Implement single-use refresh token with rotation and reuse detection
- [x] Create POST /api/auth/login with Argon2id password verification
- [x] Create POST /api/auth/refresh with token rotation
- [x] Implement rate limiting on auth endpoints (5 attempts/min/IP)
- [x] Create deny-by-default RBAC authorization policies (Patient, Staff, Admin)
- [x] Configure JWT Bearer middleware and RBAC in Program.cs
- [x] Handle edge cases: token replay revocation, concurrent device sessions
