# Task - TASK_001_BE_RBAC_ENFORCEMENT

## Requirement Reference

### User Story
- **Story ID**: US_045
- **Title**: RBAC & Access Control Enforcement
- **Path**: `.propel/context/tasks/EP-010/us_045/us_045.md`

### Acceptance Criteria Addressed
1. Deny-by-default RBAC enforced — patients access only their own data, staff access scoped patient data, admins follow minimum necessary standard per FR-032 and NFR-009.
2. OWASP Top 10 controls: parameterized queries (EF Core LINQ), Content Security Policy headers, HSTS, and input validation at all API boundaries per NFR-010.
3. Rate limiting applied to authentication endpoints to prevent brute-force attacks per NFR-010.
4. Patient attempting to access another patient's data is denied with 403 Forbidden.
5. Zero PHI transmitted to external AI providers — all inference runs locally per AIR-S01.

### Edge Cases
- Staff role changed during active session — role change takes effect on next token refresh; active session retains previous permissions until JWT expires (15 minutes).
- Admin tries to access raw PHI beyond minimum necessary standard — access denied; attempt logged to immutable audit trail.

## Design References (Frontend Tasks Only)
N/A — middleware/infrastructure task, no UI.

## Applicable Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend | ASP.NET Core | 8.0 LTS |
| Authentication | ASP.NET Identity + JWT Bearer | 8.0 |
| Rate Limiting | ASP.NET Core Rate Limiting | 8.0 (built-in) |
| ORM | EF Core (parameterized queries) | 8.0 |
| AI Runtime | Ollama (local) | 0.3.x |

## AI References (AI Tasks Only)

### AI Requirement References
- **AIR-S01**: Zero PHI transmission to external AI providers — enforce local-only inference via network policy and configuration validation.

## Mobile References (Mobile Tasks Only)
N/A — not applicable for this project.

## Task Overview
Implement comprehensive RBAC enforcement with deny-by-default authorization policies, resource-based authorization for patient data ownership, OWASP Top 10 security middleware (CSP, HSTS, input validation, rate limiting), and an AI network isolation guard ensuring all Ollama calls are local-only. This task secures all API endpoints with role-based and resource-based policies, adds security headers, and prevents brute-force attacks.

## Dependent Tasks

| Task ID | Task Name | Dependency Type |
|---------|-----------|----------------|
| TASK_002_BE_JWT_AUTH_API (US_013) | JWT Auth API | Must complete first — authentication and token infrastructure |
| TASK_002_BE_ADMIN_USER_API (US_015) | Admin User API | Must complete first — role assignment infrastructure |
| TASK_001_BE_MODULAR_MONOLITH_SETUP (US_002) | Modular Monolith Setup | Must complete first — middleware pipeline |

## Impacted Components

| Component | Type | Impact |
|-----------|------|--------|
| Authorization Policies | Backend | CREATE — Deny-by-default + role-based + resource-based policies |
| PatientOwnershipHandler | Backend | CREATE — IAuthorizationHandler for patient data ownership |
| SecurityHeadersMiddleware | Middleware | CREATE — CSP, HSTS, X-Content-Type-Options, X-Frame-Options |
| Rate Limiting Configuration | Middleware | CREATE — Fixed window rate limiter on auth endpoints |
| Input Validation Filters | Backend | CREATE — FluentValidation pipeline for all API boundaries |
| AiNetworkGuard | Backend | CREATE — Startup check ensuring Ollama runs on localhost only |

## Implementation Plan

1. **Configure Deny-by-Default Authorization**: In `Program.cs`, set `options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()` to deny all unauthenticated requests by default. Define named policies: `PatientOnly`, `StaffOrAbove`, `AdminOnly`, `ComplianceOfficer`.

2. **Implement Resource-Based Patient Ownership**: Create `PatientOwnershipAuthorizationHandler : AuthorizationHandler<PatientOwnershipRequirement, PatientResource>` that validates the authenticated user's `PatientId` claim matches the requested resource's `PatientId`. Return 403 Forbidden on mismatch. Apply to all patient-specific endpoints.

3. **Implement Staff Data Scoping**: Create `StaffDataScopeFilter` that automatically applies EF Core global query filters based on the authenticated staff member's assigned patient scope. Admin users are restricted to minimum necessary — access only the data fields required for their administrative function.

