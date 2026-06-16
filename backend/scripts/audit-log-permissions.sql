-- ============================================================================
-- audit-log-permissions.sql
-- Enforces append-only semantics on audit tables (DR-011, AC-5, FR-031).
-- Denies UPDATE and DELETE for all non-sysadmin users.
-- Run after migrations have been applied.
-- ============================================================================

USE [UnifiedPatientAccess];
GO

-- Deny UPDATE/DELETE on clinical.AuditLogs
IF OBJECT_ID('clinical.AuditLogs', 'U') IS NOT NULL
BEGIN
    DENY UPDATE ON [clinical].[AuditLogs] TO [public];
    DENY DELETE ON [clinical].[AuditLogs] TO [public];
    PRINT 'clinical.AuditLogs append-only permissions applied.';
END
GO

-- Deny UPDATE/DELETE on clinical.AiInvocationAuditLogs
IF OBJECT_ID('clinical.AiInvocationAuditLogs', 'U') IS NOT NULL
BEGIN
    DENY UPDATE ON [clinical].[AiInvocationAuditLogs] TO [public];
    DENY DELETE ON [clinical].[AiInvocationAuditLogs] TO [public];
    PRINT 'clinical.AiInvocationAuditLogs append-only permissions applied.';
END
GO

-- Deny UPDATE/DELETE on scheduling.AuditLogs
IF OBJECT_ID('scheduling.AuditLogs', 'U') IS NOT NULL
BEGIN
    DENY UPDATE ON [scheduling].[AuditLogs] TO [public];
    DENY DELETE ON [scheduling].[AuditLogs] TO [public];
    PRINT 'scheduling.AuditLogs append-only permissions applied.';
END
GO

PRINT 'All audit log append-only permissions applied successfully.';
PRINT 'Retention: Records must be retained for minimum 7 years per DR-011.';
GO
