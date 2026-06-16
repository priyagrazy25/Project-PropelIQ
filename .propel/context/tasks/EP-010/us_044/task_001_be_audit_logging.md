# Task - TASK_001_BE_AUDIT_LOGGING

## Requirement Reference

### User Story
- **Story ID**: US_044
- **Title**: Immutable Audit Logging
- **Path**: `.propel/context/tasks/EP-010/us_044/us_044.md`

### Acceptance Criteria Addressed
1. Immutable audit log record created for every PHI-touching action capturing actor identity, action type, timestamp, affected resource, and before/after state snapshots per FR-031.
2. No application role has UPDATE or DELETE permissions on audit log tables per DR-011.
3. AI model invocation audit records include input/output token counts, model version, confidence scores, and processing duration per AIR-S03.
4. PHI is redacted from application logs, error messages, and stack traces — only the encrypted audit store contains PHI per NFR-011.
5. All audit records retained for a minimum of 7 years per DR-011.

### Edge Cases
- Audit logging service fails — primary operation continues but is flagged; background process retries from a transient queue.
- Audit store exceeds storage — archival process compresses and moves older records to cold storage while maintaining 7-year accessibility.

## Design References (Frontend Tasks Only)

### Screen Specifications
- **Screen ID**: SCR-025 (Audit Log Viewer)
- **Figma Spec**: `.propel/context/docs/figma_spec.md#SCR-025`
- **Wireframe**: `.propel/context/wireframes/Hi-Fi/wireframe-SCR-025-audit-log-viewer.html`

> Note: SCR-025 Audit Log Viewer is a read-only compliance viewer. The viewer frontend is minimal (filtered table) and is included in this task's scope as part of the audit infrastructure.

## Applicable Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend | ASP.NET Core | 8.0 LTS |
| Logging | Serilog | 3.x |
| Database | SQL Server Express | 2022 |
| ORM | EF Core | 8.0 |
| Caching/Queue | Upstash Redis | 7.x |
| Frontend | React TypeScript | 18.x |

## AI References (AI Tasks Only)

### AI Requirement References
- **AIR-S03**: AI invocation audit logging — capture input/output token counts, model version, confidence scores, and processing duration for every AI model call.

## Mobile References (Mobile Tasks Only)
N/A — not applicable for this project.

## Task Overview
Implement a comprehensive immutable audit logging system: create append-only audit log tables with database-level restrictions preventing UPDATE/DELETE, build an `IAuditService` middleware that intercepts all PHI-touching operations and captures before/after state snapshots, extend Serilog with a `PhiScrubbingEnricher` to redact PHI from application logs while routing full audit details to the encrypted audit store, add AI invocation audit enrichment capturing token counts and model metadata, implement a transient retry queue for audit failures, and create the SCR-025 audit log viewer (read-only filtered table for compliance officers).

## Dependent Tasks

| Task ID | Task Name | Dependency Type |
|---------|-----------|----------------|
| TASK_001_BE_LOGGING_HEALTH_MONITORING (US_007) | Logging & Health Monitoring | Must complete first — Serilog infrastructure and PhiScrubbingEnricher base |
| TASK_001_DB_SCHEMA_ORM_SETUP (US_003) | Database & ORM Setup | Must complete first — audit log table schema |
| TASK_001_BE_CACHING_REALTIME_SETUP (US_004) | Caching & Real-Time Setup | Must complete first — Redis for transient retry queue |

## Impacted Components

| Component | Type | Impact |
|-----------|------|--------|
| AuditLog Table | Database | CREATE — Append-only table with no UPDATE/DELETE grants |
| AiInvocationAuditLog Table | Database | CREATE — AI-specific audit table with token counts |
| IAuditService | Backend Service | CREATE — Core audit logging service with before/after snapshots |
| AuditInterceptor | Middleware | CREATE — EF Core SaveChanges interceptor for automatic audit capture |
| PhiScrubbingEnricher | Serilog Enricher | MODIFY — Extend to handle audit-specific PHI patterns |
| AuditRetryQueue | Backend Service | CREATE — Redis-backed transient queue for failed audit writes |
| AuditLogViewer (SCR-025) | React Component | CREATE — Read-only filterable table for compliance officers |

## Implementation Plan

1. **Create Audit Log Tables**: Define `AuditLog` entity with columns: `Id (BIGINT IDENTITY)`, `ActorId`, `ActorRole`, `ActionType`, `ResourceType`, `ResourceId`, `BeforeState (NVARCHAR(MAX))`, `AfterState (NVARCHAR(MAX))`, `Timestamp (DATETIME2)`, `CorrelationId`. Create `AiInvocationAuditLog` with additional columns: `ModelVersion`, `InputTokenCount`, `OutputTokenCount`, `ConfidenceScore`, `ProcessingDurationMs`. Apply `DENY UPDATE, DELETE ON AuditLog TO [AppRole]` at database level.

2. **Implement EF Core SaveChanges Interceptor**: Create `AuditSaveChangesInterceptor : SaveChangesInterceptor` that detects changes to PHI-containing entities, serializes before/after state snapshots (excluding raw PHI from serialization — use entity IDs and field-level change indicators), and queues `AuditLog` entries within the same transaction.

