# Task - TASK_001_INFRA_RELIABILITY_BACKUP

## Requirement Reference

### User Story
- **Story ID**: US_046
- **Title**: Platform Reliability & Backup Security
- **Path**: `.propel/context/tasks/EP-010/us_046/us_046.md`

### Acceptance Criteria Addressed
1. 99.9% monthly uptime maintained with maximum 43.8 minutes unplanned downtime per NFR-012.
2. Mean time between failures (MTBF) meets or exceeds 720 hours for core scheduling operations per NFR-018.
3. Database backups encrypted using AES-256 and stored in a separate storage location per DR-015.
4. Zero PHI reaches external AI providers — all AI inference runs locally per AIR-S01.
5. System can restore from backup within RPO 24h and RTO 4h per DR-014.

### Edge Cases
- Uptime drops below 99.9% — alert triggered; root cause analysis initiated; incident logged for compliance.
- Backup storage becomes inaccessible — backup job fails with alert; rerouted to secondary storage path; no backups skipped.

## Design References (Frontend Tasks Only)
N/A — infrastructure task, no UI.

## Applicable Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Database | SQL Server Express | 2022 |
| Backend | ASP.NET Core | 8.0 LTS |
| Health Checks | ASP.NET Core Health Checks | 8.0 |
| Logging | Serilog | 3.x |
| Backup Encryption | System.Security.Cryptography (AES-256) | .NET 8 |
| AI Runtime | Ollama (local) | 0.3.x |

## AI References (AI Tasks Only)

### AI Requirement References
- **AIR-S01**: Zero PHI to external AI providers — validated by network isolation checks in backup/recovery procedures.

## Mobile References (Mobile Tasks Only)
N/A — not applicable for this project.

## Task Overview
Implement platform reliability infrastructure: configure automated AES-256 encrypted database backups stored in a separate location with a secondary failover path, build uptime and MTBF monitoring dashboards via health check aggregation, create automated backup verification and restore testing procedures, and establish RPO 24h / RTO 4h disaster recovery runbooks with restoration scripts.

## Dependent Tasks

| Task ID | Task Name | Dependency Type |
|---------|-----------|----------------|
| TASK_001_DB_SCHEMA_ORM_SETUP (US_003) | Database & ORM Setup | Must complete first — SQL Server instance for backup configuration |
| TASK_001_BE_LOGGING_HEALTH_MONITORING (US_007) | Logging & Health Monitoring | Must complete first — health check infrastructure |
| TASK_001_INFRA_PHI_ENCRYPTION (US_043) | PHI Encryption | Should complete first — TDE and encryption key management |

## Impacted Components

| Component | Type | Impact |
|-----------|------|--------|
| SQL Server Backup Agent | Database | CREATE — Automated backup job with AES-256 encryption |
| Backup Storage Configuration | Infrastructure | CREATE — Primary and secondary backup storage paths |
| UptimeMonitorService | Backend Service | CREATE — Hosted service tracking uptime and MTBF metrics |
| BackupHealthCheck | Health Check | CREATE — Verifies latest backup age and integrity |
| RestoreVerificationJob | Backend Service | CREATE — Periodic automated restore test to validation database |
| Disaster Recovery Runbook | Documentation | CREATE — Step-by-step RPO 24h / RTO 4h recovery guide |

## Implementation Plan

1. **Configure Automated Encrypted Backups**: Create a SQL Server Agent job (or T-SQL scheduled task for Express edition) that runs `BACKUP DATABASE` with `ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = BackupCert)` every 4 hours (full backup daily, differential every 4 hours, transaction log every 30 minutes). Store backups in a separate directory from the primary data files.

2. **Implement Backup Storage Failover**: Configure primary backup path (e.g., `D:\Backups\Primary`) and secondary backup path (e.g., `E:\Backups\Secondary`). The backup job attempts primary first; on failure, retries to secondary and triggers an alert via Serilog. Both paths are validated at startup.

3. **Build Uptime Monitoring Service**: Create `UptimeMonitorService : BackgroundService` that pings all health check endpoints every 60 seconds and records uptime/downtime events to a `PlatformMetrics` table. Calculate rolling 30-day uptime percentage. Trigger Serilog alerts when uptime drops below 99.9% threshold.

