---
post_title: "TASK_002 - Session Management API"
author1: "AI Senior Developer"
post_slug: "task-002-be-session-management"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_014, backend, session, token-invalidation, timeout"
ai_note: "Generated with AI assistance from user story US_014"
summary: "Implement server-side 15-minute session timeout with JWT/refresh token invalidation and session extension endpoint."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_SESSION_MANAGEMENT

## Requirement Reference
- User Story: us_014
- Story Location: .propel/context/tasks/EP-001/us_014/us_014.md
- Acceptance Criteria:
    - AC-1: 15-minute JWT access token lifetime enforced server-side per NFR-008
    - AC-2: Session timeout invalidates both JWT and refresh token per NFR-008
    - AC-3: POST /api/auth/extend-session validates and issues new tokens
    - AC-4: Expired sessions return 401 requiring full re-authentication
- Edge Cases:
    - Clock skew between client/server → 30-second grace period on token validation
    - Token invalidated while request in-flight → request completes; next request returns 401

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
| Caching | Upstash Redis | 7.x |

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
Implement server-side session management enforcing 15-minute JWT lifetimes. Create a session extension endpoint that validates the current refresh token and issues new JWT/refresh token pairs. On timeout, both access and refresh tokens are invalidated. Use Redis for tracking active sessions and invalidated tokens.

## Dependent Tasks
- task_002_be_jwt_auth_api (US_013) — Requires JWT and refresh token infrastructure

## Impacted Components
- NEW: ExtendSessionCommand/Handler
- NEW: Session tracking service using Redis
- NEW: Token blacklist for invalidated JWTs
- MODIFY: AuthController to add extend-session endpoint

## Implementation Plan
1. Create POST /api/auth/extend-session endpoint accepting current refresh token
2. Validate refresh token and issue new JWT + refresh token pair
3. Track active sessions in Redis with 15-minute TTL per user
4. Implement token blacklist in Redis for invalidated JWTs (short TTL matching JWT expiry)
5. Add 30-second clock skew tolerance in JWT validation parameters
6. Implement POST /api/auth/logout endpoint invalidating all tokens
7. Return 401 for expired/invalidated sessions requiring full re-authentication

## Current Project State
```
[PLACEHOLDER - Updated after US_013 JWT auth tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Modules/Identity/Identity.Application/Commands/ExtendSession/ExtendSessionCommand.cs | Session extension command |
| CREATE | backend/src/Modules/Identity/Identity.Infrastructure/Services/SessionTrackingService.cs | Redis session management |
| CREATE | backend/src/Modules/Identity/Identity.Infrastructure/Services/TokenBlacklistService.cs | JWT blacklist |
| MODIFY | backend/src/Modules/Identity/Identity.API/Controllers/AuthController.cs | Add extend-session, logout endpoints |

## External References
- JWT Validation Parameters: https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters

## Build Commands
- `dotnet build` — Build solution

## Implementation Validation Strategy
- [x] Extend-session returns new JWT with refreshed expiry
- [x] Expired tokens return 401 Unauthorized
- [x] Logout invalidates all active tokens
- [x] Clock skew tolerance handles 30-second window

## Implementation Checklist
- [x] Create POST /api/auth/extend-session with refresh token validation
- [x] Track active sessions in Redis with 15-minute TTL
- [x] Implement token blacklist in Redis for invalidated JWTs
- [x] Add 30-second clock skew tolerance in JWT validation
- [x] Create POST /api/auth/logout invalidating all tokens
- [x] Return 401 for expired/invalidated sessions
- [x] Test concurrent session extension and logout scenarios