4. **Add Security Headers Middleware**: Create `SecurityHeadersMiddleware` that adds: `Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' https://api.sendgrid.com https://api.twilio.com`, `Strict-Transport-Security: max-age=31536000; includeSubDomains`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`.

5. **Configure Rate Limiting**: Use ASP.NET Core 8.0 built-in rate limiting middleware. Apply a fixed window policy to `/api/auth/login` and `/api/auth/register`: 5 requests per 15-minute window per IP. Return 429 Too Many Requests with `Retry-After` header on violation.

6. **Add Input Validation Pipeline**: Register FluentValidation with the MediatR/controller pipeline. Create validators for all request DTOs. Reject invalid input with 400 Bad Request before reaching business logic. EF Core LINQ ensures parameterized queries (no raw SQL).

7. **Implement AI Network Isolation Guard**: Create `AiNetworkGuard` startup validator that reads the Ollama base URL from configuration and verifies it resolves to `127.0.0.1` or `localhost`. If an external URL is configured, the application fails to start with a clear error: "AIR-S01 violation: Ollama must run locally."

## Current Project State
[PLACEHOLDER — to be filled during implementation sprint]

## Expected Changes

| Action | File/Component | Description |
|--------|---------------|-------------|
| MODIFY | `Program.cs` | Add FallbackPolicy, named authorization policies, rate limiting, security headers |
| CREATE | `src/Shared/Authorization/PatientOwnershipHandler.cs` | Resource-based authorization for patient data ownership |
| CREATE | `src/Shared/Authorization/PatientOwnershipRequirement.cs` | Authorization requirement definition |
| CREATE | `src/Shared/Authorization/StaffDataScopeFilter.cs` | EF Core global query filter for staff data scoping |
| CREATE | `src/Shared/Middleware/SecurityHeadersMiddleware.cs` | CSP, HSTS, X-Content-Type-Options, X-Frame-Options |
| CREATE | `src/Shared/Validation/AiNetworkGuard.cs` | Startup check for Ollama localhost-only enforcement |
| MODIFY | `Rate limiting configuration` | Fixed window policy on /api/auth/login and /api/auth/register |
| CREATE | `FluentValidation validators` | Request DTO validators for all API endpoints |

## External References
- [ASP.NET Core Authorization Policies](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [Resource-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased)
- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [OWASP Top 10 (2021)](https://owasp.org/Top10/)

## Build Commands
```bash
# Install FluentValidation
dotnet add src/Shared package FluentValidation.AspNetCore

# Build solution
dotnet build src/UnifiedPatientAccess.sln

# Run with HTTPS to validate security headers
dotnet run --project src/Host -- --urls "https://localhost:5001"

# Test rate limiting
for i in {1..6}; do curl -s -o /dev/null -w "%{http_code}" https://localhost:5001/api/auth/login -X POST; done
```

## Implementation Validation Strategy
- [x] Verify unauthenticated requests to any endpoint return 401 (deny-by-default)
- [x] Verify patient accessing another patient's data returns 403 Forbidden
- [x] Verify staff can only access data within their assigned scope
- [x] Verify security headers (CSP, HSTS, X-Content-Type-Options, X-Frame-Options) present on all responses
- [x] Verify 6th login attempt within 15 minutes returns 429 Too Many Requests
- [x] Verify AI network guard rejects startup with non-localhost Ollama URL

## Implementation Checklist
- [x] Configure deny-by-default FallbackPolicy and named role-based policies
- [x] Implement PatientOwnershipAuthorizationHandler for resource-based 403 enforcement
- [x] Add StaffDataScopeFilter with EF Core global query filters
- [x] Create SecurityHeadersMiddleware with CSP, HSTS, X-Frame-Options, X-Content-Type-Options
- [x] Configure fixed window rate limiting on authentication endpoints (5 req/15 min/IP)
- [x] Register FluentValidation pipeline for all request DTO input validation
- [x] Implement AiNetworkGuard startup check for Ollama localhost-only enforcement
- [x] Validate end-to-end: 401 unauthenticated, 403 ownership, 429 rate limit, headers present