3. **Build IAuditService**: Create `AuditService` implementing `IAuditService` with methods `LogActionAsync(AuditEntry entry)` and `LogAiInvocationAsync(AiAuditEntry entry)`. On write failure, push the entry to the Redis-backed `AuditRetryQueue` and flag the operation for audit gap. Include a background `AuditRetryProcessor` hosted service that drains the retry queue every 30 seconds.

4. **Extend Serilog PHI Scrubbing**: Enhance the existing `PhiScrubbingEnricher` to intercept audit-related log events and ensure PHI fields are replaced with `[REDACTED]` tokens in application logs. Route the unscrubbed audit data exclusively to the encrypted audit store sink.

5. **Add AI Invocation Audit Enrichment**: Create `AiAuditMiddleware` that wraps Semantic Kernel function calls and captures `InputTokenCount`, `OutputTokenCount`, `ModelVersion`, `ConfidenceScore`, and `ProcessingDurationMs`. Automatically log to `AiInvocationAuditLog` on every AI model invocation.

6. **Implement Retention Policy Infrastructure**: Create a `RetentionPolicyService` background job that checks audit record age. Records older than 7 years are archived (compressed to cold storage path), never deleted. Add a configuration setting for retention period.

7. **Build SCR-025 Audit Log Viewer**: Create a React page at `/admin/audit-logs` with a filterable, paginated, read-only table showing audit records. Filters: date range, actor, action type, resource type. Expose a GET `/api/admin/audit-logs` endpoint with query parameters for filtering and pagination. Restrict access to Admin and ComplianceOfficer roles.

## Current Project State
[PLACEHOLDER — to be filled during implementation sprint]

## Expected Changes

| Action | File/Component | Description |
|--------|---------------|-------------|
| CREATE | `src/Modules/Identity/Entities/AuditLog.cs` | Append-only audit log entity |
| CREATE | `src/Modules/Clinical/Entities/AiInvocationAuditLog.cs` | AI-specific audit log entity |
| CREATE | `src/Shared/Audit/AuditService.cs` | Core audit logging service |
| CREATE | `src/Shared/Audit/IAuditService.cs` | Audit service interface |
| CREATE | `src/Shared/Audit/AuditSaveChangesInterceptor.cs` | EF Core interceptor for automatic audit |
| CREATE | `src/Shared/Audit/AuditRetryProcessor.cs` | Background service for retry queue |
| CREATE | `src/Shared/Audit/AiAuditMiddleware.cs` | AI invocation audit capture middleware |
| CREATE | `src/Shared/Audit/RetentionPolicyService.cs` | Background job for 7-year retention archival |
| MODIFY | `src/Shared/Logging/PhiScrubbingEnricher.cs` | Extend PHI redaction for audit log events |
| CREATE | `src/Host/Controllers/Admin/AuditLogsController.cs` | GET /api/admin/audit-logs endpoint |
| CREATE | `frontend/src/features/admin/pages/AuditLogViewer.tsx` | SCR-025 audit log viewer page |
| CREATE | `EF Core migration` | Audit tables with DENY UPDATE/DELETE grants |

## External References
- [EF Core SaveChanges Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Serilog Enrichment](https://github.com/serilog/serilog/wiki/Enrichment)
- [HIPAA Audit Controls (§164.312(b))](https://www.hhs.gov/hipaa/for-professionals/security/guidance/index.html)

## Build Commands
```bash
# Generate audit tables migration
dotnet ef migrations add AddAuditLogTables --project src/Infrastructure
dotnet ef database update --project src/Infrastructure

# Apply DENY permissions via SQL
sqlcmd -S localhost -d UnifiedPatientAccess -i scripts/audit-permissions.sql

# Build backend
dotnet build src/UnifiedPatientAccess.sln

# Build frontend
cd frontend && npm run build
```

## Implementation Validation Strategy
- [x] Verify AuditLog table rejects UPDATE/DELETE from application role
- [x] Verify PHI-touching operations produce audit records with before/after snapshots
- [x] Verify AI invocations log token counts, model version, confidence, and duration
- [x] Verify application logs contain `[REDACTED]` instead of PHI values
- [x] Verify audit retry queue captures and replays failed audit writes
- [x] Verify SCR-025 viewer displays filtered, paginated audit records for Admin role

## Implementation Checklist
- [x] Create AuditLog and AiInvocationAuditLog tables with DENY UPDATE/DELETE permissions
- [x] Implement AuditSaveChangesInterceptor for automatic before/after snapshot capture
- [x] Build AuditService with Redis-backed retry queue and AuditRetryProcessor
- [x] Extend PhiScrubbingEnricher to redact PHI from application logs
- [x] Add AiAuditMiddleware capturing token counts, model version, and duration
- [x] Implement RetentionPolicyService for 7-year archival (no deletion)
- [x] Build SCR-025 Audit Log Viewer page with filterable table and API endpoint
- [x] Validate end-to-end: audit capture, PHI redaction, retry queue, viewer access