4. **Implement MTBF Tracking**: Track failure events for core scheduling operations (appointment create, update, cancel). Calculate MTBF as total operational hours divided by failure count. Log to `PlatformMetrics` and alert when MTBF drops below 720 hours.

5. **Create Backup Health Check**: Implement `BackupHealthCheck : IHealthCheck` that verifies: (a) latest backup is less than 4 hours old, (b) backup file exists and is not zero-length, (c) backup certificate is valid and not expiring within 30 days. Return `Degraded` if backup age exceeds 4 hours, `Unhealthy` if exceeds 24 hours. Register in the health check endpoint.

6. **Build Restore Verification Job**: Create `RestoreVerificationService : BackgroundService` that runs weekly. It restores the latest backup to a `_Verify` database, runs validation queries (row counts, schema integrity), then drops the verification database. Logs success/failure to audit trail.

7. **Create Disaster Recovery Runbook**: Document step-by-step procedures for RPO 24h / RTO 4h recovery: (a) identify failure, (b) locate latest valid backup, (c) restore to new or repaired instance, (d) verify data integrity, (e) update connection strings, (f) validate health checks pass. Include estimated time for each step.

## Current Project State
[PLACEHOLDER — to be filled during implementation sprint]

## Expected Changes

| Action | File/Component | Description |
|--------|---------------|-------------|
| CREATE | `scripts/backup-job.sql` | SQL Server backup job with AES-256 encryption |
| CREATE | `scripts/backup-certificate.sql` | Backup encryption certificate creation and backup script |
| CREATE | `src/Shared/HealthChecks/BackupHealthCheck.cs` | Health check verifying backup recency and integrity |
| CREATE | `src/Shared/Monitoring/UptimeMonitorService.cs` | Background service tracking uptime percentage |
| CREATE | `src/Shared/Monitoring/MtbfTracker.cs` | MTBF calculation for core scheduling operations |
| CREATE | `src/Shared/Backup/RestoreVerificationService.cs` | Weekly automated restore verification |
| CREATE | `src/Modules/Identity/Entities/PlatformMetrics.cs` | Entity for uptime/MTBF metric storage |
| CREATE | `docs/disaster-recovery-runbook.md` | RPO 24h / RTO 4h step-by-step recovery guide |
| MODIFY | `Program.cs` | Register backup health check and monitoring services |

## External References
- [SQL Server Backup Encryption](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/backup-encryption)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [SQL Server Express Limitations](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2022)

## Build Commands
```bash
# Create backup certificate
sqlcmd -S localhost -d master -i scripts/backup-certificate.sql

# Schedule backup job
sqlcmd -S localhost -d UnifiedPatientAccess -i scripts/backup-job.sql

# Run manual backup test
sqlcmd -S localhost -Q "BACKUP DATABASE UnifiedPatientAccess TO DISK='D:\Backups\Primary\test.bak' WITH ENCRYPTION(ALGORITHM=AES_256, SERVER CERTIFICATE=BackupCert)"

# Build solution
dotnet build src/UnifiedPatientAccess.sln

# Verify health checks
curl https://localhost:5001/health
```

## Implementation Validation Strategy
- [x] Verify backup job produces AES-256 encrypted backup files in the separate storage location
- [x] Verify backup failover path is used when primary is unavailable
- [x] Verify BackupHealthCheck returns Degraded when backup age exceeds 4 hours
- [x] Verify UptimeMonitorService calculates rolling 30-day uptime correctly
- [x] Verify restore verification job restores and validates backup successfully
- [x] Verify DR runbook achieves restore within 4 hours on a test scenario

## Implementation Checklist
- [x] Create SQL Server backup job with AES-256 encryption (full daily, differential 4h, log 30m)
- [x] Configure primary and secondary backup storage paths with failover logic
- [x] Implement UptimeMonitorService with 99.9% threshold alerting
- [x] Add MTBF tracking for core scheduling operations with 720-hour threshold
- [x] Build BackupHealthCheck verifying backup recency, file existence, and certificate validity
- [x] Create RestoreVerificationService for weekly automated restore testing
- [x] Write disaster recovery runbook with RPO 24h / RTO 4h procedures
- [x] Validate end-to-end: encrypted backups, failover, health checks, restore test passes
